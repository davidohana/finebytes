using Avalonia.Media;
using Mfr.App.Ui.Services.FileList;
using Mfr.App.Ui.ViewModels.FileList;
using Mfr.Utils;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Tests File List listing, mask, and navigation in <see cref="FileListViewModel"/>.
    /// </summary>
    public sealed class FileListViewModelTests : IDisposable
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
        /// Verifies typed masks are remembered only on commit, not on every property change.
        /// </summary>
        [Fact]
        public void Mask_Is_Remembered_Only_On_Commit()
        {
            var dir = _CreateTree();
            var viewModel = _CreateViewModel(dir);
            var defaultCount = viewModel.MaskSuggestions.Count;

            viewModel.Mask = "4";
            viewModel.Mask = "44";
            viewModel.Mask = "444";

            Assert.Equal(defaultCount, viewModel.MaskSuggestions.Count);

            viewModel.CommitMask();

            Assert.Equal("444", viewModel.MaskSuggestions[0]);
            Assert.DoesNotContain("4", viewModel.MaskSuggestions);
            Assert.DoesNotContain("44", viewModel.MaskSuggestions);
        }

        /// <summary>
        /// Verifies committing a mask moves it to the front of suggestions.
        /// </summary>
        [Fact]
        public void CommitMask_Moves_Existing_Mask_To_Front()
        {
            var dir = _CreateTree();
            var viewModel = _CreateViewModel(dir);

            viewModel.Mask = "*.txt";
            viewModel.CommitMask();

            Assert.Equal("*.txt", viewModel.MaskSuggestions[0]);
            Assert.Equal(1, viewModel.MaskSuggestions.Count(m => m == "*.txt"));
        }

        /// <summary>
        /// Verifies only the 10 most recently committed masks are kept.
        /// </summary>
        [Fact]
        public void CommitMask_Keeps_Only_Last_10_Masks()
        {
            var dir = _CreateTree();
            var viewModel = _CreateViewModel(dir);

            for (var i = 1; i <= 12; i++)
            {
                viewModel.Mask = $"*.ext{i}";
                viewModel.CommitMask();
            }

            Assert.Equal(10, viewModel.MaskSuggestions.Count);
            Assert.Equal("*.ext12", viewModel.MaskSuggestions[0]);
            Assert.Equal("*.ext3", viewModel.MaskSuggestions[^1]);
            Assert.DoesNotContain("*.ext1", viewModel.MaskSuggestions);
            Assert.DoesNotContain("*.ext2", viewModel.MaskSuggestions);
        }

        /// <summary>
        /// Verifies exclude masks hide matching files from the listing when enabled.
        /// </summary>
        [Fact]
        public void ExcludeMasks_Hide_Matching_Files_When_Enabled()
        {
            var dir = _CreateTree();
            var viewModel = _CreateViewModel(dir);

            viewModel.ApplyExcludeMasks(enabled: true, editorText: "*.txt\n*.bak");

            Assert.Equal(["zeta-folder", "beta.md"], _Names(viewModel));
        }

        /// <summary>
        /// Verifies stored exclude masks do not filter until enabled.
        /// </summary>
        [Fact]
        public void ExcludeMasks_Do_Not_Filter_When_Disabled()
        {
            var dir = _CreateTree();
            var viewModel = _CreateViewModel(dir);

            viewModel.ApplyExcludeMasks(enabled: false, editorText: "*.txt\n*.bak");

            Assert.Equal(["zeta-folder", "alpha.txt", "beta.md"], _Names(viewModel));
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
            {
                File.SetAttributes(hiddenPath, FileAttributes.Hidden);
            }

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
            {
                Assert.Equal(FileListViewModel.ComputerDisplayName, labels[0]);
            }
            else
            {
                Assert.Equal(FileListViewModel.UnixRootPath, labels[0]);
            }

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
        /// Verifies the address-bar computer/folder icon opens This PC on Windows, or <c>/</c> on Unix.
        /// </summary>
        [Fact]
        public void AddressBar_Root_Icon_Navigates_To_Computer_Or_Unix_Root()
        {
            var viewModel = _CreateViewModel(_CreateTree());

            viewModel.NavigateTo(viewModel.RootTargetPath);

            Assert.False(viewModel.IsPathEditing);
            if (OperatingSystem.IsWindows())
            {
                Assert.Equal(FileListViewModel.ComputerDisplayName, viewModel.RootTargetPath);
                Assert.Equal(FileListViewModel.ComputerPath, viewModel.CurrentPath);
                return;
            }

            Assert.Equal(FileListViewModel.UnixRootPath, viewModel.RootTargetPath);
            Assert.Equal(FileListViewModel.UnixRootPath, viewModel.CurrentPath);
        }

        /// <summary>
        /// Verifies This PC lists Network with other folders and opening it shows the Network location.
        /// </summary>
        [Fact]
        public void ThisPc_Lists_Network_And_OpenSelected_Navigates()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var viewModel = _CreateViewModel(_CreateTree());
            viewModel.NavigateTo(FileListViewModel.ComputerDisplayName);

            Assert.Equal(FileListViewModel.ComputerPath, viewModel.CurrentPath);
            var network = Assert.Single(viewModel.Entries, entry => entry.Name == FileListViewModel.NetworkDisplayName);
            Assert.Equal("Network location", network.Type);
            Assert.False(viewModel.CanGoUp);

            viewModel.SelectedEntry = network;
            viewModel.OpenSelected();

            Assert.Equal(FileListViewModel.NetworkPath, viewModel.CurrentPath);
            Assert.Equal(FileListViewModel.NetworkDisplayName, viewModel.PathText);
            Assert.True(viewModel.CanGoUp);
            Assert.Equal(
                [FileListViewModel.ComputerDisplayName, FileListViewModel.NetworkDisplayName],
                viewModel.BreadcrumbSegments.Select(segment => segment.Label)
            );

            viewModel.GoUp();
            Assert.Equal(FileListViewModel.ComputerPath, viewModel.CurrentPath);
        }

        /// <summary>
        /// Verifies Network stays with known folders on This PC when the name column is reversed.
        /// </summary>
        [Fact]
        public void ThisPc_Keeps_Network_With_Known_Places_When_Sorted()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var viewModel = _CreateViewModel(_CreateTree());
            viewModel.NavigateTo(FileListViewModel.ComputerDisplayName);
            _AssertNetworkAfterDrives(_Names(viewModel));

            viewModel.SortByColumn(nameof(FileListEntry.Name));

            _AssertNetworkAfterDrives(_Names(viewModel));
        }

        /// <summary>
        /// Verifies typing Network or <c>\\</c> opens the Network location.
        /// </summary>
        [Fact]
        public void CommitPath_Network_Aliases_Open_Network()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var viewModel = _CreateViewModel(_CreateTree());
            viewModel.BeginPathEdit();
            viewModel.PathText = @"\\";
            viewModel.CommitPath();

            Assert.Equal(FileListViewModel.NetworkPath, viewModel.CurrentPath);
            Assert.False(viewModel.IsPathEditing);
        }

        /// <summary>
        /// Verifies Network lists recent UNC folders from address-bar history.
        /// </summary>
        [Fact]
        public void Network_Lists_Unc_Paths_From_History()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var viewModel = _CreateViewModel(_CreateTree());
            viewModel.NavigateTo(FileListViewModel.NetworkDisplayName);
            viewModel.PathHistory.Insert(0, @"\\nas\music\albums");
            viewModel.Refresh();

            Assert.Contains(@"\\nas\music\albums", _Names(viewModel));
        }

        /// <summary>
        /// Verifies typing <c>\\wsl</c> opens the live WSL root and lists distros.
        /// </summary>
        [Fact]
        public void NavigateTo_Wsl_Alias_Opens_Live_Root()
        {
            if (!OperatingSystem.IsWindows() || !WindowsWslUnc.TryGetLiveRoot(out var liveRoot))
            {
                return;
            }

            var viewModel = _CreateViewModel(_CreateTree());
            viewModel.NavigateTo(@"\\wsl");

            Assert.True(PathRelations.IsSamePath(liveRoot, viewModel.CurrentPath));
            Assert.Equal(liveRoot, viewModel.PathText);
            Assert.Contains(liveRoot[2..], viewModel.BreadcrumbSegments.Select(segment => segment.Label));
            Assert.True(WindowsWslUnc.TryListDistroPaths(liveRoot, out var distroPaths));
            Assert.NotEmpty(distroPaths);
            Assert.Equal(distroPaths.Count, viewModel.Entries.Count);
            foreach (var distroPath in distroPaths)
            {
                var name = Path.GetFileName(distroPath);
                if (string.IsNullOrEmpty(name))
                {
                    name = distroPath[(liveRoot.Length + 1)..];
                }

                Assert.Contains(name, _Names(viewModel));
            }

            viewModel.GoUp();
            Assert.Equal(FileListViewModel.NetworkPath, viewModel.CurrentPath);
            Assert.Contains(liveRoot[2..], _Names(viewModel));
        }

        /// <summary>
        /// Verifies Network lists the live WSL host when the redirector is present.
        /// </summary>
        [Fact]
        public void Network_Lists_Live_Wsl_Root()
        {
            if (!OperatingSystem.IsWindows() || !WindowsWslUnc.TryGetLiveRoot(out var liveRoot))
            {
                return;
            }

            var viewModel = _CreateViewModel(_CreateTree());
            viewModel.NavigateTo(FileListViewModel.NetworkDisplayName);

            Assert.Contains(liveRoot[2..], _Names(viewModel));
        }

        /// <summary>
        /// Verifies the Windows root breadcrumb opens the drive list.
        /// </summary>
        [Fact]
        public void Breadcrumb_Root_Opens_Drive_List_On_Windows()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

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
        /// Verifies a readable empty folder does not show a listing error.
        /// </summary>
        [Fact]
        public void Empty_Folder_Has_No_ListingError()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var viewModel = _CreateViewModel(dir);

            Assert.Empty(viewModel.Entries);
            Assert.False(viewModel.HasListingError);
            Assert.Equal(string.Empty, viewModel.ListingError);
        }

        /// <summary>
        /// Verifies navigating into an unreadable folder shows an in-pane listing error.
        /// </summary>
        [Fact]
        public void NavigateTo_Inaccessible_Folder_Sets_ListingError()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var parent = _tempDirectoryFixture.CreateTempDir();
            var deniedFolder = Directory.CreateDirectory(Path.Combine(parent, "Denied")).FullName;
            _DenyDirectoryTraverse(deniedFolder);

            try
            {
                var viewModel = _CreateViewModel(parent);
                viewModel.NavigateTo(deniedFolder);

                Assert.Equal(deniedFolder, viewModel.CurrentPath);
                Assert.Empty(viewModel.Entries);
                Assert.True(viewModel.HasListingError);
                Assert.Contains("Access denied", viewModel.ListingError, StringComparison.Ordinal);
                Assert.False(viewModel.CanShowLogInExplorer);
            }
            finally
            {
                _AllowDirectoryTraverse(deniedFolder);
            }
        }

        /// <summary>
        /// Verifies leaving an unreadable folder clears the listing error.
        /// </summary>
        [Fact]
        public void NavigateAway_From_Inaccessible_Folder_Clears_ListingError()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var parent = _tempDirectoryFixture.CreateTempDir();
            var deniedFolder = Directory.CreateDirectory(Path.Combine(parent, "Denied")).FullName;
            _DenyDirectoryTraverse(deniedFolder);

            try
            {
                var viewModel = _CreateViewModel(deniedFolder);
                Assert.True(viewModel.HasListingError);

                viewModel.NavigateTo(parent);

                Assert.False(viewModel.HasListingError);
                Assert.Equal(string.Empty, viewModel.ListingError);
            }
            finally
            {
                _AllowDirectoryTraverse(deniedFolder);
            }
        }

        /// <summary>
        /// Verifies the File List starts in Report view.
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
        /// Verifies thumbnail size starts at Medium and cell width includes padding.
        /// </summary>
        [Fact]
        public void ThumbnailSize_Defaults_To_Medium()
        {
            var viewModel = _CreateViewModel(_CreateTree());

            Assert.Equal(ThumbnailSizes.Medium, viewModel.ThumbnailSize);
            Assert.True(viewModel.IsThumbnailSizeMedium);
            Assert.Equal(ThumbnailSizes.Medium + ThumbnailSizes.CellPadding, viewModel.ThumbnailCellWidth);
            Assert.Equal(ThumbnailSizes.Medium + ThumbnailSizes.CaptionHeight, viewModel.ThumbnailCellHeight);
            Assert.False(viewModel.ZoomThumbnailsInCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies zoom commands step through sizes only while Thumbnails view is active.
        /// </summary>
        [Fact]
        public void Thumbnail_Zoom_Steps_And_Stops_At_Ends()
        {
            var viewModel = _CreateViewModel(_CreateTree());
            viewModel.SetViewMode(FileListViewMode.Thumbnails);

            Assert.True(viewModel.ZoomThumbnailsInCommand.CanExecute(null));
            viewModel.ZoomThumbnailsIn();
            Assert.Equal(ThumbnailSizes.Large, viewModel.ThumbnailSize);
            Assert.True(viewModel.IsThumbnailSizeLarge);
            Assert.Equal(ThumbnailSizes.Large + ThumbnailSizes.CaptionHeight, viewModel.ThumbnailCellHeight);

            viewModel.ZoomThumbnailsIn();
            viewModel.ZoomThumbnailsIn();
            viewModel.ZoomThumbnailsIn();
            Assert.Equal(ThumbnailSizes.Huge, viewModel.ThumbnailSize);
            viewModel.ZoomThumbnailsIn();
            Assert.Equal(ThumbnailSizes.Huge, viewModel.ThumbnailSize);

            viewModel.ResetThumbnailSize();
            Assert.Equal(ThumbnailSizes.Default, viewModel.ThumbnailSize);

            viewModel.ZoomThumbnailsOut();
            Assert.Equal(ThumbnailSizes.Small, viewModel.ThumbnailSize);
            viewModel.ZoomThumbnailsOut();
            viewModel.ZoomThumbnailsOut();
            Assert.Equal(ThumbnailSizes.ExtraSmall, viewModel.ThumbnailSize);
        }

        /// <summary>
        /// Verifies SetThumbnailSize snaps to a step without rebuilding the listing.
        /// </summary>
        [Fact]
        public void SetThumbnailSize_Snaps_Without_Rebuilding_Entries()
        {
            var viewModel = _CreateViewModel(_CreateTree());
            viewModel.SetViewMode(FileListViewMode.Thumbnails);
            var first = viewModel.Entries[0];

            viewModel.SetThumbnailSize(100);

            Assert.Equal(ThumbnailSizes.Medium, viewModel.ThumbnailSize);
            Assert.Same(first, viewModel.Entries[0]);
        }

        /// <summary>
        /// Verifies thumbnail size survives refresh and folder navigation.
        /// </summary>
        [Fact]
        public void ThumbnailSize_Survives_Refresh_And_Navigate()
        {
            var dir = _CreateTree();
            var viewModel = _CreateViewModel(dir);
            viewModel.SetViewMode(FileListViewMode.Thumbnails);
            viewModel.SetThumbnailSize(ThumbnailSizes.ExtraLarge);

            viewModel.Refresh();
            Assert.Equal(ThumbnailSizes.ExtraLarge, viewModel.ThumbnailSize);

            viewModel.SelectedEntry = viewModel.Entries.First(entry => entry.IsDirectory);
            viewModel.OpenSelected();
            Assert.Equal(ThumbnailSizes.ExtraLarge, viewModel.ThumbnailSize);

            viewModel.GoUp();
            Assert.Equal(ThumbnailSizes.ExtraLarge, viewModel.ThumbnailSize);
            Assert.Equal(dir, viewModel.CurrentPath);
        }

        /// <summary>
        /// Verifies SetSelectedEntries tracks multiple rows and keeps the focused entry.
        /// </summary>
        [Fact]
        public void SetSelectedEntries_Tracks_Multiple_And_Focused_Entry()
        {
            var viewModel = _CreateViewModel(_CreateTree());
            var alpha = viewModel.Entries.First(entry => entry.Name == "alpha.txt");
            var beta = viewModel.Entries.First(entry => entry.Name == "beta.md");

            viewModel.SetSelectedEntries([alpha, beta], focusedEntry: beta);

            Assert.Equal(2, viewModel.SelectedEntries.Count);
            Assert.Same(beta, viewModel.SelectedEntry);
            Assert.Contains(alpha, viewModel.SelectedEntries);
            Assert.Contains(beta, viewModel.SelectedEntries);
        }

        /// <summary>
        /// Verifies assigning SelectedEntry collapses the selection to that one row.
        /// </summary>
        [Fact]
        public void SelectedEntry_Set_Collapses_To_Single_Selection()
        {
            var viewModel = _CreateViewModel(_CreateTree());
            var alpha = viewModel.Entries.First(entry => entry.Name == "alpha.txt");
            var beta = viewModel.Entries.First(entry => entry.Name == "beta.md");

            viewModel.SetSelectedEntries([alpha, beta]);
            viewModel.SelectedEntry = alpha;

            Assert.Single(viewModel.SelectedEntries);
            Assert.Same(alpha, viewModel.SelectedEntry);
        }

        /// <summary>
        /// Verifies refresh rebuilds entry objects but keeps multi-select by full path.
        /// </summary>
        [Fact]
        public void Refresh_Preserves_Multi_Selection_By_Path()
        {
            var viewModel = _CreateViewModel(_CreateTree());
            var alphaPath = viewModel.Entries.First(entry => entry.Name == "alpha.txt").FullPath;
            var betaPath = viewModel.Entries.First(entry => entry.Name == "beta.md").FullPath;

            viewModel.SetSelectedEntries([
                viewModel.Entries.First(entry => entry.Name == "alpha.txt"),
                viewModel.Entries.First(entry => entry.Name == "beta.md"),
            ]);
            viewModel.Refresh();

            Assert.Equal(2, viewModel.SelectedEntries.Count);
            Assert.Contains(viewModel.SelectedEntries, entry => entry.FullPath == alphaPath);
            Assert.Contains(viewModel.SelectedEntries, entry => entry.FullPath == betaPath);
        }

        /// <summary>
        /// Verifies navigation clears the current selection.
        /// </summary>
        [Fact]
        public void Navigate_Clears_Selection()
        {
            var dir = _CreateTree();
            var nested = Path.Combine(dir, "zeta-folder", "nested.txt");
            File.WriteAllText(nested, "n");
            var viewModel = _CreateViewModel(dir);
            viewModel.SetSelectedEntries([viewModel.Entries[0], viewModel.Entries[1]]);

            viewModel.SelectedEntry = viewModel.Entries.First(entry => entry.IsDirectory);
            viewModel.OpenSelected();

            Assert.Empty(viewModel.SelectedEntries);
            Assert.Null(viewModel.SelectedEntry);
        }

        /// <summary>
        /// Verifies TryMoveSelection replaces multi-select with the adjacent row.
        /// </summary>
        [Fact]
        public void TryMoveSelection_Replaces_Multi_Select_With_Adjacent_Row()
        {
            var viewModel = _CreateViewModel(_CreateTree());
            var alpha = viewModel.Entries.First(entry => entry.Name == "alpha.txt");
            var beta = viewModel.Entries.First(entry => entry.Name == "beta.md");
            viewModel.SetSelectedEntries([alpha, beta], focusedEntry: beta);

            Assert.True(viewModel.TryMoveSelection(delta: -1));

            Assert.Single(viewModel.SelectedEntries);
            Assert.Same(alpha, viewModel.SelectedEntry);
        }

        /// <summary>
        /// Verifies TryMoveSelection moves down to the next row.
        /// </summary>
        [Fact]
        public void TryMoveSelection_Moves_Down()
        {
            var viewModel = _CreateViewModel(_CreateTree());
            var folder = viewModel.Entries.First(entry => entry.IsDirectory);
            var alpha = viewModel.Entries.First(entry => entry.Name == "alpha.txt");
            viewModel.SelectedEntry = folder;

            Assert.True(viewModel.TryMoveSelection(delta: 1));

            Assert.Single(viewModel.SelectedEntries);
            Assert.Same(alpha, viewModel.SelectedEntry);
        }

        /// <summary>
        /// Verifies TryMoveSelection stops at the first row.
        /// </summary>
        [Fact]
        public void TryMoveSelection_Stops_At_List_Edge()
        {
            var viewModel = _CreateViewModel(_CreateTree());
            viewModel.SelectedEntry = viewModel.Entries[0];

            Assert.False(viewModel.TryMoveSelection(delta: -1));
            Assert.Same(viewModel.Entries[0], viewModel.SelectedEntry);
        }

        /// <summary>
        /// Verifies Thumbnails requests jumbo shell icons so glyphs are not upscaled from 32×32.
        /// </summary>
        [Fact]
        public void Thumbnails_View_Requests_Jumbo_Shell_Icons()
        {
            var provider = new RecordingIconProvider();
            var viewModel = new FileListViewModel(provider, _CreateTree(), NullFileShellOpener.Instance);
            _viewModels.Add(viewModel);

            provider.RequestedSizes.Clear();
            viewModel.ViewMode = FileListViewMode.Thumbnails;

            Assert.NotEmpty(provider.RequestedSizes);
            Assert.All(provider.RequestedSizes, size => Assert.Equal(ShellIconSize.Jumbo, size));
        }

        /// <summary>
        /// Verifies switching to Thumbnails lists image files immediately without waiting on decode.
        /// </summary>
        [Fact]
        public void Thumbnails_View_Lists_Image_Folder_Without_Waiting_For_Decode()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "tiny.jpeg");
            Assert.True(File.Exists(fixture), $"Missing fixture '{fixture}'.");
            var expected = new List<string>();
            for (var i = 0; i < 40; i++)
            {
                var name = $"photo-{i:00}.jpeg";
                File.Copy(fixture, Path.Combine(dir, name));
                expected.Add(name);
            }

            var viewModel = _CreateViewModel(dir);
            viewModel.ViewMode = FileListViewMode.Thumbnails;

            Assert.Equal(expected, _Names(viewModel));
            Assert.Equal(40, viewModel.Entries.Count);
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

        /// <summary>
        /// Verifies This PC lists drives above Documents, Music, and Pictures.
        /// </summary>
        [Fact]
        public void ThisPc_Lists_Drives_Before_Known_Places()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var viewModel = _CreateViewModel(_CreateTree());
            viewModel.NavigateTo(FileListViewModel.ComputerDisplayName);
            var names = _Names(viewModel);

            Assert.True(_IsDriveName(names[0]));
            var lastDriveIndex = names.FindLastIndex(_IsDriveName);
            Assert.True(lastDriveIndex >= 0);

            _AssertPlaceAfterDrives(names, "Documents", lastDriveIndex, Environment.SpecialFolder.MyDocuments);
            _AssertPlaceAfterDrives(names, "Music", lastDriveIndex, Environment.SpecialFolder.MyMusic);
            _AssertPlaceAfterDrives(names, "Pictures", lastDriveIndex, Environment.SpecialFolder.MyPictures);
            _AssertNetworkAfterDrives(names);

            viewModel.SortByColumn(nameof(FileListEntry.Name));
            var reversed = _Names(viewModel);
            Assert.True(_IsDriveName(reversed[0]));
            var reversedDriveIndex = reversed.FindLastIndex(_IsDriveName);
            _AssertPlaceAfterDrives(reversed, "Documents", reversedDriveIndex, Environment.SpecialFolder.MyDocuments);
            _AssertNetworkAfterDrives(reversed);
        }

        /// <summary>
        /// Verifies typing a drive letter such as <c>D:</c> opens the drive root.
        /// </summary>
        [Fact]
        public void NavigateTo_Drive_Letter_Opens_Root()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var dir = _CreateTree();
            var root = Path.GetPathRoot(dir);
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
            {
                return;
            }

            var driveSpec = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var viewModel = _CreateViewModel(dir);
            viewModel.BeginPathEdit();
            viewModel.PathText = driveSpec.ToLowerInvariant();
            viewModel.CommitPath();

            Assert.True(PathRelations.IsSamePath(root, viewModel.CurrentPath));
            Assert.False(viewModel.IsPathEditing);
        }

        /// <summary>
        /// Verifies typing Documents or Music opens those folders.
        /// </summary>
        [Fact]
        public void NavigateTo_Known_Place_Alias_Opens_Folder()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var documents = _ExistingSpecialFolder(Environment.SpecialFolder.MyDocuments);
            if (documents is null)
            {
                return;
            }

            var viewModel = _CreateViewModel(_CreateTree());
            viewModel.NavigateTo("Documents");
            Assert.Equal(documents, viewModel.CurrentPath);
            Assert.Equal(
                [FileListViewModel.ComputerDisplayName, "Documents"],
                viewModel.BreadcrumbSegments.Select(segment => segment.Label)
            );

            viewModel.GoUp();
            Assert.Equal(FileListViewModel.ComputerPath, viewModel.CurrentPath);

            var music = _ExistingSpecialFolder(Environment.SpecialFolder.MyMusic);
            if (music is null)
            {
                return;
            }

            viewModel.BeginPathEdit();
            viewModel.PathText = "Music";
            viewModel.CommitPath();
            Assert.Equal(music, viewModel.CurrentPath);
            Assert.False(viewModel.IsPathEditing);
        }

        /// <summary>
        /// Verifies environment variables in a typed path are expanded.
        /// </summary>
        [Fact]
        public void CommitPath_Expands_Environment_Variables()
        {
            var expanded = Environment.ExpandEnvironmentVariables("%USERPROFILE%");
            if (expanded == "%USERPROFILE%" || !Directory.Exists(expanded))
            {
                return;
            }

            var viewModel = _CreateViewModel(_CreateTree());
            viewModel.BeginPathEdit();
            viewModel.PathText = "%USERPROFILE%";
            viewModel.CommitPath();

            Assert.Equal(new DirectoryInfo(expanded).FullName, viewModel.CurrentPath);
        }

        private static void _AssertPlaceAfterDrives(
            List<string> names,
            string placeName,
            int lastDriveIndex,
            Environment.SpecialFolder folder
        )
        {
            if (_ExistingSpecialFolder(folder) is null)
            {
                return;
            }

            var index = names.IndexOf(placeName);
            Assert.True(index > 0, placeName + " should be listed on This PC");
            Assert.True(index > lastDriveIndex, placeName + " should appear after drives");
        }

        private static void _AssertNetworkAfterDrives(List<string> names)
        {
            var lastDriveIndex = names.FindLastIndex(_IsDriveName);
            var networkIndex = names.IndexOf(FileListViewModel.NetworkDisplayName);
            Assert.True(networkIndex > lastDriveIndex, "Network should appear with folders after drives");
        }

        /// <summary>
        /// Verifies Open is enabled for files as well as folders.
        /// </summary>
        [Fact]
        public void OpenSelectedCommand_CanExecute_For_Files()
        {
            var viewModel = _CreateViewModel(_CreateTree());
            var file = viewModel.Entries.First(entry => !entry.IsDirectory);
            viewModel.SetSelectedEntries([file], file);

            Assert.True(viewModel.OpenSelectedCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies Open launches the default app for a selected file.
        /// </summary>
        [Fact]
        public void OpenSelected_Opens_File_With_Shell()
        {
            var shell = new RecordingShellOpener();
            var dir = _CreateTree();
            var viewModel = _CreateViewModel(dir, shell);
            var file = viewModel.Entries.First(entry => entry.Name == "alpha.txt");
            viewModel.SetSelectedEntries([file], file);

            viewModel.OpenSelected();

            Assert.Equal([file.FullPath], shell.OpenedWithDefaultApp);
            Assert.Empty(shell.RevealedInFileManager);
        }

        /// <summary>
        /// Verifies Copy path writes every selected full path, one per line.
        /// </summary>
        [Fact]
        public async Task CopyPath_Writes_Selected_Paths_To_Clipboard()
        {
            var clipboard = new RecordingClipboard();
            var dir = _CreateTree();
            var viewModel = _CreateViewModel(dir, clipboard: clipboard);
            var alpha = viewModel.Entries.First(entry => entry.Name == "alpha.txt");
            var beta = viewModel.Entries.First(entry => entry.Name == "beta.md");
            viewModel.SetSelectedEntries([alpha, beta], beta);

            await viewModel.CopyPathCommand.ExecuteAsync(null);

            Assert.Equal($"{alpha.FullPath}{Environment.NewLine}{beta.FullPath}", clipboard.LastText);
        }

        /// <summary>
        /// Verifies Copy path is disabled when nothing is selected.
        /// </summary>
        [Fact]
        public void CopyPathCommand_Disabled_When_Selection_Empty()
        {
            var viewModel = _CreateViewModel(_CreateTree());
            viewModel.SetSelectedEntries([]);

            Assert.False(viewModel.CopyPathCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies Show in Explorer reveals the focused selection.
        /// </summary>
        [Fact]
        public void ShowInExplorer_Reveals_Selected_Entry()
        {
            var shell = new RecordingShellOpener();
            var dir = _CreateTree();
            var viewModel = _CreateViewModel(dir, shell);
            var file = viewModel.Entries.First(entry => entry.Name == "alpha.txt");
            viewModel.SetSelectedEntries([file], file);

            viewModel.ShowInExplorer();

            Assert.Equal([file.FullPath], shell.RevealedInFileManager);
            Assert.Empty(shell.OpenedFolders);
        }

        /// <summary>
        /// Verifies Show in Explorer opens the current folder when the selection is empty.
        /// </summary>
        [Fact]
        public void ShowInExplorer_Opens_Current_Folder_When_Selection_Empty()
        {
            var shell = new RecordingShellOpener();
            var dir = _CreateTree();
            var viewModel = _CreateViewModel(dir, shell);
            viewModel.SetSelectedEntries([]);

            Assert.True(viewModel.ShowInExplorerCommand.CanExecute(null));
            viewModel.ShowInExplorer();

            Assert.Equal([viewModel.CurrentPath], shell.OpenedFolders);
            Assert.Empty(shell.RevealedInFileManager);
        }

        /// <summary>
        /// Verifies Show in Explorer is disabled on This PC with no selection.
        /// </summary>
        [Fact]
        public void ShowInExplorerCommand_Disabled_On_ThisPc_Without_Selection()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var viewModel = _CreateViewModel(_CreateTree());
            viewModel.NavigateTo(FileListViewModel.ComputerDisplayName);
            viewModel.SetSelectedEntries([]);

            Assert.False(viewModel.ShowInExplorerCommand.CanExecute(null));
        }

        private static bool _IsDriveName(string name)
        {
            return name.Contains(':', StringComparison.Ordinal);
        }

        private static string? _ExistingSpecialFolder(Environment.SpecialFolder folder)
        {
            var path = Environment.GetFolderPath(folder);
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                return null;
            }

            return new DirectoryInfo(path).FullName;
        }

        private string _CreateTree()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            Directory.CreateDirectory(Path.Combine(dir, "zeta-folder"));
            File.WriteAllText(Path.Combine(dir, "alpha.txt"), "a");
            File.WriteAllText(Path.Combine(dir, "beta.md"), "b");
            return dir;
        }

        private FileListViewModel _CreateViewModel(
            string dir,
            IFileShellOpener? shellOpener = null,
            ITextClipboard? clipboard = null
        )
        {
            var viewModel = new FileListViewModel(
                NullSystemIconProvider.Instance,
                dir,
                shellOpener ?? NullFileShellOpener.Instance,
                clipboard ?? NullTextClipboard.Instance
            );
            _viewModels.Add(viewModel);
            return viewModel;
        }

        private static List<string> _Names(FileListViewModel viewModel)
        {
            return [.. viewModel.Entries.Select(entry => entry.Name)];
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

        private sealed class RecordingIconProvider : ISystemIconProvider
        {
            public List<ShellIconSize> RequestedSizes { get; } = [];

            public IImage? GetIcon(string path, bool isDirectory, ShellIconSize size)
            {
                RequestedSizes.Add(size);
                return null;
            }
        }

        private sealed class RecordingShellOpener : IFileShellOpener
        {
            public List<string> OpenedWithDefaultApp { get; } = [];
            public List<string> RevealedInFileManager { get; } = [];
            public List<string> OpenedFolders { get; } = [];

            public void OpenWithDefaultApp(string path)
            {
                OpenedWithDefaultApp.Add(path);
            }

            public void RevealInFileManager(string path)
            {
                RevealedInFileManager.Add(path);
            }

            public void OpenFolderInFileManager(string folderPath)
            {
                OpenedFolders.Add(folderPath);
            }
        }

        private sealed class RecordingClipboard : ITextClipboard
        {
            public string? LastText { get; private set; }

            public Task SetTextAsync(string text)
            {
                LastText = text;
                return Task.CompletedTask;
            }
        }
    }
}
