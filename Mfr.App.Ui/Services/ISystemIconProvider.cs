using Avalonia.Media;

namespace Mfr.App.Ui.Services
{
    /// <summary>
    /// Supplies small filesystem icons for the File Explorer grid.
    /// </summary>
    public interface ISystemIconProvider
    {
        /// <summary>
        /// Returns a small icon for a filesystem entry, or <see langword="null"/> when none is available.
        /// </summary>
        /// <param name="path">Full path of the file, folder, or drive.</param>
        /// <param name="isDirectory">Whether <paramref name="path"/> is a directory or drive.</param>
        /// <returns>An image to show beside the name, or <see langword="null"/>.</returns>
        IImage? GetSmallIcon(string path, bool isDirectory);
    }
}
