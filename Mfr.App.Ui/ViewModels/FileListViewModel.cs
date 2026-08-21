using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.App.Ui.Services.FileList;
using Mfr.Utils;

namespace Mfr.App.Ui.ViewModels
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

        private static readonly EnumerationOptions _ListingOptions = new()
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = false,
            ReturnSpecialDirectories = false,
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System,
        };

        // Caps how long a disconnected UNC or mapped drive may block Exists/enumerate.
        // The OS SMB timeout cannot be cancelled; this bound keeps the File List responsive.
        private static readonly TimeSpan _NetworkProbeTimeout = TimeSpan.FromSeconds(3);

        // First contact with a UNC server (\\ohanas) is often slower than a share Exists check.
        private static readonly TimeSpan _UncServerProbeTimeout = TimeSpan.FromSeconds(8);

        private const int _VolumeListingGroup = 0;
        private const int _KnownPlaceListingGroup = 1;
        private const int _ThumbnailLoadParallelismCap = 4;

        private readonly ISystemIconProvider _iconProvider;
        private readonly List<ListedItem> _listedItems = [];
        private readonly Dictionary<string, IImage?> _pathToThumbnail = new(PathComparers.Os);
        private CancellationTokenSource? _thumbnailLoadCts;

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
        public FileListViewModel(ISystemIconProvider? iconProvider, string? initialPath)
        {
            _iconProvider = iconProvider ?? SystemIconProvider.CreateDefault();
            Entries = [];
            MaskSuggestions = [.. _DefaultMasks];
            PathHistory = [];
            BreadcrumbSegments = [];
            _Navigate(_ResolveStartPath(initialPath));
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
        /// <c>:</c>- or <c>;</c>-delimited exclude masks applied to file names.
        /// </summary>
        [ObservableProperty]
        private string _excludeMasks = string.Empty;

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
        /// The selected grid row, or <see langword="null"/> when nothing is selected.
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(OpenSelectedCommand))]
        private FileListEntry? _selectedEntry;

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
                return;

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
                return;

            _Navigate(parent);
        }

        /// <summary>
        /// Reloads the current folder listing.
        /// </summary>
        [RelayCommand]
        public void Refresh()
        {
            _ReloadEntries();
        }

        /// <summary>
        /// Navigates into the selected folder. Files are ignored until add-to-list (G2).
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanOpenSelected))]
        public void OpenSelected()
        {
            if (SelectedEntry is not { IsDirectory: true, FullPath: var path })
                return;

            _Navigate(path);
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
            _CancelThumbnailLoad();
            _DisposeAndClearThumbnails();
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
            var column = _NormalizeSortMemberPath(memberPath);
            if (column == SortMemberPath)
                IsSortAscending = !IsSortAscending;
            else
            {
                SortMemberPath = column;
                IsSortAscending = true;
            }

            _ApplyListingSort();
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
        /// Remembers the current include mask after the user commits it (Enter or leave the combo).
        /// </summary>
        public void CommitMask()
        {
            _RememberMask(Mask);
        }

        partial void OnMaskChanged(string value)
        {
            _ReloadEntries();
        }

        partial void OnExcludeMasksChanged(string value)
        {
            _ReloadEntries();
        }

        partial void OnViewModeChanged(FileListViewMode value)
        {
            _RebuildVisibleEntries(preserveSelection: true);
        }

        partial void OnThumbnailSizeChanged(int value)
        {
            var clamped = ThumbnailSizes.Clamp(value);
            if (clamped != value)
                ThumbnailSize = clamped;
        }

        private bool _CanOpenSelected()
        {
            return SelectedEntry is { IsDirectory: true };
        }

        private void _Navigate(string? path)
        {
            if (!_TryResolvePath(path, out var resolved))
                return;

            if (PathComparers.Os.Equals(resolved, CurrentPath))
                return;

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
                BreadcrumbSegments.Add(segment);
        }

        private void _ReloadEntries()
        {
            _CancelThumbnailLoad();
            SelectedEntry = null;
            Entries.Clear();
            _listedItems.Clear();
            _DisposeAndClearThumbnails();

            if (FileListPath.IsComputerPath(CurrentPath))
            {
                _listedItems.AddRange(_ListKnownPlaces());
                _listedItems.AddRange(_ListDrives());
                if (OperatingSystem.IsWindows())
                    _listedItems.Add(_CreateNetworkRootItem());

                _ApplyListingSort();
                _RebuildVisibleEntries(preserveSelection: false);
                return;
            }

            if (FileListPath.IsNetworkPath(CurrentPath))
            {
                _listedItems.AddRange(_ListNetworkLocations());
                _ApplyListingSort();
                _RebuildVisibleEntries(preserveSelection: false);
                return;
            }

            if (OperatingSystem.IsWindows() && WindowsWslUnc.IsWslServerRoot(CurrentPath))
            {
                _listedItems.AddRange(_ListWslDistros(CurrentPath));
                _ApplyListingSort();
                _RebuildVisibleEntries(preserveSelection: false);
                return;
            }

            if (FileListPath.IsUncServerRoot(CurrentPath))
            {
                if (OperatingSystem.IsWindows())
                    _listedItems.AddRange(_ListUncShares(CurrentPath));

                _ApplyListingSort();
                _RebuildVisibleEntries(preserveSelection: false);
                return;
            }

            if (!_TryListFolder(CurrentPath, out var folders, out var files))
            {
                _RebuildVisibleEntries(preserveSelection: false);
                return;
            }

            _listedItems.AddRange(folders);
            _listedItems.AddRange(files);
            _ApplyListingSort();
            _RebuildVisibleEntries(preserveSelection: false);
        }

        private void _ApplyListingSort()
        {
            _listedItems.Sort(_CompareListedItems);
        }

        private int _CompareListedItems(ListedItem left, ListedItem right)
        {
            var groupCmp = left.ListingGroup.CompareTo(right.ListingGroup);
            if (groupCmp != 0)
                return groupCmp;

            var folderCmp = right.IsDirectory.CompareTo(left.IsDirectory);
            if (folderCmp != 0)
                return folderCmp;

            var fieldCmp = _CompareSortField(left, right);
            if (fieldCmp == 0)
                fieldCmp = PathComparers.Os.Compare(left.Name, right.Name);

            return IsSortAscending ? fieldCmp : -fieldCmp;
        }

        private int _CompareSortField(ListedItem left, ListedItem right)
        {
            if (SortMemberPath == nameof(FileListEntry.LastWriteTime))
                return Comparer<DateTime?>.Default.Compare(left.LastWriteTime, right.LastWriteTime);

            if (SortMemberPath == nameof(FileListEntry.Length))
                return Comparer<long?>.Default.Compare(left.Length, right.Length);

            if (SortMemberPath == nameof(FileListEntry.Type))
                return PathComparers.Os.Compare(_TypeLabel(left), _TypeLabel(right));

            return PathComparers.Os.Compare(left.Name, right.Name);
        }

        private static string _NormalizeSortMemberPath(string? memberPath)
        {
            if (string.Equals(memberPath, nameof(FileListEntry.LastWriteTime), StringComparison.Ordinal))
                return nameof(FileListEntry.LastWriteTime);

            if (string.Equals(memberPath, nameof(FileListEntry.Type), StringComparison.Ordinal))
                return nameof(FileListEntry.Type);

            if (string.Equals(memberPath, nameof(FileListEntry.Length), StringComparison.Ordinal))
                return nameof(FileListEntry.Length);

            return nameof(FileListEntry.Name);
        }

        private void _RebuildVisibleEntries(bool preserveSelection)
        {
            _CancelThumbnailLoad();
            var selectedPath = preserveSelection ? SelectedEntry?.FullPath : null;
            Entries.Clear();

            foreach (var item in _listedItems)
                Entries.Add(_CreateEntry(item));

            SelectedEntry = selectedPath is null
                ? null
                : Entries.FirstOrDefault(entry => PathComparers.Os.Equals(entry.FullPath, selectedPath));

            if (ViewMode == FileListViewMode.Thumbnails)
                _StartThumbnailLoad();
        }

        private FileListEntry _CreateEntry(ListedItem item)
        {
            return new FileListEntry
            {
                Name = item.Name,
                FullPath = item.Path,
                IsDirectory = item.IsDirectory,
                ListingGroup = item.ListingGroup,
                Icon = _ResolveIcon(item),
                Details = ViewMode == FileListViewMode.Tiles ? _FormatDetails(item) : string.Empty,
                Type = _TypeLabel(item),
                DateModifiedDisplay = _FormatDate(item.LastWriteTime),
                SizeDisplay = item.Length is { } bytes ? _FormatSize(bytes) : string.Empty,
                LastWriteTime = item.LastWriteTime,
                Length = item.Length,
            };
        }

        private IImage? _ResolveIcon(ListedItem item)
        {
            if (ViewMode == FileListViewMode.Thumbnails)
            {
                if (_pathToThumbnail.TryGetValue(item.Path, out var cached) && cached is not null)
                    return cached;

                return _iconProvider.GetIcon(item.Path, item.IsDirectory, ShellIconSize.Jumbo);
            }

            var usesLargeIcon = ViewMode is FileListViewMode.LargeIcons or FileListViewMode.Tiles;
            var size = usesLargeIcon ? ShellIconSize.Large : ShellIconSize.Small;
            return _iconProvider.GetIcon(item.Path, item.IsDirectory, size);
        }

        private void _StartThumbnailLoad()
        {
            var pending = new List<FileListEntry>();
            foreach (var entry in Entries)
            {
                if (entry.IsDirectory)
                    continue;
                if (_pathToThumbnail.ContainsKey(entry.FullPath))
                    continue;
                if (!ImageThumbnailLoader.CanLoad(entry.FullPath, entry.Length))
                    continue;

                pending.Add(entry);
            }

            if (pending.Count == 0)
                return;

            var cts = new CancellationTokenSource();
            _thumbnailLoadCts = cts;
            var token = cts.Token;
            var loadTask = _LoadThumbnailsAsync(pending, token);
            _ = loadTask.ContinueWith(
                static completed => completed.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default
            );
        }

        private async Task _LoadThumbnailsAsync(IReadOnlyList<FileListEntry> pending, CancellationToken token)
        {
            var options = new ParallelOptions
            {
                CancellationToken = token,
                MaxDegreeOfParallelism = Math.Min(_ThumbnailLoadParallelismCap, Environment.ProcessorCount),
            };

            try
            {
                await Parallel.ForEachAsync(
                    pending,
                    options,
                    (entry, ct) =>
                    {
                        var thumbnail = ImageThumbnailLoader.TryLoad(entry.FullPath, entry.Length, ThumbnailSizes.Huge);
                        if (ct.IsCancellationRequested)
                        {
                            _DisposeImage(thumbnail);
                            return ValueTask.CompletedTask;
                        }

                        _PostToUi(() => _ApplyThumbnail(entry, thumbnail, ct));
                        return ValueTask.CompletedTask;
                    }
                );
            }
            catch (OperationCanceledException) { }
        }

        private void _ApplyThumbnail(FileListEntry entry, IImage? thumbnail, CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                _DisposeImage(thumbnail);
                return;
            }

            _pathToThumbnail[entry.FullPath] = thumbnail;
            if (thumbnail is not null)
                entry.Icon = thumbnail;
        }

        private void _CancelThumbnailLoad()
        {
            if (_thumbnailLoadCts is null)
                return;

            _thumbnailLoadCts.Cancel();
            _thumbnailLoadCts.Dispose();
            _thumbnailLoadCts = null;
        }

        private void _DisposeAndClearThumbnails()
        {
            foreach (var image in _pathToThumbnail.Values)
                _DisposeImage(image);

            _pathToThumbnail.Clear();
        }

        private static void _DisposeImage(IImage? image)
        {
            if (image is IDisposable disposable)
                disposable.Dispose();
        }

        private static void _PostToUi(Action action)
        {
            if (Application.Current is null)
            {
                action();
                return;
            }

            Dispatcher.UIThread.Post(action);
        }

        private static ListedItem _CreateListedItem(string path, bool isDirectory, int listingGroup = 0)
        {
            var name = isDirectory ? _DirectoryDisplayName(path) : Path.GetFileName(path);
            if (isDirectory)
            {
                return new ListedItem(
                    path,
                    name,
                    IsDirectory: true,
                    Length: null,
                    LastWriteTime: _TryGetLastWriteTime(path),
                    ListingGroup: listingGroup
                );
            }

            var (length, lastWriteTime) = _TryGetFileInfo(path);
            return new ListedItem(path, name, IsDirectory: false, Length: length, LastWriteTime: lastWriteTime);
        }

        private bool _PassesFileMasks(string path)
        {
            var fileName = Path.GetFileName(path);
            if (!WildcardMask.IsMatch(fileName, Mask))
                return false;

            return !WildcardMask.MatchesAny(fileName, ExcludeMasks);
        }

        private List<ListedItem> _ListDrives()
        {
            DriveInfo[] drives;
            try
            {
                drives = DriveInfo.GetDrives();
            }
            catch (IOException)
            {
                return [];
            }

            var items = new List<ListedItem>();
            foreach (var drive in drives)
            {
                string name;
                try
                {
                    name = drive.Name;
                }
                catch (IOException)
                {
                    continue;
                }

                items.Add(_CreateListedItem(name, isDirectory: true, listingGroup: _VolumeListingGroup));
            }

            return items;
        }

        private static List<ListedItem> _ListKnownPlaces()
        {
            var items = new List<ListedItem>();
            foreach (var place in WindowsKnownPlaces.GetPlaces())
            {
                items.Add(
                    new ListedItem(
                        place.Path,
                        place.Name,
                        IsDirectory: true,
                        Length: null,
                        LastWriteTime: _TryGetLastWriteTime(place.Path),
                        ListingGroup: _KnownPlaceListingGroup
                    )
                );
            }

            return items;
        }

        private List<ListedItem> _ListNetworkLocations()
        {
            var items = new List<ListedItem>();
            var pathToIsAdded = new HashSet<string>(PathComparers.Os);

            foreach (var drive in _ListNetworkDrives())
            {
                if (!pathToIsAdded.Add(drive.Path))
                    continue;

                items.Add(drive);
            }

            if (
                OperatingSystem.IsWindows()
                && WindowsWslUnc.TryGetLiveRoot(out var wslRoot)
                && pathToIsAdded.Add(wslRoot)
            )
            {
                items.Add(new ListedItem(wslRoot, wslRoot[2..], IsDirectory: true, Length: null, LastWriteTime: null));
            }

            foreach (var historyPath in PathHistory)
            {
                if (!FileListPath.IsUncPath(historyPath))
                    continue;

                var location = historyPath.TrimTrailingSeparator();
                if (!pathToIsAdded.Add(location))
                    continue;

                items.Add(new ListedItem(location, location, IsDirectory: true, Length: null, LastWriteTime: null));
            }

            return items;
        }

        private static List<ListedItem> _ListWslDistros(string serverRoot)
        {
            if (!WindowsWslUnc.TryListDistroPaths(serverRoot, out var distroPaths))
                return [];

            var items = new List<ListedItem>();
            foreach (var distroPath in distroPaths)
            {
                var name = _LastUncSegment(distroPath);
                items.Add(new ListedItem(distroPath, name, IsDirectory: true, Length: null, LastWriteTime: null));
            }

            return items;
        }

        [SupportedOSPlatform("windows")]
        private List<ListedItem> _ListUncShares(string serverRoot)
        {
            if (
                !_TryRunWithTimeout(() => _TryReadUncShares(serverRoot), _UncServerProbeTimeout, out var sharePaths)
                || sharePaths is null
            )
                return [];

            var items = new List<ListedItem>();
            foreach (var sharePath in sharePaths)
            {
                var name = Path.GetFileName(sharePath.TrimTrailingSeparator());
                items.Add(
                    new ListedItem(
                        sharePath,
                        string.IsNullOrEmpty(name) ? sharePath : name,
                        IsDirectory: true,
                        Length: null,
                        LastWriteTime: null
                    )
                );
            }

            return items;
        }

        [SupportedOSPlatform("windows")]
        private static List<string>? _TryReadUncShares(string serverRoot)
        {
            if (!WindowsUncShareLister.TryListDiskShares(serverRoot, out var sharePaths))
                return null;

            return sharePaths;
        }

        [SupportedOSPlatform("windows")]
        private static bool _UncServerIsReachable(string serverRoot)
        {
            return _TryRunWithTimeout(
                    () => _TryReadUncShares(serverRoot) is not null,
                    _UncServerProbeTimeout,
                    out var reachable
                ) && reachable;
        }

        private static List<ListedItem> _ListNetworkDrives()
        {
            DriveInfo[] drives;
            try
            {
                drives = DriveInfo.GetDrives();
            }
            catch (IOException)
            {
                return [];
            }

            var items = new List<ListedItem>();
            foreach (var drive in drives)
            {
                DriveType driveType;
                string name;
                try
                {
                    driveType = drive.DriveType;
                    name = drive.Name;
                }
                catch (IOException)
                {
                    continue;
                }

                if (driveType != DriveType.Network)
                    continue;

                items.Add(_CreateListedItem(name, isDirectory: true));
            }

            return items;
        }

        private static ListedItem _CreateNetworkRootItem()
        {
            return new ListedItem(
                NetworkPath,
                NetworkDisplayName,
                IsDirectory: true,
                Length: null,
                LastWriteTime: null,
                ListingGroup: _KnownPlaceListingGroup
            );
        }

        private bool _TryListFolder(string path, out List<ListedItem> folders, out List<ListedItem> files)
        {
            folders = [];
            files = [];
            try
            {
                if (!_NeedsNetworkTimeout(path))
                {
                    (folders, files) = _ReadFolderListing(path);
                    return true;
                }

                var timeout = WindowsWslUnc.IsWslUncPath(path) ? _UncServerProbeTimeout : _NetworkProbeTimeout;
                if (!_TryRunWithTimeout(() => _ReadFolderListing(path), timeout, out var listing))
                    return false;

                folders = listing.Folders;
                files = listing.Files;
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return false;
            }
        }

        private (List<ListedItem> Folders, List<ListedItem> Files) _ReadFolderListing(string path)
        {
            var folders = Directory
                .EnumerateDirectories(path, "*", _ListingOptions)
                .Select(folderPath => _CreateListedItem(folderPath, isDirectory: true))
                .ToList();

            var files = Directory
                .EnumerateFiles(path, "*", _ListingOptions)
                .Where(_PassesFileMasks)
                .Select(filePath => _CreateListedItem(filePath, isDirectory: false))
                .ToList();

            return (folders, files);
        }

        private static bool _DirectoryExists(string path)
        {
            if (!_NeedsNetworkTimeout(path))
                return Directory.Exists(path);

            return _TryRunWithTimeout(() => Directory.Exists(path), _NetworkProbeTimeout, out var exists) && exists;
        }

        private static bool _NeedsNetworkTimeout(string path)
        {
            if (FileListPath.IsUncPath(path))
                return true;

            try
            {
                var root = Path.GetPathRoot(path);
                if (string.IsNullOrEmpty(root))
                    return false;

                return new DriveInfo(root).DriveType == DriveType.Network;
            }
            catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }

        private static bool _TryRunWithTimeout<T>(Func<T> action, TimeSpan timeout, out T result)
        {
            var task = Task.Run(action);
            try
            {
                if (task.Wait(timeout))
                {
                    result = task.Result;
                    return true;
                }
            }
            catch (AggregateException)
            {
                result = default!;
                return false;
            }

            // Exists/enumerate cannot be cancelled; observe later faults so they are not unhandled.
            _ = task.ContinueWith(
                static completed => completed.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default
            );
            result = default!;
            return false;
        }

        private void _UpdateNavigationFlags()
        {
            CanGoUp = FileListPath.GetParentPath(CurrentPath) is not null;
        }

        private void _RememberPath(string displayPath)
        {
            if (string.IsNullOrWhiteSpace(displayPath))
                return;

            if (PathHistory.Contains(displayPath, PathComparers.Os))
                return;

            PathHistory.Insert(0, displayPath);
        }

        private void _RememberMask(string mask)
        {
            if (string.IsNullOrWhiteSpace(mask))
                return;

            if (MaskSuggestions.Contains(mask, StringComparer.OrdinalIgnoreCase))
                return;

            MaskSuggestions.Add(mask);
        }

        private static string _FormatDetails(ListedItem item)
        {
            var typeLabel = _TypeLabel(item);
            if (item.IsDirectory || item.Length is null)
                return typeLabel;

            return typeLabel + "\n" + _FormatSize(item.Length.Value);
        }

        private static string _TypeLabel(ListedItem item)
        {
            if (FileListPath.IsNetworkPath(item.Path))
                return "Network location";

            if (item.IsDirectory)
                return "File folder";

            var extension = Path.GetExtension(item.Name);
            if (string.IsNullOrEmpty(extension))
                return "File";

            return extension.TrimStart('.').ToUpperInvariant() + " File";
        }

        private static string _FormatDate(DateTime? lastWriteTime)
        {
            if (lastWriteTime is null)
                return string.Empty;

            return lastWriteTime.Value.ToString("g", CultureInfo.CurrentCulture);
        }

        private static string _FormatSize(long bytes)
        {
            const double kb = 1024;
            const double mb = kb * 1024;
            const double gb = mb * 1024;

            if (bytes >= gb)
                return (bytes / gb).ToString("0.#", CultureInfo.InvariantCulture) + " GB";
            if (bytes >= mb)
                return (bytes / mb).ToString("0.#", CultureInfo.InvariantCulture) + " MB";
            if (bytes >= kb)
                return (bytes / kb).ToString("0.#", CultureInfo.InvariantCulture) + " KB";

            return bytes.ToString(CultureInfo.InvariantCulture) + " B";
        }

        private static (long? Length, DateTime? LastWriteTime) _TryGetFileInfo(string path)
        {
            try
            {
                var info = new FileInfo(path);
                return (info.Length, info.LastWriteTime);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return (null, null);
            }
        }

        private static DateTime? _TryGetLastWriteTime(string path)
        {
            try
            {
                return Directory.GetLastWriteTime(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return null;
            }
        }

        private static string _ResolveStartPath(string? initialPath)
        {
            if (_TryResolvePath(initialPath, out var resolved) && !FileListPath.IsComputerPath(resolved))
                return resolved;

            var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (_TryResolvePath(profile, out resolved) && !FileListPath.IsComputerPath(resolved))
                return resolved;

            return Directory.GetCurrentDirectory();
        }

        private static bool _TryResolvePath(string? path, [NotNullWhen(true)] out string resolved)
        {
            if (FileListPath.IsComputerPath(path))
            {
                if (!OperatingSystem.IsWindows())
                {
                    resolved = ComputerPath;
                    return false;
                }

                resolved = ComputerPath;
                return true;
            }

            if (FileListPath.IsNetworkPath(path))
            {
                resolved = NetworkPath;
                return true;
            }

            if (WindowsKnownPlaces.TryResolveAlias(path, out var aliasPath))
            {
                resolved = aliasPath;
                return true;
            }

            if (OperatingSystem.IsWindows() && WindowsWslUnc.IsWslUncPath(path))
            {
                if (WindowsWslUnc.TryResolve(path, out var wslPath))
                {
                    resolved = wslPath;
                    return true;
                }

                resolved = ComputerPath;
                return false;
            }

            if (OperatingSystem.IsWindows())
            {
                var isUncServer = path is not null && FileListPath.IsUncServerRoot(path);
                if (isUncServer && FileListPath.TryGetUncServerRoot(path!, out var serverRoot))
                {
                    resolved = serverRoot;
                    return _UncServerIsReachable(serverRoot);
                }
            }

            try
            {
                var expanded = Environment.ExpandEnvironmentVariables(path!);
                if (FileListPath.TryGetDriveRoot(expanded, out var driveRoot))
                    expanded = driveRoot;

                resolved = new DirectoryInfo(expanded).FullName;
                return _DirectoryExists(resolved);
            }
            catch (Exception ex)
                when (ex is ArgumentException or NotSupportedException or IOException or UnauthorizedAccessException)
            {
                resolved = ComputerPath;
                return false;
            }
        }

        private static string _DirectoryDisplayName(string path)
        {
            var name = Path.GetFileName(path.TrimTrailingSeparator());
            return string.IsNullOrEmpty(name) ? _LastUncSegment(path) : name;
        }

        private static string _LastUncSegment(string path)
        {
            var trimmed = path.TrimTrailingSeparator();
            var slash = trimmed.LastIndexOf('\\');
            if (slash < 0 || slash == trimmed.Length - 1)
                return trimmed;

            return trimmed[(slash + 1)..];
        }

        private sealed record ListedItem(
            string Path,
            string Name,
            bool IsDirectory,
            long? Length,
            DateTime? LastWriteTime,
            int ListingGroup = 0
        );
    }
}
