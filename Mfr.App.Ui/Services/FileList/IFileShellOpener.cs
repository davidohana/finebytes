namespace Mfr.App.Ui.Services.FileList
{
    /// <summary>
    /// Opens filesystem paths with the OS shell (default app or file manager).
    /// </summary>
    public interface IFileShellOpener
    {
        /// <summary>
        /// Opens <paramref name="path"/> with the default application for its type.
        /// </summary>
        /// <param name="path">File or folder path.</param>
        void OpenWithDefaultApp(string path);

        /// <summary>
        /// Reveals <paramref name="path"/> in the file manager, selecting it when possible.
        /// </summary>
        /// <param name="path">File or folder path to highlight.</param>
        void RevealInFileManager(string path);

        /// <summary>
        /// Opens <paramref name="folderPath"/> as a folder in the file manager.
        /// </summary>
        /// <param name="folderPath">Directory path to open.</param>
        void OpenFolderInFileManager(string folderPath);
    }
}
