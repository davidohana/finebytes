using Avalonia.Media;

namespace Mfr.App.Ui.ViewModels
{
    /// <summary>
    /// One row in the File Explorer Name grid.
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
        /// Gets the small system icon, or <see langword="null"/> when none is available.
        /// </summary>
        public IImage? Icon { get; init; }
    }
}
