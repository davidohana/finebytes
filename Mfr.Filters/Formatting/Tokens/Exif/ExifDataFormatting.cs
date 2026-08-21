using System.Globalization;
using Mfr.Utils;

namespace Mfr.Filters.Formatting.Tokens.Exif
{
    /// <summary>
    /// Formats <see cref="ExifData"/> fields for formatter tokens.
    /// </summary>
    internal static class ExifDataFormatting
    {
        /// <summary>
        /// Formats a semantic EXIF field for token expansion.
        /// </summary>
        /// <param name="exif">Loaded snapshot, or <see langword="null"/> when unset.</param>
        /// <param name="field">Which field to format.</param>
        /// <returns>Formatted text, or empty when absent.</returns>
        public static string Format(ExifData? exif, ExifPropertyField field)
        {
            if (exif is null)
            {
                return string.Empty;
            }

            return field switch
            {
                ExifPropertyField.Make => _FormatText(exif.Make),
                ExifPropertyField.Model => _FormatText(exif.Model),
                ExifPropertyField.Exposure => _FormatText(exif.Exposure),
                ExifPropertyField.FNumber => _FormatText(exif.FNumber),
                ExifPropertyField.Iso => _FormatText(exif.Iso),
                ExifPropertyField.FocalLength => _FormatText(exif.FocalLength),
                ExifPropertyField.FocalLength35mm => _FormatText(exif.FocalLength35mm),
                _ => string.Empty,
            };
        }

        /// <summary>
        /// Formats <see cref="ExifData.DateTaken"/> with a .NET date format string.
        /// </summary>
        /// <param name="exif">Loaded snapshot, or <see langword="null"/> when unset.</param>
        /// <param name="format">.NET date format string (not validated).</param>
        /// <returns>Formatted date, or empty when <see cref="ExifData.DateTaken"/> is missing.</returns>
        public static string FormatDate(ExifData? exif, string format)
        {
            if (exif?.DateTaken is not { } dateTaken)
            {
                return string.Empty;
            }

            return dateTaken.ToString(format, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Looks up a flattened extended tag by <c>{source}/{name}</c>.
        /// </summary>
        /// <param name="exif">Loaded snapshot, or <see langword="null"/> when unset.</param>
        /// <param name="source">Directory alias (for example <c>Exif</c> or <c>ExifSub</c>).</param>
        /// <param name="name">MetadataExtractor tag name or decimal id.</param>
        /// <returns>Stored description, or empty when missing.</returns>
        public static string FormatExtendedTag(ExifData? exif, string source, string name)
        {
            if (exif is null)
            {
                return string.Empty;
            }

            return exif.TagToDescription.TryGetValue($"{source}/{name}", out var value) ? value : string.Empty;
        }

        private static string _FormatText(string? value)
        {
            return value.IsBlank() ? string.Empty : value;
        }
    }

    /// <summary>
    /// Semantic fields exposed by no-arg <c>exif-*</c> formatter tokens.
    /// </summary>
    internal enum ExifPropertyField
    {
        Make,
        Model,
        Exposure,
        FNumber,
        Iso,
        FocalLength,
        FocalLength35mm,
    }
}
