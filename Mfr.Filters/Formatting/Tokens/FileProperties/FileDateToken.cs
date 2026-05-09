using System.Diagnostics;
using System.Globalization;
using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.FileProperties
{
    /// <summary>
    /// Resolves the <c>&lt;file-date&gt;</c> token.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Argument shape: <c>format</c>; <c>format,kind</c>; or a lone kind keyword (<c>creation</c>,
    /// <c>lastWrite</c>, <c>lastAccess</c>, case-insensitive — matching preset <c>timestampField</c> names).
    /// Default format when none supplied is <c>dd-MM-yyyy</c>.
    /// </para>
    /// </remarks>
    internal sealed class FileDateToken : IFormatToken
    {
        private const string DefaultFormat = "dd-MM-yyyy";

        /// <summary>
        /// Case-insensitive keywords aligned with preset <c>timestampField</c> JSON names (<see cref="TimestampField"/>).
        /// </summary>
        private static readonly Dictionary<string, TimestampField> _keywordToTimestampField = new(
            StringComparer.OrdinalIgnoreCase)
        {
            ["creation"] = TimestampField.Creation,
            ["lastWrite"] = TimestampField.LastWrite,
            ["lastAccess"] = TimestampField.LastAccess,
        };

        /// <summary>
        /// Parsed arguments for <c>&lt;file-date&gt;</c>.
        /// </summary>
        /// <param name="Format">.NET date format string.</param>
        /// <param name="TimestampField">Filesystem timestamp field to format.</param>
        private sealed record Options(string Format, TimestampField TimestampField);

        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["file-date"];

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">Thrown when an unsupported date kind is supplied in the template argument.</exception>
        /// <exception cref="UnreachableException">Thrown when an unexpected enum value appears at runtime (should not happen for parsed options).</exception>
        public Func<RenameItem, string> Compile(string arg)
        {
            var options = _ParseOptions(FormatOptionsParsing.TokenDisplayName(this), arg);
            return item =>
            {
                var date = options.TimestampField switch
                {
                    TimestampField.Creation => item.Original.CreationTime,
                    TimestampField.LastWrite => item.Original.LastWriteTime,
                    TimestampField.LastAccess => item.Original.LastAccessTime,
                    _ => throw new UnreachableException(),
                };
                return date.ToString(options.Format, CultureInfo.InvariantCulture);
            };
        }

        private static Options _ParseOptions(string tokenDisplayName, string arg)
        {
            if (string.IsNullOrWhiteSpace(arg))
                return new Options(Format: DefaultFormat, TimestampField: TimestampField.Creation);

            var trimmed = arg.Trim();

            // Without a comma we cannot split "format,kind". Treat the whole segment as either a lone
            // TimestampField keyword (<file-date:lastWrite> → default format + that field) or as a .NET
            // format pattern (<file-date:yyyy> → that pattern + creation time).
            if (!trimmed.Contains(','))
            {
                if (_TryParseFileDateKindKeyword(trimmed, out var fieldOnly))
                    return new Options(Format: DefaultFormat, TimestampField: fieldOnly);
                return new Options(Format: trimmed, TimestampField: TimestampField.Creation);
            }

            var lastComma = trimmed.LastIndexOf(',');
            var formatPart = trimmed[..lastComma];
            var dateKindPart = trimmed[(lastComma + 1)..].Trim();
            var format = string.IsNullOrWhiteSpace(formatPart) ? DefaultFormat : formatPart;
            if (string.IsNullOrEmpty(dateKindPart))
                return new Options(Format: format, TimestampField: TimestampField.Creation);

            if (!_TryParseFileDateKindKeyword(dateKindPart, out var timestampField))
                throw new NotSupportedException(
                    $"{tokenDisplayName} invalid timestamp keyword '{dateKindPart}' " +
                    $"(expected {FormatOptionsParsing.FormatExpectedKeywords(_keywordToTimestampField.Keys)}).");

            return new Options(Format: format, TimestampField: timestampField);
        }

        /// <summary>
        /// Maps a case-insensitive <see cref="TimestampField"/> keyword (preset <c>timestampField</c> strings).
        /// </summary>
        private static bool _TryParseFileDateKindKeyword(string raw, out TimestampField timestampField)
        {
            timestampField = default;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            return _keywordToTimestampField.TryGetValue(raw.Trim(), out timestampField);
        }
    }
}
