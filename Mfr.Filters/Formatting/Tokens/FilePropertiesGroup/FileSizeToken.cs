using System.Globalization;
using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.FilePropertiesGroup
{
    /// <summary>
    /// Resolves the <c>&lt;file-size&gt;</c> token.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Argument shape: <c>unit</c> or <c>unit,decimals</c>. Supported units are <c>0</c>/<c>auto</c> (default),
    /// <c>1</c>/<c>b</c>/<c>bytes</c>, <c>2</c>/<c>kb</c>, <c>3</c>/<c>mb</c>, <c>4</c>/<c>gb</c>.
    /// </para>
    /// </remarks>
    internal sealed class FileSizeToken : IFormatToken
    {
        private const double Kb = 1024;
        private const double Mb = 1024 * 1024;
        private const double Gb = 1024 * 1024 * 1024;

        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["file-size"];

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">Thrown when an unrecognized unit is supplied.</exception>
        public string Resolve(string arg, RenameItem item)
        {
            var parts = arg.Split(',', 2, StringSplitOptions.TrimEntries);
            var unitArg = parts.Length > 0 ? parts[0] : "";
            var decimalArg = parts.Length > 1 ? parts[1] : "";

            var decimals = string.IsNullOrWhiteSpace(decimalArg)
                ? 0
                : int.Parse(decimalArg, CultureInfo.InvariantCulture);
            var bytes = (double)item.Original.FileSize;

            return unitArg.ToLowerInvariant() switch
            {
                "" or "0" or "auto" => _FormatAuto(bytes, decimals),
                "1" or "b" or "bytes" => _Format(bytes, divisor: 1.0, unit: "B", decimals),
                "2" or "kb" => _Format(bytes, divisor: Kb, unit: "KB", decimals),
                "3" or "mb" => _Format(bytes, divisor: Mb, unit: "MB", decimals),
                "4" or "gb" => _Format(bytes, divisor: Gb, unit: "GB", decimals),
                _ => throw new NotSupportedException($"File size unit '{unitArg}' is not supported.")
            };
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
