using Mfr.App.Ui.Services.FileList;
using Mfr.App.Ui.Services.Session;
using Mfr.App.Ui.Services.Shell;
using Mfr.App.Ui.ViewModels.FileList;
using Mfr.Models.Config;

namespace Mfr.Tests.Ui.FileList
{
    /// <summary>
    /// Tests File List session snapshot apply and capture round-trips.
    /// </summary>
    public sealed class FileListSessionSnapshotTests : IDisposable
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

            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Verifies apply restores mask, exclude masks, and suggestions from a snapshot.
        /// </summary>
        [Fact]
        public void ApplySession_Restores_Mask_Exclude_And_Suggestions()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var viewModel = _CreateViewModel(dir);
            var snapshot = new FileListSessionSnapshot(
                LastOpenedDirectory: dir,
                FileMask: "*.wav",
                ExcludeMasks: ["*.tmp", "*.bak"],
                ExcludeMasksEnabled: true,
                MaskSuggestions: ["*.wav", "*.mp3"],
                ViewMode: FileListViewMode.Tiles
            );

            viewModel.ApplySession(snapshot);

            Assert.Equal("*.wav", viewModel.Mask);
            Assert.True(viewModel.ExcludeMasksEnabled);
            Assert.Equal(["*.tmp", "*.bak"], viewModel.ExcludeMasks);
            Assert.Equal(["*.wav", "*.mp3"], viewModel.MaskSuggestions);
            Assert.Equal(FileListViewMode.Tiles, viewModel.ViewMode);
        }

        /// <summary>
        /// Verifies capture round-trips mask, exclude, path, and view-mode fields.
        /// </summary>
        [Fact]
        public void CaptureSession_RoundTrips_Mask_Exclude_And_Path()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var viewModel = _CreateViewModel(dir);
            viewModel.Mask = "*.jpg";
            viewModel.ExcludeMasks = ["*.exe"];
            viewModel.ExcludeMasksEnabled = true;
            viewModel.MaskSuggestions.Clear();
            viewModel.MaskSuggestions.Add("*.jpg");
            viewModel.MaskSuggestions.Add("*.png");
            viewModel.SetViewMode(FileListViewMode.List);

            var captured = viewModel.CaptureSession();
            Assert.Equal(dir, captured.LastOpenedDirectory);
            Assert.Equal(FileListViewMode.List, captured.ViewMode);

            var restored = _CreateViewModel(_tempDirectoryFixture.CreateTempDir());
            restored.ApplySession(captured);

            Assert.Equal(captured.FileMask, restored.Mask);
            Assert.Equal(captured.ExcludeMasksEnabled, restored.ExcludeMasksEnabled);
            Assert.Equal(captured.ExcludeMasks, restored.ExcludeMasks);
            Assert.Equal(captured.MaskSuggestions, restored.MaskSuggestions);
            Assert.Equal(captured.ViewMode, restored.ViewMode);
        }

        /// <summary>
        /// Verifies unset snapshot fields keep File List defaults.
        /// </summary>
        [Fact]
        public void ApplySession_Unset_Fields_Keep_Defaults()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var viewModel = _CreateViewModel(dir);
            var snapshot = new FileListSessionSnapshot(
                LastOpenedDirectory: null,
                FileMask: null,
                ExcludeMasks: null,
                ExcludeMasksEnabled: null,
                MaskSuggestions: null,
                ViewMode: null
            );

            viewModel.ApplySession(snapshot);

            Assert.Equal("*", viewModel.Mask);
            Assert.False(viewModel.ExcludeMasksEnabled);
            Assert.Equal(FileListViewModel.DefaultExcludeMasks, viewModel.ExcludeMasks);
            Assert.NotEmpty(viewModel.MaskSuggestions);
            Assert.Equal(FileListViewMode.Report, viewModel.ViewMode);
        }

        /// <summary>
        /// Verifies an empty exclude-mask list clears patterns instead of keeping defaults.
        /// </summary>
        [Fact]
        public void ApplySession_Empty_ExcludeMasks_Clears_Patterns()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var viewModel = _CreateViewModel(dir);
            var snapshot = new FileListSessionSnapshot(
                LastOpenedDirectory: null,
                FileMask: "*.txt",
                ExcludeMasks: [],
                ExcludeMasksEnabled: false,
                MaskSuggestions: null,
                ViewMode: null
            );

            viewModel.ApplySession(snapshot);

            Assert.Equal("*.txt", viewModel.Mask);
            Assert.Empty(viewModel.ExcludeMasks);
            Assert.False(viewModel.ExcludeMasksEnabled);
        }

        /// <summary>
        /// Verifies session view-mode is copied through FromSessionState.
        /// </summary>
        [Fact]
        public void FromSessionState_Copies_ViewMode()
        {
            var session = new SessionState
            {
                FileList = new SessionStateFileList { ViewMode = FileListViewMode.Thumbnails },
            };

            var snapshot = FileListSessionSnapshot.FromSessionState(session);

            Assert.Equal(FileListViewMode.Thumbnails, snapshot.ViewMode);
        }

        private FileListViewModel _CreateViewModel(string path)
        {
            var viewModel = new FileListViewModel(NullSystemIconProvider.Instance, path, NullFileShellOpener.Instance);
            _viewModels.Add(viewModel);
            return viewModel;
        }
    }
}
