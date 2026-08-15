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
        /// Verifies Name sort keeps folders above files, matching Windows Explorer.
        /// </summary>
        [Fact]
        public void SortByColumn_Name_Keeps_Folders_First()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            Directory.CreateDirectory(Path.Combine(dir, "alpha-folder"));
            Directory.CreateDirectory(Path.Combine(dir, "zeta-folder"));
            File.WriteAllText(Path.Combine(dir, "alpha.txt"), "a");
            File.WriteAllText(Path.Combine(dir, "zeta.txt"), "z");
            var viewModel = _CreateViewModel(dir);

            Assert.Equal(["alpha-folder", "zeta-folder", "alpha.txt", "zeta.txt"], _Names(viewModel));

            viewModel.SortByColumn(nameof(FileListEntry.Name));

            Assert.Equal(["zeta-folder", "alpha-folder", "zeta.txt", "alpha.txt"], _Names(viewModel));
            Assert.True(viewModel.Entries[0].IsDirectory);
            Assert.True(viewModel.Entries[1].IsDirectory);
            Assert.False(viewModel.Entries[2].IsDirectory);
        }

        /// <summary>
        /// Verifies Size sort keeps folders above files.
        /// </summary>
        [Fact]
        public void SortByColumn_Length_Keeps_Folders_First()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            Directory.CreateDirectory(Path.Combine(dir, "zeta-folder"));
            File.WriteAllText(Path.Combine(dir, "tiny.txt"), "a");
            File.WriteAllText(Path.Combine(dir, "huge.txt"), new string('b', 32));
            var viewModel = _CreateViewModel(dir);

            viewModel.SortByColumn(nameof(FileListEntry.Length));

            Assert.Equal(["zeta-folder", "tiny.txt", "huge.txt"], _Names(viewModel));

            viewModel.SortByColumn(nameof(FileListEntry.Length));

            Assert.Equal(["zeta-folder", "huge.txt", "tiny.txt"], _Names(viewModel));
            Assert.True(viewModel.Entries[0].IsDirectory);
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
        /// Verifies the address bar trail includes the OS root and the current folder.
        /// </summary>
        [Fact]
        public void Breadcrumb_Includes_Root_And_Current_Folder()
        {
            var dir = _CreateTree();
            var child = Path.Combine(dir, "zeta-folder");
            var viewModel = _CreateViewModel(child);
            var labels = viewModel.BreadcrumbSegments.Select(segment => segment.Label).ToList();

            Assert.Contains("zeta-folder", labels);
            Assert.Contains(new DirectoryInfo(dir).Name, labels);
            Assert.Equal(OperatingSystem.IsWindows(), viewModel.ShowsComputerRoot);

            if (OperatingSystem.IsWindows())
                Assert.Equal(FileListViewModel.ComputerDisplayName, labels[0]);
            else
                Assert.Equal(FileListViewModel.UnixRootPath, labels[0]);

            Assert.False(viewModel.BreadcrumbSegments[0].ShowLeadingChevron);
            Assert.True(viewModel.BreadcrumbSegments[^1].ShowLeadingChevron);
        }

        /// <summary>
        /// Verifies clicking an ancestor breadcrumb opens that folder.
        /// </summary>
        [Fact]
        public void Breadcrumb_Segment_Navigates_To_Ancestor()
        {
            var dir = _CreateTree();
            var child = Path.Combine(dir, "zeta-folder");
            var viewModel = _CreateViewModel(child);

            viewModel.NavigateTo(viewModel.BreadcrumbSegments[^2].TargetPath);

            Assert.Equal(new DirectoryInfo(dir).FullName, viewModel.CurrentPath);
        }

        /// <summary>
        /// Verifies the Windows root breadcrumb opens the drive list.
        /// </summary>
        [Fact]
        public void Breadcrumb_Root_Opens_Drive_List_On_Windows()
        {
            if (!OperatingSystem.IsWindows())
                return;

            var viewModel = _CreateViewModel(_CreateTree());
            viewModel.NavigateTo(viewModel.BreadcrumbSegments[0].TargetPath);

            Assert.Equal(FileListViewModel.ComputerPath, viewModel.CurrentPath);
            var root = Assert.Single(viewModel.BreadcrumbSegments);
            Assert.Equal(FileListViewModel.ComputerDisplayName, root.Label);
        }

        /// <summary>
        /// Verifies typed-path mode can be cancelled without navigating.
        /// </summary>
        [Fact]
        public void CancelPathEdit_Restores_Display_Path()
        {
            var viewModel = _CreateViewModel(_CreateTree());
            var original = viewModel.PathText;

            viewModel.BeginPathEdit();
            viewModel.PathText = "does-not-exist";
            viewModel.CancelPathEdit();

            Assert.False(viewModel.IsPathEditing);
            Assert.Equal(original, viewModel.PathText);
            Assert.Equal(new DirectoryInfo(original).FullName, viewModel.CurrentPath);
        }

        /// <summary>
        /// Verifies committing a typed path leaves address-bar edit mode.
        /// </summary>
        [Fact]
        public void CommitPath_Leaves_Typed_Path_Mode()
        {
            var dir = _CreateTree();
            var child = Path.Combine(dir, "zeta-folder");
            var viewModel = _CreateViewModel(dir);

            viewModel.BeginPathEdit();
            viewModel.PathText = child;
            viewModel.CommitPath();

            Assert.False(viewModel.IsPathEditing);
            Assert.Equal(new DirectoryInfo(child).FullName, viewModel.CurrentPath);
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

        /// <summary>
        /// Verifies the File Explorer starts in Report view.
        /// </summary>
        [Fact]
        public void ViewMode_Defaults_To_Report()
        {
            var viewModel = _CreateViewModel(_CreateTree());

            Assert.Equal(FileListViewMode.Report, viewModel.ViewMode);
            Assert.True(viewModel.IsReportView);
            Assert.False(viewModel.IsListView);
        }

        /// <summary>
        /// Verifies changing the layout does not re-order or drop listed names.
        /// </summary>
        [Fact]
        public void Changing_ViewMode_Does_Not_Change_Listing()
        {
            var viewModel = _CreateViewModel(_CreateTree());
            var names = _Names(viewModel);

            viewModel.ViewMode = FileListViewMode.LargeIcons;

            Assert.Equal(names, _Names(viewModel));
            Assert.True(viewModel.IsLargeIconsView);
        }

        /// <summary>
        /// Verifies Tiles fill type and size details, while other modes leave details empty.
        /// </summary>
        [Fact]
        public void Tiles_Populate_Details_For_Files()
        {
            var viewModel = _CreateViewModel(_CreateTree());
            var reportFile = viewModel.Entries.First(entry => entry.Name == "alpha.txt");
            Assert.Equal(string.Empty, reportFile.Details);

            viewModel.ViewMode = FileListViewMode.Tiles;

            var file = viewModel.Entries.First(entry => entry.Name == "alpha.txt");
            Assert.Contains("TXT File", file.Details, StringComparison.Ordinal);
            Assert.Contains("B", file.Details, StringComparison.Ordinal);

            var folder = viewModel.Entries.First(entry => entry.IsDirectory);
            Assert.Equal("File folder", folder.Details);
        }

        /// <summary>
        /// Verifies Report rows include Explorer-style type, date, and size.
        /// </summary>
        [Fact]
        public void Report_Populates_Explorer_Columns()
        {
            var viewModel = _CreateViewModel(_CreateTree());
            var file = viewModel.Entries.First(entry => entry.Name == "alpha.txt");
            var folder = viewModel.Entries.First(entry => entry.IsDirectory);

            Assert.Equal("TXT File", file.Type);
            Assert.False(string.IsNullOrWhiteSpace(file.DateModifiedDisplay));
            Assert.Contains("B", file.SizeDisplay, StringComparison.Ordinal);
            Assert.Equal(1, file.Length);

            Assert.Equal("File folder", folder.Type);
            Assert.False(string.IsNullOrWhiteSpace(folder.DateModifiedDisplay));
            Assert.Equal(string.Empty, folder.SizeDisplay);
            Assert.Null(folder.Length);
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
