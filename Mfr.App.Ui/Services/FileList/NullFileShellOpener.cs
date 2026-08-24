namespace Mfr.App.Ui.Services.FileList
{
    /// <summary>
    /// No-op shell opener for tests and platforms that do not launch a file manager.
    /// </summary>
    public sealed class NullFileShellOpener : IFileShellOpener
    {
        /// <summary>
        /// Gets the shared instance.
        /// </summary>
        public static NullFileShellOpener Instance { get; } = new();

        /// <inheritdoc />
        public void OpenWithDefaultApp(string path) { }

        /// <inheritdoc />
        public void RevealInFileManager(string path) { }

        /// <inheritdoc />
        public void OpenFolderInFileManager(string folderPath) { }
    }
}
