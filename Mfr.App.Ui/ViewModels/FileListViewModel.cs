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
        /// Combo text shown when listing drives.
        /// </summary>
        public const string ComputerDisplayName = "This PC";

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
            _Navigate(_ResolveStartPath(initialPath), NavigationKind.Replace);
        }

        /// <summary>
        /// Gets the items shown in the File Explorer pane.
        /// </summary>
        public ObservableCollection<FileListEntry> Entries { get; }

        /// <summary>
        /// Gets recent and suggested filesystem paths for the path combo.
        /// </summary>
        public ObservableCollection<string> PathHistory { get; }

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
        /// Editable path combo text (display name for the drive list).
        /// </summary>
        [ObservableProperty]
        private string _pathText = string.Empty;

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
        /// Navigates to <see cref="PathText"/> when the user commits the path combo.
        /// </summary>
        [RelayCommand]
        public void CommitPath()
        {
            _Navigate(PathText, NavigationKind.Direct);
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
        /// Navigates to a filesystem path or the drive list.
        /// </summary>
        /// <param name="path">Directory path, or empty / <see cref="ComputerDisplayName"/> for drives.</param>
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
            _RememberPath(PathText);
            _ReloadEntries();
            _UpdateNavigationFlags();
        }

        private void _ReloadEntries()
        {
            SelectedEntry = null;
            _listedItems.Clear();
            _pathToThumbnail.Clear();

            if (_IsComputerPath(CurrentPath))
            {
                _listedItems.AddRange(_ListDrives());
                _RebuildVisibleEntries(preserveSelection: false);
                return;
            }

            try
            {
                var folders = Directory.EnumerateDirectories(CurrentPath, "*", _ListingOptions)
                    .Select(path => _CreateListedItem(path, isDirectory: true))
                    .OrderBy(item => item.Name, PathComparers.Os);

                var files = Directory.EnumerateFiles(CurrentPath, "*", _ListingOptions)
                    .Where(_PassesFileMasks)
                    .Select(path => _CreateListedItem(path, isDirectory: false))
                    .OrderBy(item => item.Name, PathComparers.Os);

                _listedItems.AddRange(folders);
                _listedItems.AddRange(files);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                _RebuildVisibleEntries(preserveSelection: false);
                return;
            }

            _RebuildVisibleEntries(preserveSelection: false);
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
            return new ListedItem(path, name, isDirectory, isDirectory ? null : _TryGetLength(path));
        }

        private bool _PassesFileMasks(string path)
        {
            var fileName = Path.GetFileName(path);
            if (!WildcardMask.IsMatch(fileName, Mask))
                return false;

            return !WildcardMask.MatchesAny(fileName, ExcludeMasks);
        }

        private IEnumerable<ListedItem> _ListDrives()
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

            return items.OrderBy(item => item.Name, PathComparers.Os);
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

        private static long? _TryGetLength(string path)
        {
            try
            {
                return new FileInfo(path).Length;
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

        private sealed record ListedItem(string Path, string Name, bool IsDirectory, long? Length);
    }
}
