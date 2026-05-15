using System.Diagnostics;
using System.Globalization;
using Mfr.Models;
using Mfr.Utils;

namespace Mfr.Filters.Formatting.Tokens.FileProperties
{
    /// <summary>
    /// Resolves the <c>&lt;file-date:…&gt;</c> token.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Supply <c>format,date-kind</c>: a .NET date format string, a comma, then <c>creation</c>, <c>lastWrite</c>,
    /// or <c>lastAccess</c> (case-insensitive, aligned with preset <c>timestampField</c> JSON names).
    /// The comma before <c>date-kind</c> is the last comma in the argument so format patterns may contain commas.
    /// </para>
    /// </remarks>
    internal sealed class FileDateToken : IFormatToken
    {
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
        /// <exception cref="ArgumentException">Thrown when the template argument is malformed or <c>date-kind</c> is not recognized.</exception>
        /// <exception cref="UnreachableException">Thrown when an unexpected enum value appears at runtime (should not happen for parsed options).</exception>
        public Formatter Compile(string arg)
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
            Require.That(
                !string.IsNullOrWhiteSpace(arg),
                $"{tokenDisplayName} requires arguments: a .NET format string and date-kind separated by a comma " +
                    "(for example 'dd-MM-yyyy,creation').",
                nameof(arg));

            var trimmed = arg.Trim();

            var lastComma = trimmed.LastIndexOf(',');
            Require.That(
                lastComma >= 0,
                $"{tokenDisplayName} requires a .NET format string and date-kind separated by a comma " +
                    "(for example 'dd-MM-yyyy,creation').",
                nameof(arg));

            var formatPart = trimmed[..lastComma].Trim();
            var dateKindPart = trimmed[(lastComma + 1)..].Trim();

            Require.That(
                formatPart.Length > 0,
                $"{tokenDisplayName} format string must not be empty (expected 'format,date-kind').",
                nameof(arg));

            Require.That(
                dateKindPart.Length > 0,
                $"{tokenDisplayName} date-kind must not be empty after the comma (expected {FormatOptionsParsing.FormatExpectedKeywords(_keywordToTimestampField.Keys)}).",
                nameof(arg));

            if (!_TryParseFileDateKindKeyword(dateKindPart, out var timestampField))
                throw new ArgumentException(
                    $"{tokenDisplayName} invalid date-kind '{dateKindPart}' " +
                        $"(expected {FormatOptionsParsing.FormatExpectedKeywords(_keywordToTimestampField.Keys)}).",
                    nameof(arg));

            return new Options(Format: formatPart, TimestampField: timestampField);
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
