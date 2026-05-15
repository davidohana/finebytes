using System.Diagnostics;
using System.Globalization;

namespace Mfr.Filters.Formatting.Tokens.FileProperties
{
    /// <summary>
    /// Unit selector for <c>&lt;file-size&gt;</c>.
    /// </summary>
    internal enum FileSizeFormatUnitKind
    {
        Auto,
        Bytes,
        Kb,
        Mb,
        Gb
    }

    /// <summary>
    /// Resolves the <c>&lt;file-size&gt;</c> token.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Argument shape: <c>unit</c> or <c>unit,decimals</c>. Supported units are <c>(omit)</c>/<c>auto</c> (default),
    /// <c>b</c>/<c>bytes</c>, <c>kb</c>, <c>mb</c>, <c>gb</c> (case-insensitive).
    /// </para>
    /// </remarks>
    internal sealed class FileSizeToken : IFormatToken
    {
        private const double Kb = 1024;
        private const double Mb = 1024 * 1024;
        private const double Gb = 1024 * 1024 * 1024;

        /// <summary>
        /// Unit keywords for the first argument segment (case-insensitive).
        /// </summary>
        private static readonly Dictionary<string, FileSizeFormatUnitKind> _unitKeywordToKind = new(StringComparer.OrdinalIgnoreCase)
        {
            [""] = FileSizeFormatUnitKind.Auto,
            ["auto"] = FileSizeFormatUnitKind.Auto,
            ["b"] = FileSizeFormatUnitKind.Bytes,
            ["bytes"] = FileSizeFormatUnitKind.Bytes,
            ["kb"] = FileSizeFormatUnitKind.Kb,
            ["mb"] = FileSizeFormatUnitKind.Mb,
            ["gb"] = FileSizeFormatUnitKind.Gb,
        };

        /// <summary>
        /// Maps unit dictionary keys to user-visible hint strings (empty segment → <c>(omit)</c>).
        /// </summary>
        private static IEnumerable<string> _UnitKeywordsForErrorHint()
        {
            foreach (var key in _unitKeywordToKind.Keys)
                yield return key.Length == 0 ? "(omit)" : key;
        }

        /// <summary>
        /// Parsed arguments for <c>&lt;file-size&gt;</c>.
        /// </summary>
        /// <param name="Unit">Fixed unit or auto-scaled output.</param>
        /// <param name="Decimals">Fractional digits for formatted values.</param>
        private sealed record Options(FileSizeFormatUnitKind Unit, int Decimals);

        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["file-size"];

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">Thrown when an unrecognized unit is supplied.</exception>
        /// <exception cref="UnreachableException">Thrown when an unexpected unit enum value appears at runtime.</exception>
        public Formatter Compile(string tokenArgs)
        {
            var options = _ParseOptions(FormatOptionsParsing.TokenDisplayName(this), tokenArgs);
            return item =>
            {
                var bytes = (double)item.Original.FileSize;
                return options.Unit switch
                {
                    FileSizeFormatUnitKind.Auto => _FormatAuto(bytes, options.Decimals),
                    FileSizeFormatUnitKind.Bytes => _Format(bytes, divisor: 1.0, unit: "B", options.Decimals),
                    FileSizeFormatUnitKind.Kb => _Format(bytes, divisor: Kb, unit: "KB", options.Decimals),
                    FileSizeFormatUnitKind.Mb => _Format(bytes, divisor: Mb, unit: "MB", options.Decimals),
                    FileSizeFormatUnitKind.Gb => _Format(bytes, divisor: Gb, unit: "GB", options.Decimals),
                    _ => throw new UnreachableException()
                };
            };
        }

        private static Options _ParseOptions(string tokenDisplayName, string tokenArgs)
        {
            var parts = tokenArgs.Split(',', 2, StringSplitOptions.TrimEntries);
            var unitArg = parts.Length > 0 ? parts[0] : "";
            var decimalArg = parts.Length > 1 ? parts[1] : "";

            var decimals = string.IsNullOrWhiteSpace(decimalArg)
                ? 0
                : int.Parse(decimalArg, CultureInfo.InvariantCulture);

            if (!_unitKeywordToKind.TryGetValue(unitArg, out var unitKind))
                throw new NotSupportedException(
                    $"{tokenDisplayName} unit '{unitArg}' is not supported " +
                    $"(expected {FormatOptionsParsing.FormatExpectedKeywords(_UnitKeywordsForErrorHint())}).");

            return new Options(Unit: unitKind, Decimals: decimals);
        }

        private static string _FormatAuto(double bytes, int decimals)
        {
            if (bytes >= Gb)
                return _Format(bytes, Gb, "GB", decimals);
            if (bytes >= Mb)
                return _Format(bytes, Mb, "MB", decimals);
            if (bytes >= Kb)
                return _Format(bytes, Kb, "KB", decimals);
            return _Format(bytes, 1.0, "B", decimals);
        }

        private static string _Format(double bytes, double divisor, string unit, int decimals)
        {
            var value = bytes / divisor;
            var fmt = $"F{Math.Max(0, decimals)}";
            return $"{value.ToString(fmt, CultureInfo.InvariantCulture)} {unit}";
        }
    }
}
