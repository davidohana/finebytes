using System.Diagnostics;
using Mfr.Filters.Formatting.FormatString;
using Mfr.Filters.Formatting.Tokens;
using Mfr.Models.RenameList.Fields.Extended;

namespace Mfr.Filters
{
    /// <summary>
    /// Maps formatter token canonical names to Rename List catalog fields.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Meta/session/generator tokens (<c>counter</c>, <c>substr</c>, <c>token</c>, <c>now</c>, …) are omitted.
    /// Generic <c>exif</c> (tag-id lookup) is omitted — only named <c>exif-*</c> / <c>exif-date</c> map via
    /// <see cref="IRenameListMappedFormatToken"/>.
    /// </para>
    /// </remarks>
    internal static class FormatTokenRenameListFieldMap
    {
        /// <summary>
        /// Resolves a validated formatter token to a catalog field when mapped.
        /// </summary>
        /// <param name="canonicalName">Primary token name from validation.</param>
        /// <param name="args">Raw argument text after the first <c>:</c>, or empty.</param>
        /// <param name="groupId">Catalog group id when mapped.</param>
        /// <param name="propertyKey">Catalog property key when mapped.</param>
        /// <returns><see langword="true"/> when the token maps to a Rename List field.</returns>
        internal static bool TryMap(string canonicalName, string args, out string groupId, out string propertyKey)
        {
            if (string.Equals(canonicalName, "file-date", StringComparison.Ordinal))
            {
                return _TryMapFileDate(args, out groupId, out propertyKey);
            }

            if (
                FormatTokenRegistry.NameToToken.TryGetValue(canonicalName, out var token)
                && token is IRenameListMappedFormatToken mapped
                && mapped.TryGetFixedField(out groupId, out propertyKey)
            )
            {
                return true;
            }

            groupId = string.Empty;
            propertyKey = string.Empty;
            return false;
        }

        /// <summary>
        /// Maps a filesystem <see cref="TimestampField"/> to the Extended date column.
        /// </summary>
        /// <param name="timestampField">Which file timestamp the filter or token targets.</param>
        /// <param name="groupId">Catalog group id when mapped.</param>
        /// <param name="propertyKey">Catalog property key when mapped.</param>
        /// <returns>Always <see langword="true"/> for known enum values.</returns>
        /// <exception cref="UnreachableException">Unexpected enum value.</exception>
        internal static bool TryMapTimestampField(
            TimestampField timestampField,
            out string groupId,
            out string propertyKey
        )
        {
            groupId = ExtendedRenameListFields.Group;
            propertyKey = timestampField switch
            {
                TimestampField.Creation => ExtendedRenameListFields.Key.CreationDate,
                TimestampField.LastWrite => ExtendedRenameListFields.Key.LastWriteDate,
                TimestampField.LastAccess => ExtendedRenameListFields.Key.LastAccessDate,
                _ => throw new UnreachableException(),
            };
            return true;
        }

        /// <summary>
        /// Maps <c>file-date</c> args (<c>format,date-kind</c>) to an Extended date column.
        /// </summary>
        private static bool _TryMapFileDate(string args, out string groupId, out string propertyKey)
        {
            groupId = string.Empty;
            propertyKey = string.Empty;
            if (string.IsNullOrWhiteSpace(args))
            {
                return false;
            }

            var lastComma = args.LastIndexOf(',');
            if (lastComma < 0 || lastComma >= args.Length - 1)
            {
                return false;
            }

            var dateKind = args[(lastComma + 1)..].Trim();
            if (!TimestampFieldKeywords.TryParse(dateKind, out var timestampField))
            {
                return false;
            }

            return TryMapTimestampField(timestampField, out groupId, out propertyKey);
        }
    }
}
