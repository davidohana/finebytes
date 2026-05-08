using System.Globalization;
using System.Text.RegularExpressions;
using Mfr.Models;
using Mfr.Utils;

namespace Mfr.Filters.Formatting
{
    /// <summary>
    /// Resolves formatter tokens in template text.
    /// </summary>
    internal static partial class FormatStringResolver
    {
        /// <summary>
        /// Resolves all formatter tokens inside <paramref name="template"/>.
        /// </summary>
        /// <param name="template">Template text that may contain tokens.</param>
        /// <param name="item">Rename item used to resolve item-aware tokens.</param>
        /// <returns>Template text with tokens resolved.</returns>
        internal static string ResolveTemplate(string template, RenameItem item)
        {
            return _TokenRegex().Replace(template, m => _ResolveToken(m.Groups[1].Value, item));
        }

        private static string _ResolveToken(string tokenInner, RenameItem item)
        {
            var parts = tokenInner.Split(':', 2);
            var name = parts[0];
            var arg = parts.Length == 2 ? parts[1] : "";

            return name switch
            {
                "file-name" => item.Original.Prefix,
                "file-extension" or "ext" => item.Original.Extension,
                "full-name" => item.Original.Prefix + item.Original.Extension,
                "parent-folder" => _ResolveParentFolderToken(arg, item),
                "full-path" => item.Original.FullPath,
                "file-date" => _ResolveFileDateToken(arg, item),
                "label" => _ResolveLabelToken(item),
                "drive-letter" => _ResolveDriveLetterToken(item),
                "file-count" => _ResolveFileCountToken(item),
                "file-size" => _ResolveFileSizeToken(arg, item),
                "now" => string.IsNullOrWhiteSpace(arg) ? DateTimeOffset.UtcNow.ToString("o") : DateTimeOffset.UtcNow.ToString(arg),
                "counter" => _ResolveCounterToken(arg, item),
                _ => throw new NotSupportedException($"Phase 1 formatter token '{name}' is not supported.")
            };
        }

        private static string _ResolveParentFolderToken(string arg, RenameItem item)
        {
            var level = string.IsNullOrWhiteSpace(arg) ? 1 : int.Parse(arg, CultureInfo.InvariantCulture);
            try
            {
                return DirectoryPathAncestor.GetSegmentName(item.Original.DirectoryPath, level);
            }
            catch (InvalidOperationException)
            {
                return string.Empty;
            }
        }

        private static string _ResolveFileDateToken(string arg, RenameItem item)
        {
            const string defaultFormat = "dd-MM-yyyy";

            string format;
            int dateType;

            if (string.IsNullOrWhiteSpace(arg))
            {
                format = defaultFormat;
                dateType = 0;
            }
            else
            {
                var lastComma = arg.LastIndexOf(',');
                if (lastComma < 0)
                {
                    format = arg;
                    dateType = 0;
                }
                else
                {
                    format = arg[..lastComma];
                    var dateTypePart = arg[(lastComma + 1)..].Trim();
                    dateType = string.IsNullOrEmpty(dateTypePart) ? 0 : int.Parse(dateTypePart, CultureInfo.InvariantCulture);
                }
            }

            if (string.IsNullOrWhiteSpace(format))
                format = defaultFormat;

            var date = dateType switch
            {
                0 => item.Original.CreationTime,
                1 => item.Original.LastWriteTime,
                2 => item.Original.LastAccessTime,
                _ => throw new NotSupportedException($"File date type '{dateType}' is not supported.")
            };

            return date.ToString(format, CultureInfo.InvariantCulture);
        }

        private static string _ResolveLabelToken(RenameItem item)
        {
            var root = Path.GetPathRoot(item.Original.DirectoryPath);
            if (string.IsNullOrEmpty(root))
                return string.Empty;
            return new DriveInfo(root).VolumeLabel;
        }

        private static string _ResolveDriveLetterToken(RenameItem item)
        {
            var root = Path.GetPathRoot(item.Original.DirectoryPath) ?? string.Empty;
            if (root.StartsWith(@"\\", StringComparison.Ordinal))
                return "$";
            return root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static string _ResolveFileCountToken(RenameItem item)
        {
            var dir = item.Original.DirectoryPath;
            if (!Directory.Exists(dir))
                return string.Empty;
            return Directory.GetFileSystemEntries(dir).Length.ToString(CultureInfo.InvariantCulture);
        }

        private static string _ResolveFileSizeToken(string arg, RenameItem item)
        {
            var parts = arg.Split(',', 2, StringSplitOptions.TrimEntries);
            var unitArg = parts.Length > 0 ? parts[0] : "";
            var decimalArg = parts.Length > 1 ? parts[1] : "";

            var decimals = string.IsNullOrWhiteSpace(decimalArg) ? 0 : int.Parse(decimalArg, CultureInfo.InvariantCulture);
            var bytes = (double)item.Original.FileSize;

            return unitArg.ToLowerInvariant() switch
            {
                "" or "0" or "auto" => _FormatSizeAuto(bytes, decimals),
                "1" or "b" or "bytes" => _FormatSize(bytes, divisor: 1.0, unit: "B", decimals),
                "2" or "kb" => _FormatSize(bytes, divisor: 1024.0, unit: "KB", decimals),
                "3" or "mb" => _FormatSize(bytes, divisor: 1024.0 * 1024, unit: "MB", decimals),
                "4" or "gb" => _FormatSize(bytes, divisor: 1024.0 * 1024 * 1024, unit: "GB", decimals),
                _ => throw new NotSupportedException($"File size unit '{unitArg}' is not supported.")
            };
        }

        private static string _FormatSizeAuto(double bytes, int decimals)
        {
            const double kb = 1024;
            const double mb = 1024 * 1024;
            const double gb = 1024 * 1024 * 1024;

            if (bytes >= gb)
                return _FormatSize(bytes, gb, "GB", decimals);
            if (bytes >= mb)
                return _FormatSize(bytes, mb, "MB", decimals);
            if (bytes >= kb)
                return _FormatSize(bytes, kb, "KB", decimals);
            return _FormatSize(bytes, 1.0, "B", decimals);
        }

        private static string _FormatSize(double bytes, double divisor, string unit, int decimals)
        {
            var value = bytes / divisor;
            var fmt = $"F{Math.Max(0, decimals)}";
            return $"{value.ToString(fmt, CultureInfo.InvariantCulture)} {unit}";
        }

        private static string _ResolveCounterToken(string arg, RenameItem item)
        {
            var parts = arg.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 5)
                throw new InvalidOperationException($"Invalid counter token arg '{arg}'. Expected 5 comma-separated params.");

            var start = int.Parse(parts[0], CultureInfo.InvariantCulture);
            var step = int.Parse(parts[1], CultureInfo.InvariantCulture);
            var reset = int.Parse(parts[2], CultureInfo.InvariantCulture);
            var width = int.Parse(parts[3], CultureInfo.InvariantCulture);
            var pad = int.Parse(parts[4], CultureInfo.InvariantCulture);

            var n = reset == 1 ? item.Original.InFolderIndex : item.Original.GlobalIndex;
            var value = start + ((long)step * n);
            var raw = value.ToString(CultureInfo.InvariantCulture);
            if (width <= 0)
                return raw;

            var padChar = pad == 0 ? '0' : ' ';
            return raw.PadLeft(width, padChar);
        }

        [GeneratedRegex(@"<([^<>]+)>", RegexOptions.Compiled)]
        private static partial Regex _TokenRegex();
    }
}
