using System.Globalization;
using Mfr.Models.Media;
using Mfr.Models.Rename;
using Mfr.Utils;

namespace Mfr.Models.RenameList
{
    /// <summary>
    /// Shared display formatting for Rename List catalog field resolvers.
    /// </summary>
    internal static class RenameListFieldDisplay
    {
        /// <summary>
        /// Formats a filesystem timestamp for grid display (general date/time short).
        /// </summary>
        /// <param name="value">Timestamp from scan or preview metadata.</param>
        /// <returns>Culture-formatted date/time, or empty when unset.</returns>
        internal static string FormatFileDate(DateTime value)
        {
            if (value == default)
            {
                return string.Empty;
            }

            return value.ToString("g", CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Formats file size in bytes (MFR7 <c>ExtendedPG.Size</c> display).
        /// </summary>
        /// <param name="bytes">Size in bytes.</param>
        /// <returns>Invariant digit string.</returns>
        internal static string FormatFileSizeBytes(long bytes)
        {
            return bytes.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Formats the non-recursive file count for a folder item or its parent (MFR7 <c>FileCount</c>).
        /// </summary>
        /// <param name="meta">Scan metadata for the row.</param>
        /// <returns>Invariant digit string, or empty when the directory does not exist.</returns>
        internal static string FormatFolderFileCount(FileMeta meta)
        {
            var directoryPath = meta.Attributes.IsDirectory() ? meta.FullPath : meta.DirectoryPath;
            if (!Directory.Exists(directoryPath))
            {
                return string.Empty;
            }

            return Directory.GetFiles(directoryPath).Length.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Formats the MFR7 RAHS attribute string for grid display.
        /// </summary>
        /// <param name="attributes">Filesystem attributes from scan metadata.</param>
        /// <returns>Four-character <c>R/A/H/S</c> or dash flags.</returns>
        internal static string FormatAttributes(FileAttributes attributes)
        {
            return string.Concat(
                _FormatAttributeFlag(attributes, FileAttributes.ReadOnly, 'R'),
                _FormatAttributeFlag(attributes, FileAttributes.Archive, 'A'),
                _FormatAttributeFlag(attributes, FileAttributes.Hidden, 'H'),
                _FormatAttributeFlag(attributes, FileAttributes.System, 'S')
            );
        }

        /// <summary>
        /// Formats a positive integer property, or empty when zero.
        /// </summary>
        /// <param name="value">Property value; zero means absent.</param>
        /// <returns>Invariant digits, or empty.</returns>
        internal static string FormatPositiveInt(int value)
        {
            if (value == 0)
            {
                return string.Empty;
            }

            return value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Formats horizontal or vertical DPI, or empty when unset.
        /// </summary>
        /// <param name="value">Dots per inch; zero or negative means absent.</param>
        /// <returns>Rounded or general-format DPI text.</returns>
        internal static string FormatDpi(double value)
        {
            if (value <= 0)
            {
                return string.Empty;
            }

            var rounded = Math.Round(value);
            if (Math.Abs(value - rounded) < 1e-9)
            {
                return rounded.ToString(CultureInfo.InvariantCulture);
            }

            return value.ToString("G", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Formats optional text from image or EXIF snapshots.
        /// </summary>
        /// <param name="value">Stored text, or <see langword="null"/> when absent.</param>
        /// <returns>Trimmed text, or empty.</returns>
        internal static string FormatOptionalText(string? value)
        {
            return value.IsBlank() ? string.Empty : value;
        }

        /// <summary>
        /// Formats <see cref="ExifData.DateTaken"/> with general date/time short pattern.
        /// </summary>
        /// <param name="exif">Loaded EXIF snapshot, or <see langword="null"/>.</param>
        /// <returns>Formatted date/time, or empty when absent.</returns>
        internal static string FormatExifDateTaken(ExifData? exif)
        {
            return exif?.DateTaken is { } dateTaken ? FormatFileDate(dateTaken) : string.Empty;
        }

        /// <summary>
        /// Looks up a flattened extended EXIF tag description.
        /// </summary>
        /// <param name="exif">Loaded EXIF snapshot, or <see langword="null"/>.</param>
        /// <param name="source">Directory alias (for example <c>ExifSub</c>).</param>
        /// <param name="tagId">Decimal MetadataExtractor tag id.</param>
        /// <returns>Stored description, or empty when missing.</returns>
        internal static string FormatExifTagId(ExifData? exif, string source, int tagId)
        {
            if (exif is null)
            {
                return string.Empty;
            }

            return exif.TagToDescription.TryGetValue($"{source}/{tagId}", out var value) ? value : string.Empty;
        }

        /// <summary>
        /// Returns the first semicolon-delimited segment, trimmed.
        /// </summary>
        /// <param name="joined">Joined multi-value string.</param>
        /// <returns>First segment, or empty when unset.</returns>
        internal static string FirstDelimitedSegment(string? joined)
        {
            if (joined.IsBlank())
            {
                return string.Empty;
            }

            var firstSeparator = joined.IndexOf(';');
            var segment = firstSeparator < 0 ? joined : joined[..firstSeparator];
            return segment.Trim();
        }

        private static char _FormatAttributeFlag(FileAttributes attributes, FileAttributes flag, char letter)
        {
            return attributes.HasFlag(flag) ? letter : '-';
        }
    }
}
