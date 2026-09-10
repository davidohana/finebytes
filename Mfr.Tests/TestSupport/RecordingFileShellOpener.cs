using Mfr.App.Ui.Services.Shell;

namespace Mfr.Tests.TestSupport
{
    /// <summary>
    /// Records <see cref="IFileShellOpener"/> calls for UI tests.
    /// </summary>
    public sealed class RecordingFileShellOpener : IFileShellOpener
    {
        /// <summary>
        /// Gets paths passed to <see cref="OpenWithDefaultApp"/>.
        /// </summary>
        public List<string> OpenedWithDefaultApp { get; } = [];

        /// <summary>
        /// Gets paths passed to <see cref="RevealInFileManager"/>.
        /// </summary>
        public List<string> RevealedInFileManager { get; } = [];

        /// <summary>
        /// Gets folder paths passed to <see cref="OpenFolderInFileManager"/>.
        /// </summary>
        public List<string> OpenedFolders { get; } = [];

        /// <summary>
        /// Gets paths passed to <see cref="ShowProperties"/>.
        /// </summary>
        public List<string> ShownProperties { get; } = [];

        /// <inheritdoc />
        public void OpenWithDefaultApp(string path)
        {
            OpenedWithDefaultApp.Add(path);
        }

        /// <inheritdoc />
        public void RevealInFileManager(string path)
        {
            RevealedInFileManager.Add(path);
        }

        /// <inheritdoc />
        public void OpenFolderInFileManager(string folderPath)
        {
            OpenedFolders.Add(folderPath);
        }

        /// <inheritdoc />
        public void ShowProperties(string path)
        {
            ShownProperties.Add(path);
        }
    }
}
