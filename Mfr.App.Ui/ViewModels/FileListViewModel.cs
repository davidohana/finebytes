using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.App.Ui.Services;
using Mfr.Utils;

namespace Mfr.App.Ui.ViewModels
{
    /// <summary>
    /// File Explorer pane: folder listing with path, mask, exclude masks, and view modes.
    /// </summary>
    public sealed partial class FileListViewModel : ViewModelBase
    {
        /// <summary>
        /// Sentinel path for the Windows drive list ("This PC").
        /// </summary>
        public const string ComputerPath = "";

        /// <summary>
        /// Address-bar label shown when listing drives on Windows.
        /// </summary>
        public const string ComputerDisplayName = "This PC";

        /// <summary>
        /// Address-bar label and path for the filesystem root on Unix.
        /// </summary>
        public const string UnixRootPath = "/";

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

        private readonly ISystemIconProvider _iconProvider;
        private readonly List<string> _backPaths = [];
        private readonly List<string> _forwardPaths = [];
        private readonly List<ListedItem> _listedItems = [];
        private readonly Dictionary<string, IImage?> _pathToThumbnail = new(PathComparers.Os);

        /// <summary>
        /// Initializes the explorer at the user profile folder with the default icon provider.
        /// </summary>
        public FileListViewModel()
            : this(iconProvider: null, initialPath: null)
        {
        }

        /// <summary>
        /// Initializes the explorer.
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
            _Navigate(_ResolveStartPath(initialPath), NavigationKind.Replace);
        }

        /// <summary>
        /// Gets the items shown in the File Explorer pane.
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
        /// Gets include-mask suggestions for the Mask combo.
        /// </summary>
        public ObservableCollection<string> MaskSuggestions { get; }

        /// <summary>
        /// Filesystem path of the current folder, or <see cref="ComputerPath"/> for the drive list.
        /// </summary>
        [ObservableProperty]
        private string _currentPath = ComputerPath;

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
        private FileListViewMode _viewMode = FileListViewMode.Report;

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
        /// Whether <see cref="GoBack"/> has a previous folder.
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(GoBackCommand))]
        private bool _canGoBack;

        /// <summary>
        /// Whether <see cref="GoForward"/> has a next folder.
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(GoForwardCommand))]
        private bool _canGoForward;

        /// <summary>
        /// Whether <see cref="GoUp"/> can move to a parent folder or the drive list.
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
        /// Navigates to <see cref="PathText"/> when the user commits the typed path.
        /// </summary>
        [RelayCommand]
        public void CommitPath()
        {
            _Navigate(PathText, NavigationKind.Direct);
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

            PathText = _ToDisplayPath(CurrentPath);
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
        /// Opens the current folder's parent, or the drive list at a volume root on Windows.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoUp))]
        public void GoUp()
        {
            var parent = _GetParentPath(CurrentPath);
            if (parent is null)
                return;

            _Navigate(parent, NavigationKind.Direct);
        }

        /// <summary>
        /// Goes back in the explorer history.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoBack))]
        public void GoBack()
        {
            if (!_TryPop(_backPaths, out var path))
                return;

            _Navigate(path, NavigationKind.Back);
        }

        /// <summary>
        /// Goes forward in the explorer history.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoForward))]
        public void GoForward()
        {
            if (!_TryPop(_forwardPaths, out var path))
                return;

            _Navigate(path, NavigationKind.Forward);
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

            _Navigate(path, NavigationKind.Direct);
        }

        /// <summary>
        /// Switches the File Explorer layout.
        /// </summary>
        /// <param name="mode">Layout to show.</param>
        [RelayCommand]
        public void SetViewMode(FileListViewMode mode)
        {
            ViewMode = mode;
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
        /// Navigates to a filesystem path or the drive list.
        /// </summary>
        /// <param name="path">Directory path, or empty / <see cref="ComputerDisplayName"/> for drives.</param>
        [RelayCommand]
        public void NavigateTo(string? path)
        {
            _Navigate(path, NavigationKind.Direct);
        }

        partial void OnMaskChanged(string value)
        {
            _RememberMask(value);
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

        private bool _CanOpenSelected()
        {
            return SelectedEntry is { IsDirectory: true };
        }

        private void _Navigate(string? path, NavigationKind kind)
        {
            if (!_TryResolvePath(path, out var resolved))
                return;

            if (kind == NavigationKind.Direct && PathComparers.Os.Equals(resolved, CurrentPath))
                return;

            if (kind == NavigationKind.Direct)
            {
                _Push(_backPaths, CurrentPath);
                _forwardPaths.Clear();
            }
            else if (kind == NavigationKind.Back)
                _Push(_forwardPaths, CurrentPath);
            else if (kind == NavigationKind.Forward)
                _Push(_backPaths, CurrentPath);

            CurrentPath = resolved;
            PathText = _ToDisplayPath(resolved);
            IsPathEditing = false;
            _RememberPath(PathText);
            _RebuildBreadcrumbs();
            _ReloadEntries();
            _UpdateNavigationFlags();
        }

        private void _EndPathEdit()
        {
            PathText = _ToDisplayPath(CurrentPath);
            IsPathEditing = false;
        }

        private void _RebuildBreadcrumbs()
        {
            BreadcrumbSegments.Clear();
            foreach (var segment in _BuildBreadcrumbSegments(CurrentPath))
                BreadcrumbSegments.Add(segment);
        }

        private void _ReloadEntries()
        {
            SelectedEntry = null;
            _listedItems.Clear();
            _pathToThumbnail.Clear();

            if (_IsComputerPath(CurrentPath))
            {
                _listedItems.AddRange(_ListDrives());
                _ApplyListingSort();
                _RebuildVisibleEntries(preserveSelection: false);
                return;
            }

            try
            {
                var folders = Directory.EnumerateDirectories(CurrentPath, "*", _ListingOptions)
                    .Select(path => _CreateListedItem(path, isDirectory: true));

                var files = Directory.EnumerateFiles(CurrentPath, "*", _ListingOptions)
                    .Where(_PassesFileMasks)
                    .Select(path => _CreateListedItem(path, isDirectory: false));

                _listedItems.AddRange(folders);
                _listedItems.AddRange(files);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                _RebuildVisibleEntries(preserveSelection: false);
                return;
            }

            _ApplyListingSort();
            _RebuildVisibleEntries(preserveSelection: false);
        }

        private void _ApplyListingSort()
        {
            _listedItems.Sort(_CompareListedItems);
        }

        private int _CompareListedItems(ListedItem left, ListedItem right)
        {
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
            var selectedPath = preserveSelection ? SelectedEntry?.FullPath : null;
            Entries.Clear();

            foreach (var item in _listedItems)
                Entries.Add(_CreateEntry(item));

            if (selectedPath is null)
            {
                SelectedEntry = null;
                return;
            }

            SelectedEntry = Entries.FirstOrDefault(entry => PathComparers.Os.Equals(entry.FullPath, selectedPath));
        }

        private FileListEntry _CreateEntry(ListedItem item)
        {
            return new FileListEntry
            {
                Name = item.Name,
                FullPath = item.Path,
                IsDirectory = item.IsDirectory,
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
                var thumbnail = _TryGetThumbnail(item);
                if (thumbnail is not null)
                    return thumbnail;

                return _iconProvider.GetIcon(item.Path, item.IsDirectory, ShellIconSize.Large);
            }

            var usesLargeIcon = ViewMode is FileListViewMode.LargeIcons or FileListViewMode.Tiles;
            var size = usesLargeIcon ? ShellIconSize.Large : ShellIconSize.Small;
            return _iconProvider.GetIcon(item.Path, item.IsDirectory, size);
        }

        private IImage? _TryGetThumbnail(ListedItem item)
        {
            if (item.IsDirectory)
                return null;

            if (_pathToThumbnail.TryGetValue(item.Path, out var cached))
                return cached;

            var thumbnail = ImageThumbnailLoader.TryLoad(item.Path, item.Length);
            _pathToThumbnail[item.Path] = thumbnail;
            return thumbnail;
        }

        private static ListedItem _CreateListedItem(string path, bool isDirectory)
        {
            var name = isDirectory ? _DirectoryDisplayName(path) : Path.GetFileName(path);
            if (isDirectory)
            {
                return new ListedItem(
                    path,
                    name,
                    IsDirectory: true,
                    Length: null,
                    LastWriteTime: _TryGetLastWriteTime(path));
            }

            var (length, lastWriteTime) = _TryGetFileInfo(path);
            return new ListedItem(
                path,
                name,
                IsDirectory: false,
                Length: length,
                LastWriteTime: lastWriteTime);
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

                items.Add(_CreateListedItem(name, isDirectory: true));
            }

            return items;
        }

        private void _UpdateNavigationFlags()
        {
            CanGoBack = _backPaths.Count > 0;
            CanGoForward = _forwardPaths.Count > 0;
            CanGoUp = _GetParentPath(CurrentPath) is not null;
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
            if (_TryResolvePath(initialPath, out var resolved) && !_IsComputerPath(resolved))
                return resolved;

            var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (_TryResolvePath(profile, out resolved) && !_IsComputerPath(resolved))
                return resolved;

            return Directory.GetCurrentDirectory();
        }

        private static bool _TryResolvePath(string? path, [NotNullWhen(true)] out string resolved)
        {
            if (_IsComputerPath(path))
            {
                if (!OperatingSystem.IsWindows())
                {
                    resolved = ComputerPath;
                    return false;
                }

                resolved = ComputerPath;
                return true;
            }

            try
            {
                resolved = new DirectoryInfo(path!).FullName;
                return Directory.Exists(resolved);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or IOException or UnauthorizedAccessException)
            {
                resolved = ComputerPath;
                return false;
            }
        }

        private static string? _GetParentPath(string path)
        {
            if (_IsComputerPath(path))
                return null;

            var parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent))
                return parent;

            return OperatingSystem.IsWindows() ? ComputerPath : null;
        }

        private static string _ToDisplayPath(string path)
        {
            return _IsComputerPath(path) ? ComputerDisplayName : path;
        }

        private static List<PathBreadcrumbSegment> _BuildBreadcrumbSegments(string path)
        {
            if (OperatingSystem.IsWindows())
                return _BuildWindowsBreadcrumbSegments(path);

            return _BuildUnixBreadcrumbSegments(path);
        }

        private static List<PathBreadcrumbSegment> _BuildWindowsBreadcrumbSegments(string path)
        {
            var segments = new List<PathBreadcrumbSegment>();
            if (_IsComputerPath(path))
            {
                segments.Add(_CreateSegment(
                    ComputerDisplayName,
                    ComputerDisplayName,
                    showLeadingChevron: false));
                return segments;
            }

            var isUnc = path.StartsWith(@"\\", StringComparison.Ordinal);
            if (!isUnc)
                segments.Add(_CreateSegment(
                    ComputerDisplayName,
                    ComputerDisplayName,
                    showLeadingChevron: false));

            var root = Path.GetPathRoot(path);
            if (string.IsNullOrEmpty(root))
                return segments;

            var rootLabel = isUnc ? root.TrimTrailingSeparator() : _FormatDriveLabel(root);
            segments.Add(_CreateSegment(rootLabel, root, showLeadingChevron: segments.Count > 0));
            _AddChildBreadcrumbSegments(segments, path, root);
            return segments;
        }

        private static List<PathBreadcrumbSegment> _BuildUnixBreadcrumbSegments(string path)
        {
            var segments = new List<PathBreadcrumbSegment>
            {
                _CreateSegment(UnixRootPath, UnixRootPath, showLeadingChevron: false),
            };

            if (_IsComputerPath(path) || _IsSamePath(path, UnixRootPath))
                return segments;

            _AddChildBreadcrumbSegments(segments, path, UnixRootPath);
            return segments;
        }

        private static void _AddChildBreadcrumbSegments(
            List<PathBreadcrumbSegment> segments,
            string path,
            string rootPath)
        {
            DirectoryInfo? current;
            string rootFullName;
            try
            {
                current = new DirectoryInfo(path);
                rootFullName = new DirectoryInfo(rootPath).FullName;
            }
            catch (Exception ex) when (
                ex is ArgumentException or NotSupportedException or IOException)
            {
                return;
            }

            var parts = new List<PathBreadcrumbSegment>();
            while (current is not null && !_IsSamePath(current.FullName, rootFullName))
            {
                var name = current.Name;
                if (string.IsNullOrEmpty(name))
                    break;

                parts.Add(_CreateSegment(name, current.FullName, showLeadingChevron: true));
                current = current.Parent;
            }

            parts.Reverse();
            segments.AddRange(parts);
        }

        private static PathBreadcrumbSegment _CreateSegment(
            string label,
            string targetPath,
            bool showLeadingChevron)
        {
            return new PathBreadcrumbSegment
            {
                Label = label,
                TargetPath = targetPath,
                ShowLeadingChevron = showLeadingChevron,
            };
        }

        private static string _FormatDriveLabel(string root)
        {
            try
            {
                var drive = new DriveInfo(root);
                var letter = drive.Name.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);
                if (drive.IsReady)
                {
                    var volume = drive.VolumeLabel;
                    if (!string.IsNullOrWhiteSpace(volume))
                        return volume + " (" + letter + ")";
                }

                return letter;
            }
            catch (Exception ex) when (
                ex is ArgumentException or IOException or UnauthorizedAccessException)
            {
                return root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
        }

        private static bool _IsSamePath(string first, string second)
        {
            var firstPath = first.TrimTrailingSeparator();
            var secondPath = second.TrimTrailingSeparator();
            return PathComparers.Os.Equals(firstPath, secondPath);
        }

        private static bool _IsComputerPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return true;

            return path.Equals(ComputerDisplayName, StringComparison.OrdinalIgnoreCase);
        }

        private static string _DirectoryDisplayName(string path)
        {
            var name = Path.GetFileName(path.TrimTrailingSeparator());
            return string.IsNullOrEmpty(name) ? path : name;
        }

        private static void _Push(List<string> stack, string path)
        {
            if (stack.Count > 0 && PathComparers.Os.Equals(stack[^1], path))
                return;

            stack.Add(path);
        }

        private static bool _TryPop(List<string> stack, [NotNullWhen(true)] out string? path)
        {
            if (stack.Count == 0)
            {
                path = null;
                return false;
            }

            path = stack[^1];
            stack.RemoveAt(stack.Count - 1);
            return true;
        }

        private enum NavigationKind
        {
            Replace,
            Direct,
            Back,
            Forward,
        }

        private sealed record ListedItem(
            string Path,
            string Name,
            bool IsDirectory,
            long? Length,
            DateTime? LastWriteTime);
    }
}
