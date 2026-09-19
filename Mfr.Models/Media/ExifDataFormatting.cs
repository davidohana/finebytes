using System.Diagnostics;
using System.Globalization;
using Mfr.Utils;

namespace Mfr.Models.Media
{
    /// <summary>
    /// Formats <see cref="ExifData"/> for formatter tokens and Jpeg Rename List columns.
    /// </summary>
    public static class ExifDataFormatting
    {
        /// <summary>
        /// Formats a semantic EXIF field for token or grid display.
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
                ExifPropertyField.Title => _FormatText(exif.Title),
                ExifPropertyField.Subject => _FormatText(exif.Subject),
                ExifPropertyField.Author => _FormatText(exif.Author),
                ExifPropertyField.Keywords => _FormatText(exif.Keywords),
                ExifPropertyField.Comments => _FormatText(exif.Comments),
                ExifPropertyField.DateTaken => FormatDateTaken(exif),
                ExifPropertyField.Make => _FormatText(exif.Make),
                ExifPropertyField.Model => _FormatText(exif.Model),
                ExifPropertyField.Description => _FormatText(exif.Description),
                ExifPropertyField.Artist => _FormatText(exif.Artist),
                ExifPropertyField.ImageNumber => FormatExtendedTag(exif, source: "ExifSub", name: "37393"),
                ExifPropertyField.UserComment => _FormatText(exif.UserComment),
                ExifPropertyField.Exposure => _FormatText(exif.Exposure),
                ExifPropertyField.FNumber => _FormatText(exif.FNumber),
                ExifPropertyField.Iso => _FormatText(exif.Iso),
                ExifPropertyField.FocalLength => _FormatText(exif.FocalLength),
                ExifPropertyField.FocalLength35mm => _FormatText(exif.FocalLength35mm),
                ExifPropertyField.GpsLatitude => ExifGpsFormatting.FormatCoordinate(exif.GpsLatitude),
                ExifPropertyField.GpsLongitude => ExifGpsFormatting.FormatCoordinate(exif.GpsLongitude),
                _ => throw new UnreachableException(),
            };
        }

        /// <summary>
        /// Formats <see cref="ExifData.DateTaken"/> with general date/time long pattern (with seconds).
        /// </summary>
        /// <param name="exif">Loaded snapshot, or <see langword="null"/> when unset.</param>
        /// <returns>Culture-formatted date/time, or empty when absent.</returns>
        public static string FormatDateTaken(ExifData? exif)
        {
            if (exif?.DateTaken is not { } dateTaken || dateTaken == default)
            {
                return string.Empty;
            }

            return dateTaken.ToString("G", CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Formats <see cref="ExifData.DateTaken"/> with a .NET date format string (formatter tokens).
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
}
