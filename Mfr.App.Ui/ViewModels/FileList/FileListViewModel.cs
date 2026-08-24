using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.App.Ui.Services.FileList;
using Mfr.App.Ui.Services.Session;
using Mfr.Engine.Logging;
using Mfr.Utils;

namespace Mfr.App.Ui.ViewModels.FileList
{
    /// <summary>
    /// File List pane: folder listing with path, mask, exclude masks, and view modes.
    /// </summary>
    public sealed partial class FileListViewModel : ViewModelBase, IDisposable
    {
        /// <summary>
        /// Sentinel path for the Windows drive list ("This PC").
        /// </summary>
        public const string ComputerPath = FileListPath.ComputerPath;

        /// <summary>
        /// Address-bar label shown when listing drives on Windows.
        /// </summary>
        public const string ComputerDisplayName = FileListPath.ComputerDisplayName;

        /// <summary>
        /// Sentinel path for mapped drives and recent UNC locations.
        /// </summary>
        public const string NetworkPath = FileListPath.NetworkPath;

        /// <summary>
        /// Address-bar label shown for <see cref="NetworkPath"/>.
        /// </summary>
        public const string NetworkDisplayName = FileListPath.NetworkDisplayName;

        /// <summary>
        /// Address-bar label and path for the filesystem root on Unix.
        /// </summary>
        public const string UnixRootPath = FileListPath.UnixRootPath;

        private static readonly string[] _DefaultMasks =
        [
            "*",
            "*.mp3",
            "*.jpg",
            "*.gif",
            "*.bmp",
            "*.wav",
            "*.txt",
            "*.doc",
            "*.htm*",
        ];

        private const int _MaxRememberedMasks = 10;

        /// <summary>
        /// Default exclude patterns; applied only when exclude masks are enabled.
        /// </summary>
        public static IReadOnlyList<string> DefaultExcludeMasks { get; } = ["*.exe", "*.dll", "*.sys"];

        private readonly ISystemIconProvider _iconProvider;
        private readonly IFileShellOpener _shellOpener;
        private readonly ITextClipboard _clipboard;
        private readonly FileListThumbnailSession _thumbnails = new();
        private readonly List<FileListListedItem> _listedItems = [];
        private readonly List<FileListEntry> _selectedEntries = [];
        private bool _suppressSelectionSync;

        /// <summary>
        /// Initializes the File List at the user profile folder with the default icon provider.
        /// </summary>
        public FileListViewModel()
            : this(iconProvider: null, initialPath: null) { }

        /// <summary>
        /// Initializes the File List.
        /// </summary>
        /// <param name="iconProvider">Shell icons, or <see langword="null"/> to use the OS default.</param>
        /// <param name="initialPath">Directory to open, or <see langword="null"/> for the user profile.</param>
        /// <param name="shellOpener">
        /// Opens paths with the OS shell, or <see langword="null"/> to use the OS default.
        /// </param>
        /// <param name="clipboard">
        /// Clipboard for Copy path, or <see langword="null"/> to use the desktop main-window clipboard.
        /// </param>
        public FileListViewModel(
            ISystemIconProvider? iconProvider,
            string? initialPath,
            IFileShellOpener? shellOpener = null,
            ITextClipboard? clipboard = null
        )
        {
            _iconProvider = iconProvider ?? SystemIconProvider.CreateDefault();
            _shellOpener = shellOpener ?? FileShellOpener.CreateDefault();
            _clipboard = clipboard ?? new DesktopTextClipboard();
            Entries = [];
            MaskSuggestions = [.. _DefaultMasks];
            PathHistory = [];
            BreadcrumbSegments = [];
            _Navigate(FileListCatalog.ResolveStartPath(initialPath));
        }

        /// <summary>
        /// Gets the items shown in the File List pane.
        /// </summary>
        public ObservableCollection<FileListEntry> Entries { get; }

        /// <summary>
        /// Gets recent filesystem paths for the address-bar history list.
        /// </summary>
        public ObservableCollection<string> PathHistory { get; }

        /// <summary>
        /// Gets the current folder trail shown in the address bar.
        /// </summary>
        public ObservableCollection<PathBreadcrumbSegment> BreadcrumbSegments { get; }

        /// <summary>
        /// Gets whether the address bar uses a This PC root instead of a filesystem root.
        /// </summary>
        public bool ShowsComputerRoot { get; } = OperatingSystem.IsWindows();

        /// <summary>
        /// Address-bar root the computer/folder icon navigates to: This PC on Windows, <c>/</c> on Unix.
        /// </summary>
        public string RootTargetPath => ShowsComputerRoot ? ComputerDisplayName : UnixRootPath;

        /// <summary>
        /// Gets include-mask suggestions for the Mask combo.
        /// </summary>
        public ObservableCollection<string> MaskSuggestions { get; }

        /// <summary>
        /// Filesystem path of the current folder, <see cref="ComputerPath"/>, or <see cref="NetworkPath"/>.
        /// </summary>
        [ObservableProperty]
        private string _currentPath = string.Empty;

        /// <summary>
        /// Editable address-bar text (display name for the drive list).
        /// </summary>
        [ObservableProperty]
        private string _pathText = string.Empty;

        /// <summary>
        /// Whether the address bar is a typed path instead of breadcrumbs.
        /// </summary>
        [ObservableProperty]
        private bool _isPathEditing;

        /// <summary>
        /// Include mask applied to file names. Folders are always listed.
        /// </summary>
        [ObservableProperty]
        private string _mask = "*";

        /// <summary>
        /// Whether <see cref="ExcludeMasks"/> are applied when listing and adding files.
        /// </summary>
        [ObservableProperty]
        private bool _excludeMasksEnabled;

        /// <summary>
        /// Exclude masks applied to file names when <see cref="ExcludeMasksEnabled"/> is true.
        /// </summary>
        [ObservableProperty]
        private IReadOnlyList<string> _excludeMasks = DefaultExcludeMasks;

        /// <summary>
        /// User-facing message when the current folder could not be listed; empty when listing succeeded.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasListingError))]
        [NotifyPropertyChangedFor(nameof(CanShowLogInExplorer))]
        [NotifyCanExecuteChangedFor(nameof(ShowLogInExplorerCommand))]
        private string _listingError = string.Empty;

        /// <summary>
        /// Gets whether <see cref="ListingError"/> should be shown in the listing pane.
        /// </summary>
        public bool HasListingError => !string.IsNullOrEmpty(ListingError);

        /// <summary>
        /// Gets whether the listing-error empty state may offer revealing the session log file.
        /// </summary>
        public bool CanShowLogInExplorer => HasListingError && !string.IsNullOrEmpty(LogSession.LogFilePath);

        /// <summary>
        /// Layout used to present <see cref="Entries"/>. Default is Report.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsLargeIconsView))]
        [NotifyPropertyChangedFor(nameof(IsSmallIconsView))]
        [NotifyPropertyChangedFor(nameof(IsReportView))]
        [NotifyPropertyChangedFor(nameof(IsListView))]
        [NotifyPropertyChangedFor(nameof(IsTilesView))]
        [NotifyPropertyChangedFor(nameof(IsThumbnailsView))]
        [NotifyCanExecuteChangedFor(nameof(ZoomThumbnailsInCommand))]
        [NotifyCanExecuteChangedFor(nameof(ZoomThumbnailsOutCommand))]
        [NotifyCanExecuteChangedFor(nameof(ResetThumbnailSizeCommand))]
        private FileListViewMode _viewMode = FileListViewMode.Report;

        /// <summary>
        /// Pixel size of the thumbnail image box in Thumbnails view.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ThumbnailCellWidth))]
        [NotifyPropertyChangedFor(nameof(ThumbnailCellHeight))]
        [NotifyPropertyChangedFor(nameof(IsThumbnailSizeExtraSmall))]
        [NotifyPropertyChangedFor(nameof(IsThumbnailSizeSmall))]
        [NotifyPropertyChangedFor(nameof(IsThumbnailSizeMedium))]
        [NotifyPropertyChangedFor(nameof(IsThumbnailSizeLarge))]
        [NotifyPropertyChangedFor(nameof(IsThumbnailSizeExtraLarge))]
        [NotifyPropertyChangedFor(nameof(IsThumbnailSizeHuge))]
        private int _thumbnailSize = ThumbnailSizes.Default;

        /// <summary>
        /// Gets the <see cref="FileListEntry"/> property used for the current column sort.
        /// </summary>
        public string SortMemberPath { get; private set; } = nameof(FileListEntry.Name);

        /// <summary>
        /// Gets whether the current column sort is ascending.
        /// </summary>
        public bool IsSortAscending { get; private set; } = true;

        /// <summary>
        /// The focused File List row, or <see langword="null"/> when nothing is selected.
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(OpenSelectedCommand))]
        [NotifyCanExecuteChangedFor(nameof(CopyPathCommand))]
        [NotifyCanExecuteChangedFor(nameof(ShowInExplorerCommand))]
        private FileListEntry? _selectedEntry;

        /// <summary>
        /// Gets every selected File List row in the current folder.
        /// </summary>
        public IReadOnlyList<FileListEntry> SelectedEntries => _selectedEntries;

        /// <summary>
        /// Whether <see cref="GoUp"/> can move to a parent folder, Network, or This PC.
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(GoUpCommand))]
        private bool _canGoUp;

        /// <summary>
        /// Gets whether the Large Icons layout is active.
        /// </summary>
        public bool IsLargeIconsView => ViewMode == FileListViewMode.LargeIcons;

        /// <summary>
        /// Gets whether the Small Icons layout is active.
        /// </summary>
        public bool IsSmallIconsView => ViewMode == FileListViewMode.SmallIcons;

        /// <summary>
        /// Gets whether the Report layout is active.
        /// </summary>
        public bool IsReportView => ViewMode == FileListViewMode.Report;

        /// <summary>
        /// Gets whether the List layout is active.
        /// </summary>
        public bool IsListView => ViewMode == FileListViewMode.List;

        /// <summary>
        /// Gets whether the Tiles layout is active.
        /// </summary>
        public bool IsTilesView => ViewMode == FileListViewMode.Tiles;

        /// <summary>
        /// Gets whether the Thumbnails layout is active.
        /// </summary>
        public bool IsThumbnailsView => ViewMode == FileListViewMode.Thumbnails;

        /// <summary>
        /// Gets the wrapping cell width for the current <see cref="ThumbnailSize"/>.
        /// </summary>
        public int ThumbnailCellWidth => ThumbnailSize + ThumbnailSizes.CellPadding;

        /// <summary>
        /// Gets the wrapping cell height for the current <see cref="ThumbnailSize"/>, including the caption.
        /// </summary>
        public int ThumbnailCellHeight => ThumbnailSize + ThumbnailSizes.CaptionHeight;

        /// <summary>
        /// Gets whether Extra Small (48) thumbnails are selected.
        /// </summary>
        public bool IsThumbnailSizeExtraSmall => ThumbnailSize == ThumbnailSizes.ExtraSmall;

        /// <summary>
        /// Gets whether Small (64) thumbnails are selected.
        /// </summary>
        public bool IsThumbnailSizeSmall => ThumbnailSize == ThumbnailSizes.Small;

        /// <summary>
        /// Gets whether Medium (96) thumbnails are selected.
        /// </summary>
        public bool IsThumbnailSizeMedium => ThumbnailSize == ThumbnailSizes.Medium;

        /// <summary>
        /// Gets whether Large (128) thumbnails are selected.
        /// </summary>
        public bool IsThumbnailSizeLarge => ThumbnailSize == ThumbnailSizes.Large;

        /// <summary>
        /// Gets whether Extra Large (192) thumbnails are selected.
        /// </summary>
        public bool IsThumbnailSizeExtraLarge => ThumbnailSize == ThumbnailSizes.ExtraLarge;

        /// <summary>
        /// Gets whether Huge (256) thumbnails are selected.
        /// </summary>
        public bool IsThumbnailSizeHuge => ThumbnailSize == ThumbnailSizes.Huge;

        /// <summary>
        /// Navigates to <see cref="PathText"/> when the user commits the typed path.
        /// </summary>
        [RelayCommand]
        public void CommitPath()
        {
            _Navigate(PathText);
            _EndPathEdit();
        }

        /// <summary>
        /// Switches the address bar to a typed path.
        /// </summary>
        [RelayCommand]
        public void BeginPathEdit()
        {
            if (IsPathEditing)
            {
                return;
            }

            PathText = FileListPath.ToDisplayPath(CurrentPath);
            IsPathEditing = true;
        }

        /// <summary>
        /// Leaves typed-path mode without navigating.
        /// </summary>
        [RelayCommand]
        public void CancelPathEdit()
        {
            _EndPathEdit();
        }

        /// <summary>
        /// Opens the current folder's parent, Network at a UNC share root, or This PC at a volume root.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoUp))]
        public void GoUp()
        {
            var parent = FileListPath.GetParentPath(CurrentPath);
            if (parent is null)
            {
                return;
            }

            _Navigate(parent);
        }

        /// <summary>
        /// Reloads the current folder listing.
        /// </summary>
        [RelayCommand]
        public void Refresh()
        {
            _ReloadEntries(preserveSelection: true);
        }

        /// <summary>
        /// Replaces the current selection. The focused row defaults to the last entry.
        /// </summary>
        /// <param name="entries">Rows to select. Duplicates and <see langword="null"/> items are ignored.</param>
        /// <param name="focusedEntry">Focused row, or <see langword="null"/> to use the last selected entry.</param>
        public void SetSelectedEntries(IReadOnlyList<FileListEntry> entries, FileListEntry? focusedEntry = null)
        {
            _suppressSelectionSync = true;
            try
            {
                _selectedEntries.Clear();
                var pathToIsAdded = new HashSet<string>(PathComparers.Os);
                foreach (var entry in entries)
                {
                    if (entry is null || !pathToIsAdded.Add(entry.FullPath))
                    {
                        continue;
                    }

                    _selectedEntries.Add(entry);
                }

                var focused = focusedEntry;
                if (focused is not null && !pathToIsAdded.Contains(focused.FullPath))
                {
                    focused = null;
                }

                SelectedEntry = focused ?? _selectedEntries.LastOrDefault();
                OnPropertyChanged(nameof(SelectedEntries));
                _NotifySelectionCommandsChanged();
            }
            finally
            {
                _suppressSelectionSync = false;
            }
        }

        /// <summary>
        /// Moves the focused row up or down. Replaces the current selection with the new row.
        /// </summary>
        /// <param name="delta">-1 for up, +1 for down.</param>
        /// <returns><see langword="true"/> when the selection moved.</returns>
        public bool TryMoveSelection(int delta)
        {
            if (delta == 0 || Entries.Count == 0)
            {
                return false;
            }

            var currentIndex = SelectedEntry is { } current ? Entries.IndexOf(current) : -1;
            var nextIndex = currentIndex < 0 ? (delta > 0 ? 0 : Entries.Count - 1) : currentIndex + delta;

            if (nextIndex < 0 || nextIndex >= Entries.Count)
            {
                return false;
            }

            var next = Entries[nextIndex];
            SetSelectedEntries([next], next);
            return true;
        }

        /// <summary>
        /// Opens the focused row: folders navigate in-app; files open with the OS default app.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanOpenSelected))]
        public void OpenSelected()
        {
            if (SelectedEntry is null)
            {
                return;
            }

            if (SelectedEntry.IsDirectory)
            {
                _Navigate(SelectedEntry.FullPath);
                return;
            }

            _shellOpener.OpenWithDefaultApp(SelectedEntry.FullPath);
        }

        /// <summary>
        /// Copies selected full paths to the clipboard, one per line.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanCopyPath))]
        public async Task CopyPathAsync()
        {
            if (_selectedEntries.Count == 0)
            {
                return;
            }

            var text = string.Join(Environment.NewLine, _selectedEntries.Select(entry => entry.FullPath));
            await _clipboard.SetTextAsync(text).ConfigureAwait(true);
        }

        /// <summary>
        /// Reveals the focused selection in the OS file manager, or opens the current folder when empty.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanShowInExplorer))]
        public void ShowInExplorer()
        {
            if (SelectedEntry is not null)
            {
                _shellOpener.RevealInFileManager(SelectedEntry.FullPath);
                return;
            }

            if (!FileListPath.IsFilesystemFolderPath(CurrentPath))
            {
                return;
            }

            _shellOpener.OpenFolderInFileManager(CurrentPath);
        }

        /// <summary>
        /// Reveals the current session log file in the OS file manager.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanShowLogInExplorer))]
        public void ShowLogInExplorer()
        {
            var logFilePath = LogSession.LogFilePath;
            if (string.IsNullOrEmpty(logFilePath))
            {
                return;
            }

            _shellOpener.RevealInFileManager(logFilePath);
        }

        /// <summary>
        /// Switches the File List layout.
        /// </summary>
        /// <param name="mode">Layout to show.</param>
        [RelayCommand]
        public void SetViewMode(FileListViewMode mode)
        {
            ViewMode = mode;
        }

        /// <summary>
        /// Sets the Thumbnails image size to the nearest allowed step.
        /// </summary>
        /// <param name="size">Requested size in pixels.</param>
        [RelayCommand]
        public void SetThumbnailSize(int size)
        {
            ThumbnailSize = ThumbnailSizes.Clamp(size);
        }

        /// <summary>
        /// Moves Thumbnails view to the next larger size step.
        /// </summary>
        [RelayCommand(CanExecute = nameof(IsThumbnailsView))]
        public void ZoomThumbnailsIn()
        {
            ThumbnailSize = ThumbnailSizes.LargerThan(ThumbnailSize);
        }

        /// <summary>
        /// Moves Thumbnails view to the next smaller size step.
        /// </summary>
        [RelayCommand(CanExecute = nameof(IsThumbnailsView))]
        public void ZoomThumbnailsOut()
        {
            ThumbnailSize = ThumbnailSizes.SmallerThan(ThumbnailSize);
        }

        /// <summary>
        /// Restores the default Thumbnails size (96 pixels).
        /// </summary>
        [RelayCommand(CanExecute = nameof(IsThumbnailsView))]
        public void ResetThumbnailSize()
        {
            ThumbnailSize = ThumbnailSizes.Default;
        }

        /// <summary>
        /// Cancels in-flight thumbnail decoding and disposes cached preview bitmaps.
        /// </summary>
        public void Dispose()
        {
            _thumbnails.Dispose();
        }

        /// <summary>
        /// Sorts the listing like Windows Explorer: folders stay first, then the column.
        /// <para>
        /// Clicking the same column again reverses order within the folder group and within the file
        /// group. Folders remain above files in both directions.
        /// </para>
        /// </summary>
        /// <param name="memberPath">
        /// A <see cref="FileListEntry"/> property name such as <c>Name</c> or <c>LastWriteTime</c>.
        /// </param>
        public void SortByColumn(string? memberPath)
        {
            var column = FileListListingSort.NormalizeMemberPath(memberPath);
            if (column == SortMemberPath)
            {
                IsSortAscending = !IsSortAscending;
            }
            else
            {
                SortMemberPath = column;
                IsSortAscending = true;
            }

            FileListListingSort.Apply(_listedItems, SortMemberPath, IsSortAscending);
            _RebuildVisibleEntries(preserveSelection: true);
        }

        /// <summary>
        /// Navigates to a filesystem path, This PC, or Network.
        /// </summary>
        /// <param name="path">
        /// Directory path, empty / <see cref="ComputerDisplayName"/> for drives, or
        /// <see cref="NetworkDisplayName"/> / <c>\\</c> for Network.
        /// </param>
        [RelayCommand]
        public void NavigateTo(string? path)
        {
            _Navigate(path);
        }

        /// <summary>
        /// Navigates the File List to <paramref name="fullPath"/>'s folder and selects that item.
        /// </summary>
        /// <param name="fullPath">Full file or folder path to locate.</param>
        /// <returns><see langword="true"/> when the row was found in the current listing.</returns>
        public bool TryLocatePath(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                return false;
            }

            var directoryPath = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                return false;
            }

            if (!FileListCatalog.TryResolvePath(directoryPath, out var resolvedDirectory))
            {
                return false;
            }

            if (!PathComparers.Os.Equals(resolvedDirectory, CurrentPath))
            {
                _Navigate(resolvedDirectory);
            }

            var match = Entries.FirstOrDefault(entry => PathComparers.Os.Equals(entry.FullPath, fullPath));
            if (match is null)
            {
                return false;
            }

            SetSelectedEntries([match], match);
            return true;
        }

        /// <summary>
        /// Remembers the current include mask after the user commits it (Enter or leave the combo).
        /// </summary>
        public void CommitMask()
        {
            _RememberMask(Mask);
        }

        partial void OnMaskChanged(string value)
        {
            _ReloadEntries(preserveSelection: true);
        }

        partial void OnExcludeMasksChanged(IReadOnlyList<string> value)
        {
            _ReloadEntries(preserveSelection: true);
        }

        partial void OnExcludeMasksEnabledChanged(bool value)
        {
            _ReloadEntries(preserveSelection: true);
        }

        /// <summary>
        /// Applies Exclude Masks dialog results (enable flag and one-mask-per-line text).
        /// </summary>
        /// <param name="enabled">Whether exclude masks are active.</param>
        /// <param name="editorText">Masks as typed in the dialog (one per line).</param>
        public void ApplyExcludeMasks(bool enabled, string? editorText)
        {
            ExcludeMasks = WildcardMask.NormalizeForStorage(editorText);
            ExcludeMasksEnabled = enabled;
        }

        /// <summary>
        /// Restores mask, exclude-mask, and suggestion fields from a session snapshot.
        /// </summary>
        /// <param name="snapshot">Persisted File List session fields.</param>
        internal void ApplySession(FileListSessionSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            if (!string.IsNullOrEmpty(snapshot.FileMask))
            {
                Mask = snapshot.FileMask;
            }

            // Null means unset: keep the defaults. An empty list means the user cleared them.
            if (snapshot.ExcludeMasks is not null)
            {
                ExcludeMasks = [.. snapshot.ExcludeMasks];
            }

            if (snapshot.ExcludeMasksEnabled is { } excludeEnabled)
            {
                ExcludeMasksEnabled = excludeEnabled;
            }

            if (snapshot.MaskSuggestions is { Count: > 0 })
            {
                MaskSuggestions.Clear();
                foreach (var mask in snapshot.MaskSuggestions)
                {
                    MaskSuggestions.Add(mask);
                }
            }
        }

        /// <summary>
        /// Captures current mask, exclude-mask, and suggestion fields for session save.
        /// </summary>
        /// <returns>Snapshot to merge into persisted session state.</returns>
        internal FileListSessionSnapshot CaptureSession()
        {
            return new FileListSessionSnapshot(
                LastOpenedDirectory: CurrentPath,
                FileMask: Mask,
                ExcludeMasks: [.. ExcludeMasks],
                ExcludeMasksEnabled: ExcludeMasksEnabled,
                MaskSuggestions: [.. MaskSuggestions]
            );
        }

        partial void OnViewModeChanged(FileListViewMode value)
        {
            _RebuildVisibleEntries(preserveSelection: true);
        }

        partial void OnThumbnailSizeChanged(int value)
        {
            var clamped = ThumbnailSizes.Clamp(value);
            if (clamped != value)
            {
                ThumbnailSize = clamped;
            }
        }

        partial void OnSelectedEntryChanged(FileListEntry? value)
        {
            if (_suppressSelectionSync)
            {
                return;
            }

            _suppressSelectionSync = true;
            try
            {
                _selectedEntries.Clear();
                if (value is not null)
                {
                    _selectedEntries.Add(value);
                }

                OnPropertyChanged(nameof(SelectedEntries));
                _NotifySelectionCommandsChanged();
            }
            finally
            {
                _suppressSelectionSync = false;
            }
        }

        private bool _CanOpenSelected()
        {
            return SelectedEntry is not null;
        }

        private bool _CanCopyPath()
        {
            return _selectedEntries.Count > 0;
        }

        private bool _CanShowInExplorer()
        {
            if (SelectedEntry is not null)
            {
                return true;
            }

            return FileListPath.IsFilesystemFolderPath(CurrentPath);
        }

        private void _NotifySelectionCommandsChanged()
        {
            OpenSelectedCommand.NotifyCanExecuteChanged();
            CopyPathCommand.NotifyCanExecuteChanged();
            ShowInExplorerCommand.NotifyCanExecuteChanged();
        }

        private void _Navigate(string? path)
        {
            if (!FileListCatalog.TryResolvePath(path, out var resolved))
            {
                return;
            }

            if (PathComparers.Os.Equals(resolved, CurrentPath))
            {
                return;
            }

            CurrentPath = resolved;
            PathText = FileListPath.ToDisplayPath(resolved);
            IsPathEditing = false;
            _RememberPath(PathText);
            _RebuildBreadcrumbs();
            _ReloadEntries();
            _UpdateNavigationFlags();
        }

        private void _EndPathEdit()
        {
            PathText = FileListPath.ToDisplayPath(CurrentPath);
            IsPathEditing = false;
        }

        private void _RebuildBreadcrumbs()
        {
            BreadcrumbSegments.Clear();
            foreach (var segment in FileListPath.BuildBreadcrumbSegments(CurrentPath))
            {
                BreadcrumbSegments.Add(segment);
            }
        }

        private void _ReloadEntries(bool preserveSelection = false)
        {
            _thumbnails.CancelLoad();
            if (!preserveSelection)
            {
                SetSelectedEntries([]);
            }

            Entries.Clear();
            _listedItems.Clear();
            _thumbnails.ClearCache();
            ListingError = string.Empty;

            var result = FileListCatalog.List(CurrentPath, Mask, ExcludeMasksEnabled, ExcludeMasks, PathHistory);
            if (result.Failure != FileListListingFailure.None)
            {
                ListingError = FileListCatalog.FormatListingError(result.Failure);
                _RebuildVisibleEntries(preserveSelection);
                return;
            }

            _listedItems.AddRange(result.Items);
            FileListListingSort.Apply(_listedItems, SortMemberPath, IsSortAscending);
            _RebuildVisibleEntries(preserveSelection);
        }

        private void _RebuildVisibleEntries(bool preserveSelection)
        {
            _thumbnails.CancelLoad();
            var selectedPaths = preserveSelection ? _selectedEntries.Select(entry => entry.FullPath).ToList() : [];
            var focusedPath = preserveSelection ? SelectedEntry?.FullPath : null;
            Entries.Clear();

            foreach (var item in _listedItems)
            {
                Entries.Add(_CreateEntry(item));
            }

            if (selectedPaths.Count == 0)
            {
                SetSelectedEntries([]);
            }
            else
            {
                var pathToIsSelected = selectedPaths.ToHashSet(PathComparers.Os);
                var restored = Entries.Where(entry => pathToIsSelected.Contains(entry.FullPath)).ToList();
                var focused = focusedPath is null
                    ? null
                    : Entries.FirstOrDefault(entry => PathComparers.Os.Equals(entry.FullPath, focusedPath));
                SetSelectedEntries(restored, focused);
            }

            if (ViewMode == FileListViewMode.Thumbnails)
            {
                _thumbnails.BeginLoad(Entries);
            }
        }

        private FileListEntry _CreateEntry(FileListListedItem item)
        {
            return new FileListEntry
            {
                Name = item.Name,
                FullPath = item.Path,
                IsDirectory = item.IsDirectory,
                ListingGroup = item.ListingGroup,
                Icon = _ResolveIcon(item),
                Details = ViewMode == FileListViewMode.Tiles ? FileListEntryDisplay.FormatDetails(item) : string.Empty,
                Type = FileListEntryDisplay.TypeLabel(item),
                DateModifiedDisplay = FileListEntryDisplay.FormatDate(item.LastWriteTime),
                SizeDisplay = item.Length is { } bytes ? FileListEntryDisplay.FormatSize(bytes) : string.Empty,
                LastWriteTime = item.LastWriteTime,
                Length = item.Length,
            };
        }

        private IImage? _ResolveIcon(FileListListedItem item)
        {
            if (ViewMode == FileListViewMode.Thumbnails)
            {
                var cached = _thumbnails.TryGetCached(item.Path);
                if (cached is not null)
                {
                    return cached;
                }

                return _iconProvider.GetIcon(item.Path, item.IsDirectory, ShellIconSize.Jumbo);
            }

            var usesLargeIcon = ViewMode is FileListViewMode.LargeIcons or FileListViewMode.Tiles;
            var size = usesLargeIcon ? ShellIconSize.Large : ShellIconSize.Small;
            return _iconProvider.GetIcon(item.Path, item.IsDirectory, size);
        }

        private void _UpdateNavigationFlags()
        {
            CanGoUp = FileListPath.GetParentPath(CurrentPath) is not null;
        }

        private void _RememberPath(string displayPath)
        {
            if (string.IsNullOrWhiteSpace(displayPath))
            {
                return;
            }

            if (PathHistory.Contains(displayPath, PathComparers.Os))
            {
                return;
            }

            PathHistory.Insert(0, displayPath);
        }

        private void _RememberMask(string mask)
        {
            if (string.IsNullOrWhiteSpace(mask))
            {
                return;
            }

            var existingIndex = -1;
            for (var i = 0; i < MaskSuggestions.Count; i++)
            {
                if (!string.Equals(MaskSuggestions[i], mask, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                existingIndex = i;
                break;
            }

            if (existingIndex == 0)
            {
                return;
            }

            if (existingIndex > 0)
            {
                // Move keeps the same item instance so an editable ComboBox selection stays valid.
                MaskSuggestions.Move(existingIndex, 0);
            }
            else
            {
                MaskSuggestions.Insert(0, mask);
            }

            while (MaskSuggestions.Count > _MaxRememberedMasks)
            {
                MaskSuggestions.RemoveAt(MaskSuggestions.Count - 1);
            }
        }
    }
}
