using Mfr.App.Ui.Services;
using Mfr.App.Ui.ViewModels;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Tests File Explorer listing, mask, and navigation in <see cref="FileListViewModel"/>.
    /// </summary>
    public sealed class FileListViewModelTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Verifies folders are listed before files, each group in name order.
        /// </summary>
        [Fact]
        public void Lists_Folders_Before_Files()
        {
            var dir = _CreateTree();
            var viewModel = _CreateViewModel(dir);

            Assert.Equal(["zeta-folder", "alpha.txt", "beta.md"], _Names(viewModel));
            Assert.True(viewModel.Entries[0].IsDirectory);
            Assert.False(viewModel.Entries[1].IsDirectory);
        }

        /// <summary>
        /// Verifies the include mask hides non-matching files but still lists folders.
        /// </summary>
        [Fact]
        public void Mask_Filters_Files_And_Keeps_Folders()
        {
            var dir = _CreateTree();
            var viewModel = _CreateViewModel(dir);

            viewModel.Mask = "*.txt";

            Assert.Equal(["zeta-folder", "alpha.txt"], _Names(viewModel));
        }

        /// <summary>
        /// Verifies exclude masks hide matching files from the listing.
        /// </summary>
        [Fact]
        public void ExcludeMasks_Hide_Matching_Files()
        {
            var dir = _CreateTree();
            var viewModel = _CreateViewModel(dir);

            viewModel.ExcludeMasks = "*.txt;*.bak";

            Assert.Equal(["zeta-folder", "beta.md"], _Names(viewModel));
        }

        /// <summary>
        /// Verifies navigating into a folder and going up restores the parent listing.
        /// </summary>
        [Fact]
        public void Navigate_Into_Folder_Then_GoUp()
        {
            var dir = _CreateTree();
            var nested = Path.Combine(dir, "zeta-folder", "nested.txt");
            File.WriteAllText(nested, "n");
            var viewModel = _CreateViewModel(dir);

            viewModel.SelectedEntry = viewModel.Entries.First(entry => entry.IsDirectory);
            viewModel.OpenSelected();

            Assert.Equal(["nested.txt"], _Names(viewModel));
            Assert.True(viewModel.CanGoUp);

            viewModel.GoUp();

            Assert.Equal(["zeta-folder", "alpha.txt", "beta.md"], _Names(viewModel));
        }

        /// <summary>
        /// Verifies back and forward walk the explorer history.
        /// </summary>
        [Fact]
        public void GoBack_And_GoForward_Restore_History()
        {
            var dir = _CreateTree();
            var viewModel = _CreateViewModel(dir);
            var child = Path.Combine(dir, "zeta-folder");

            viewModel.NavigateTo(child);
            Assert.Equal(new DirectoryInfo(child).FullName, viewModel.CurrentPath);
            Assert.True(viewModel.CanGoBack);

            viewModel.GoBack();
            Assert.Equal(new DirectoryInfo(dir).FullName, viewModel.CurrentPath);
            Assert.True(viewModel.CanGoForward);

            viewModel.GoForward();
            Assert.Equal(new DirectoryInfo(child).FullName, viewModel.CurrentPath);
        }

        /// <summary>
        /// Verifies hidden and system items are omitted from the listing.
        /// </summary>
        [Fact]
        public void Skips_Hidden_Items()
        {
            var dir = _CreateTree();
            var hiddenName = OperatingSystem.IsWindows() ? "secret.txt" : ".secret.txt";
            var hiddenPath = Path.Combine(dir, hiddenName);
            File.WriteAllText(hiddenPath, "hidden");
            if (OperatingSystem.IsWindows())
                File.SetAttributes(hiddenPath, FileAttributes.Hidden);

            var viewModel = _CreateViewModel(dir);

            Assert.DoesNotContain(hiddenName, _Names(viewModel));
        }

        /// <summary>
        /// Verifies an invalid committed path leaves the current folder unchanged.
        /// </summary>
        [Fact]
        public void CommitPath_Ignores_Missing_Directory()
        {
            var dir = _CreateTree();
            var viewModel = _CreateViewModel(dir);
            var current = viewModel.CurrentPath;

            viewModel.PathText = Path.Combine(dir, "does-not-exist");
            viewModel.CommitPath();

            Assert.Equal(current, viewModel.CurrentPath);
        }

        /// <summary>
        /// Verifies Refresh reloads entries after a file is added on disk.
        /// </summary>
        [Fact]
        public void Refresh_Reloads_Current_Folder()
        {
            var dir = _CreateTree();
            var viewModel = _CreateViewModel(dir);

            File.WriteAllText(Path.Combine(dir, "gamma.txt"), "g");
            viewModel.Refresh();

            Assert.Contains("gamma.txt", _Names(viewModel));
        }

        private string _CreateTree()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            Directory.CreateDirectory(Path.Combine(dir, "zeta-folder"));
            File.WriteAllText(Path.Combine(dir, "alpha.txt"), "a");
            File.WriteAllText(Path.Combine(dir, "beta.md"), "b");
            return dir;
        }

        private static FileListViewModel _CreateViewModel(string dir)
        {
            return new FileListViewModel(NullSystemIconProvider.Instance, dir);
        }

        private static List<string> _Names(FileListViewModel viewModel)
        {
            return [.. viewModel.Entries.Select(entry => entry.Name)];
        }
    }
}
