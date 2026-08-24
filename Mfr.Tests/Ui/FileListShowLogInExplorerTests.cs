using Mfr.App.Ui.Services.FileList;
using Mfr.App.Ui.ViewModels.FileList;
using Serilog.Events;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Tests revealing the session log file from the File List listing-error empty state.
    /// </summary>
    [Collection(SessionLogCollection.Name)]
    public sealed class FileListShowLogInExplorerTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();
        private readonly List<FileListViewModel> _viewModels = [];

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var viewModel in _viewModels)
            {
                viewModel.Dispose();
            }

            LogSession.Shutdown();
            ConfigStore.Load();
            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Verifies Show Log in Explorer selects the active session log file.
        /// </summary>
        [Fact]
        public void ShowLogInExplorer_Reveals_Session_Log_File()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var logDirectoryPath = _tempDirectoryFixture.CreateTempDir();
            var logConfig = new LogConfig { DirectoryPath = logDirectoryPath };
            LogSession.Start(LogEventLevel.Information, logConfig);
            var logFilePath = LogSession.LogFilePath;
            Assert.NotNull(logFilePath);

            var parent = _tempDirectoryFixture.CreateTempDir();
            var deniedFolder = Directory.CreateDirectory(Path.Combine(parent, "Denied")).FullName;
            _DenyDirectoryTraverse(deniedFolder);
            var shell = new RecordingShellOpener();

            try
            {
                var viewModel = new FileListViewModel(
                    NullSystemIconProvider.Instance,
                    parent,
                    shell,
                    NullTextClipboard.Instance
                );
                _viewModels.Add(viewModel);
                viewModel.NavigateTo(deniedFolder);

                Assert.True(viewModel.CanShowLogInExplorer);
                Assert.True(viewModel.ShowLogInExplorerCommand.CanExecute(null));

                viewModel.ShowLogInExplorerCommand.Execute(null);

                Assert.Equal([logFilePath], shell.RevealedPaths);
            }
            finally
            {
                _AllowDirectoryTraverse(deniedFolder);
            }
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private static void _DenyDirectoryTraverse(string directoryPath)
        {
            var directoryInfo = new DirectoryInfo(directoryPath);
            var security = directoryInfo.GetAccessControl();
            security.AddAccessRule(
                new System.Security.AccessControl.FileSystemAccessRule(
                    identity: System.Security.Principal.WindowsIdentity.GetCurrent().Name,
                    fileSystemRights: System.Security.AccessControl.FileSystemRights.ListDirectory
                        | System.Security.AccessControl.FileSystemRights.Traverse,
                    type: System.Security.AccessControl.AccessControlType.Deny
                )
            );
            directoryInfo.SetAccessControl(security);
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private static void _AllowDirectoryTraverse(string directoryPath)
        {
            var directoryInfo = new DirectoryInfo(directoryPath);
            var security = directoryInfo.GetAccessControl();
            security.RemoveAccessRuleAll(
                new System.Security.AccessControl.FileSystemAccessRule(
                    identity: System.Security.Principal.WindowsIdentity.GetCurrent().Name,
                    fileSystemRights: System.Security.AccessControl.FileSystemRights.ListDirectory
                        | System.Security.AccessControl.FileSystemRights.Traverse,
                    type: System.Security.AccessControl.AccessControlType.Deny
                )
            );
            directoryInfo.SetAccessControl(security);
        }

        private sealed class RecordingShellOpener : IFileShellOpener
        {
            public List<string> RevealedPaths { get; } = [];

            public void OpenWithDefaultApp(string path) { }

            public void RevealInFileManager(string path)
            {
                RevealedPaths.Add(path);
            }

            public void OpenFolderInFileManager(string folderPath) { }
        }
    }
}
