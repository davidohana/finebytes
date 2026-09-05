using System.Globalization;

namespace Mfr.App.Ui.Services.FileList
{
    /// <summary>
    /// Explorer-style type, size, date, and tile-detail text for File List rows.
    /// </summary>
    internal static class FileListEntryDisplay
    {
        /// <summary>
        /// Tile subtitle: type on the first line, size on the second for files.
        /// </summary>
        /// <param name="item">Listed row to describe.</param>
        /// <returns>Type, or type plus formatted size for a file with a known length.</returns>
        public static string FormatDetails(FileListListedItem item)
        {
            var typeLabel = TypeLabel(item);
            if (item.IsDirectory || item.Length is null)
            {
                return typeLabel;
            }

            return typeLabel + "\n" + FormatSize(item.Length.Value);
        }

        /// <summary>
        /// Explorer-style type label such as <c>File folder</c> or <c>TXT File</c>.
        /// </summary>
        /// <param name="item">Listed row to describe.</param>
        /// <returns>Type text for the Type column and tile details.</returns>
        public static string TypeLabel(FileListListedItem item)
        {
            if (FileListPath.IsNetworkPath(item.Path))
            {
                return "Network location";
            }

            if (item.IsDirectory)
            {
                return "File folder";
            }

            var extension = Path.GetExtension(item.Name);
            if (string.IsNullOrEmpty(extension))
            {
                return "File";
            }

            return extension.TrimStart('.').ToUpperInvariant() + " File";
        }

        /// <summary>
        /// Formats <paramref name="lastWriteTime"/> for the Date modified column.
        /// </summary>
        /// <param name="lastWriteTime">Last write time, or <see langword="null"/> when unknown.</param>
        /// <returns>A general date/time long string (with seconds), or empty when <paramref name="lastWriteTime"/> is null.</returns>
        public static string FormatDate(DateTime? lastWriteTime)
        {
            if (lastWriteTime is null)
            {
                return string.Empty;
            }

            return lastWriteTime.Value.ToString("G", CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Formats <paramref name="bytes"/> for the Size column.
        /// </summary>
        /// <param name="bytes">File length in bytes.</param>
        /// <returns>A KB/MB/GB label, or a byte count below 1 KB.</returns>
        public static string FormatSize(long bytes)
        {
            const double kb = 1024;
            const double mb = kb * 1024;
            const double gb = mb * 1024;

            if (bytes >= gb)
            {
                return (bytes / gb).ToString("0.#", CultureInfo.InvariantCulture) + " GB";
            }

            if (bytes >= mb)
            {
                return (bytes / mb).ToString("0.#", CultureInfo.InvariantCulture) + " MB";
            }

            if (bytes >= kb)
            {
                return (bytes / kb).ToString("0.#", CultureInfo.InvariantCulture) + " KB";
            }

            return bytes.ToString(CultureInfo.InvariantCulture) + " B";
        }
    }
}
