using Avalonia.Media;

namespace Mfr.App.Ui.ViewModels
{
    /// <summary>
    /// One item in the File Explorer listing.
    /// </summary>
    public sealed class FileListEntry
    {
        /// <summary>
        /// Gets the file or folder name, or the drive name.
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Gets the full filesystem path for this row.
        /// </summary>
        public required string FullPath { get; init; }

        /// <summary>
        /// Gets whether this row is a directory or drive that can be opened.
        /// </summary>
        public required bool IsDirectory { get; init; }

        /// <summary>
        /// Gets the This PC sort group: volumes before known folders. Ignored in ordinary folders.
        /// </summary>
        public int ListingGroup { get; init; }

        /// <summary>
        /// Gets the icon or thumbnail for the current view mode, or <see langword="null"/> when none is available.
        /// </summary>
        public IImage? Icon { get; init; }

        /// <summary>
        /// Gets extra text for Tiles (type and size), or empty in other view modes.
        /// </summary>
        public string Details { get; init; } = string.Empty;

        /// <summary>
        /// Gets the Explorer-style type label, such as <c>File folder</c> or <c>TXT File</c>.
        /// </summary>
        public string Type { get; init; } = string.Empty;

        /// <summary>
        /// Gets the formatted last-write time, or empty when unknown.
        /// </summary>
        public string DateModifiedDisplay { get; init; } = string.Empty;

        /// <summary>
        /// Gets the formatted size, or empty for folders and when unknown.
        /// </summary>
        public string SizeDisplay { get; init; } = string.Empty;

        /// <summary>
        /// Gets the last-write time used when sorting the Date modified column.
        /// </summary>
        public DateTime? LastWriteTime { get; init; }

        /// <summary>
        /// Gets the file length in bytes used when sorting the Size column.
        /// </summary>
        public long? Length { get; init; }
    }
}
