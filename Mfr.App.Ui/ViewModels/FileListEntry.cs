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
        /// Gets the icon or thumbnail for the current view mode, or <see langword="null"/> when none is available.
        /// </summary>
        public IImage? Icon { get; init; }

        /// <summary>
        /// Gets extra text for Tiles (type and size), or empty in other view modes.
        /// </summary>
        public string Details { get; init; } = string.Empty;
    }
}
