using System.Globalization;
using Mfr.Models;

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
    /// Argument shape: <c>unit</c> or <c>unit,decimals</c>. Supported units are <c>0</c>/<c>auto</c> (default),
    /// <c>1</c>/<c>b</c>/<c>bytes</c>, <c>2</c>/<c>kb</c>, <c>3</c>/<c>mb</c>, <c>4</c>/<c>gb</c>.
    /// </para>
    /// </remarks>
    internal sealed class FileSizeToken : IFormatToken
    {
        private const double Kb = 1024;
        private const double Mb = 1024 * 1024;
        private const double Gb = 1024 * 1024 * 1024;

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
        public Func<RenameItem, string> Compile(string arg)
        {
            var options = _ParseOptions(arg);
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
                    _ => throw new InvalidOperationException($"Unreachable file size unit '{options.Unit}'.")
                };
            };
        }

        private static Options _ParseOptions(string arg)
        {
            var parts = arg.Split(',', 2, StringSplitOptions.TrimEntries);
            var unitArg = parts.Length > 0 ? parts[0] : "";
            var decimalArg = parts.Length > 1 ? parts[1] : "";

            var decimals = string.IsNullOrWhiteSpace(decimalArg)
                ? 0
                : int.Parse(decimalArg, CultureInfo.InvariantCulture);

            var unitKind = unitArg.ToLowerInvariant() switch
            {
                "" or "0" or "auto" => FileSizeFormatUnitKind.Auto,
                "1" or "b" or "bytes" => FileSizeFormatUnitKind.Bytes,
                "2" or "kb" => FileSizeFormatUnitKind.Kb,
                "3" or "mb" => FileSizeFormatUnitKind.Mb,
                "4" or "gb" => FileSizeFormatUnitKind.Gb,
                _ => throw new NotSupportedException($"File size unit '{unitArg}' is not supported.")
            };

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
