using Mfr.App.Ui.Services.FileList;
using Mfr.App.Ui.Services.Shell;
using Mfr.App.Ui.ViewModels.FileList;

namespace Mfr.Tests.Ui.FileList
{
    /// <summary>
    /// Tests File List session apply and capture round-trips.
    /// </summary>
    public sealed class FileListSessionTests : IDisposable
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
        /// Verifies apply restores mask, exclude masks, suggestions, and view mode.
        /// </summary>
        [Fact]
        public void ApplySession_Restores_Mask_Exclude_And_Suggestions()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var viewModel = _CreateViewModel(dir);
            var fileList = new SessionStateFileList
            {
                LastOpenedDirectory = dir,
                FileMask = "*.wav",
                ExcludeMasks = ["*.tmp", "*.bak"],
                ExcludeMasksEnabled = true,
                MaskSuggestions = ["*.wav", "*.mp3"],
                ViewMode = FileListViewMode.Tiles,
                ThumbnailSize = ThumbnailSizes.Large,
            };

            viewModel.ApplySession(fileList);

            Assert.Equal("*.wav", viewModel.Mask);
            Assert.True(viewModel.ExcludeMasksEnabled);
            Assert.Equal(["*.tmp", "*.bak"], viewModel.ExcludeMasks);
            Assert.Equal(["*.wav", "*.mp3"], viewModel.MaskSuggestions);
            Assert.Equal(FileListViewMode.Tiles, viewModel.ViewMode);
            Assert.Equal(ThumbnailSizes.Large, viewModel.ThumbnailSize);
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
            viewModel.SetThumbnailSize(ThumbnailSizes.Huge);

            var captured = viewModel.CaptureSession();
            Assert.Equal(dir, captured.LastOpenedDirectory);
            Assert.Equal(FileListViewMode.List, captured.ViewMode);
            Assert.Equal(ThumbnailSizes.Huge, captured.ThumbnailSize);

            var restored = _CreateViewModel(_tempDirectoryFixture.CreateTempDir());
            restored.ApplySession(captured);

            Assert.Equal(captured.FileMask, restored.Mask);
            Assert.Equal(captured.ExcludeMasksEnabled, restored.ExcludeMasksEnabled);
            Assert.Equal(captured.ExcludeMasks, restored.ExcludeMasks);
            Assert.Equal(captured.MaskSuggestions, restored.MaskSuggestions);
            Assert.Equal(captured.ViewMode, restored.ViewMode);
            Assert.Equal(captured.ThumbnailSize, restored.ThumbnailSize);
        }

        /// <summary>
        /// Verifies unset session fields keep File List defaults.
        /// </summary>
        [Fact]
        public void ApplySession_Unset_Fields_Keep_Defaults()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var viewModel = _CreateViewModel(dir);

            viewModel.ApplySession(new SessionStateFileList());

            Assert.Equal("*", viewModel.Mask);
            Assert.False(viewModel.ExcludeMasksEnabled);
            Assert.Equal(FileListViewModel.DefaultExcludeMasks, viewModel.ExcludeMasks);
            Assert.NotEmpty(viewModel.MaskSuggestions);
            Assert.Equal(FileListViewMode.Report, viewModel.ViewMode);
            Assert.Equal(ThumbnailSizes.Default, viewModel.ThumbnailSize);
        }

        /// <summary>
        /// Verifies null session section is a no-op.
        /// </summary>
        [Fact]
        public void ApplySession_Null_Keeps_Defaults()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var viewModel = _CreateViewModel(dir);

            viewModel.ApplySession(null);

            Assert.Equal("*", viewModel.Mask);
            Assert.Equal(FileListViewMode.Report, viewModel.ViewMode);
            Assert.Equal(ThumbnailSizes.Default, viewModel.ThumbnailSize);
        }

        /// <summary>
        /// Verifies an off-step thumbnail size is snapped on restore.
        /// </summary>
        [Fact]
        public void ApplySession_Clamps_ThumbnailSize()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var viewModel = _CreateViewModel(dir);

            viewModel.ApplySession(new SessionStateFileList { ThumbnailSize = 100 });

            Assert.Equal(ThumbnailSizes.Medium, viewModel.ThumbnailSize);
        }

        /// <summary>
        /// Verifies an empty exclude-mask list clears patterns instead of keeping defaults.
        /// </summary>
        [Fact]
        public void ApplySession_Empty_ExcludeMasks_Clears_Patterns()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var viewModel = _CreateViewModel(dir);
            var fileList = new SessionStateFileList
            {
                FileMask = "*.txt",
                ExcludeMasks = [],
                ExcludeMasksEnabled = false,
            };

            viewModel.ApplySession(fileList);

            Assert.Equal("*.txt", viewModel.Mask);
            Assert.Empty(viewModel.ExcludeMasks);
            Assert.False(viewModel.ExcludeMasksEnabled);
        }

        private FileListViewModel _CreateViewModel(string path)
        {
            var viewModel = new FileListViewModel(NullSystemIconProvider.Instance, path, NullFileShellOpener.Instance);
            _viewModels.Add(viewModel);
            return viewModel;
        }
    }
}
