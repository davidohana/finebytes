using Avalonia.Media;

namespace Mfr.App.Ui.Services
{
    /// <summary>
    /// Supplies filesystem icons for the File Explorer pane.
    /// </summary>
    public interface ISystemIconProvider
    {
        /// <summary>
        /// Returns a shell icon for a filesystem entry, or <see langword="null"/> when none is available.
        /// </summary>
        /// <param name="path">Full path of the file, folder, or drive.</param>
        /// <param name="isDirectory">Whether <paramref name="path"/> is a directory or drive.</param>
        /// <param name="size">Small or large shell icon.</param>
        /// <returns>An image to show for the entry, or <see langword="null"/>.</returns>
        IImage? GetIcon(string path, bool isDirectory, ShellIconSize size);
    }
}
