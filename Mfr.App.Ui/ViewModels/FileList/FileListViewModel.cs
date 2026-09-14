using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.App.Ui.Services.FileList;
using Mfr.App.Ui.Services.Shell;
using Mfr.App.Ui.Threading;
using Mfr.Engine.Logging;
using Mfr.Models.Config;
using Mfr.Utils;
using Serilog;

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
        private const int _MaxRememberedPaths = 20;

        /// <summary>
        /// Default exclude patterns; applied only when exclude masks are enabled.
        /// </summary>
        public static IReadOnlyList<string> DefaultExcludeMasks { get; } = ["*.exe", "*.dll", "*.sys"];

        private readonly ISystemIconProvider _iconProvider;
        private readonly IFileShellOpener _shellOpener;
        private readonly IFileShellOperations _shellOperations;
        private readonly Func<IntPtr> _ownerHwnd;
        private readonly ITextClipboard _clipboard;
        private readonly IFileClipboard _fileClipboard;
        private readonly Func<
            string,
            string,
            bool,
            IReadOnlyList<string>,
            IEnumerable<string>,
            FileListCatalogResult
        > _listEntries;
        private readonly FileListThumbnailSession _thumbnails = new();
        private readonly List<FileListListedItem> _listedItems = [];
        private readonly List<FileListEntry> _selectedEntries = [];
        private bool _suppressSelectionSync;
        private bool _suppressListingReload;
        private bool _listingDeferred;
        private int _listingGeneration;
        private bool _isDisposed;
        private CancellationTokenSource? _listingBusyDelayCts;

        /// <summary>
        /// Delay before showing the Loading overlay; fast lists that finish sooner never flash.
        /// </summary>
        private static readonly TimeSpan _ListingBusyDelay = TimeSpan.FromMilliseconds(150);

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
        /// <param name="shellOperations">
        /// Shell delete/copy/move, or <see langword="null"/> to use the OS default.
        /// </param>
        /// <param name="fileClipboard">
        /// Explorer file clipboard for Cut/Copy/Paste, or <see langword="null"/> to use the OS default.
        /// </param>
        /// <param name="ownerHwnd">
        /// Owner HWND for shell UI modality, or <see langword="null"/> to use the desktop main window.
        /// </param>
        /// <param name="deferInitialListing">
        /// When <see langword="true"/>, sets the start path without listing until
        /// <see cref="ApplySession"/> (or the first explicit reload) runs — used when session masks
        /// will be applied immediately after construction.
        /// </param>
        public FileListViewModel(
            ISystemIconProvider? iconProvider,
            string? initialPath,
            IFileShellOpener? shellOpener = null,
            ITextClipboard? clipboard = null,
            IFileShellOperations? shellOperations = null,
            IFileClipboard? fileClipboard = null,
            Func<IntPtr>? ownerHwnd = null,
            bool deferInitialListing = false
        )
            : this(
                iconProvider,
                initialPath,
                shellOpener,
                clipboard,
                shellOperations,
                fileClipboard,
                ownerHwnd,
                listEntries: null,
                deferInitialListing
            ) { }

        /// <summary>
        /// Initializes the File List with an optional catalog list function (tests).
        /// </summary>
        /// <param name="iconProvider">Shell icons, or <see langword="null"/> to use the OS default.</param>
        /// <param name="initialPath">Directory to open, or <see langword="null"/> for the user profile.</param>
        /// <param name="shellOpener">
        /// Opens paths with the OS shell, or <see langword="null"/> to use the OS default.
        /// </param>
        /// <param name="clipboard">
        /// Clipboard for Copy path, or <see langword="null"/> to use the desktop main-window clipboard.
        /// </param>
        /// <param name="shellOperations">
        /// Shell delete/copy/move, or <see langword="null"/> to use the OS default.
        /// </param>
        /// <param name="fileClipboard">
        /// Explorer file clipboard for Cut/Copy/Paste, or <see langword="null"/> to use the OS default.
        /// </param>
        /// <param name="ownerHwnd">
        /// Owner HWND for shell UI modality, or <see langword="null"/> to use the desktop main window.
        /// </param>
        /// <param name="listEntries">
        /// Folder listing function, or <see langword="null"/> to use <see cref="FileListCatalog.List"/>.
        /// </param>
        /// <param name="deferInitialListing">
        /// When <see langword="true"/>, sets the start path without listing until
        /// <see cref="ApplySession"/> (or the first explicit reload) runs.
        /// </param>
        internal FileListViewModel(
            ISystemIconProvider? iconProvider,
            string? initialPath,
            IFileShellOpener? shellOpener,
            ITextClipboard? clipboard,
            IFileShellOperations? shellOperations,
            IFileClipboard? fileClipboard,
            Func<IntPtr>? ownerHwnd,
            Func<string, string, bool, IReadOnlyList<string>, IEnumerable<string>, FileListCatalogResult>? listEntries,
            bool deferInitialListing = false
        )
        {
            _iconProvider = iconProvider ?? SystemIconProvider.CreateDefault();
            _shellOpener = shellOpener ?? FileShellOpener.CreateDefault();
            _shellOperations = shellOperations ?? FileShellOperations.CreateDefault();
            _ownerHwnd = ownerHwnd ?? ShellOwnerHwnd.TryGetMainWindowHandle;
            _clipboard = clipboard ?? new DesktopTextClipboard();
            _fileClipboard = fileClipboard ?? FileClipboard.CreateDefault();
            _listEntries = listEntries ?? FileListCatalog.List;
            _listingDeferred = deferInitialListing;
            _fileClipboard.Changed += _OnFileClipboardChanged;
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
        [NotifyCanExecuteChangedFor(nameof(CutCommand))]
        [NotifyCanExecuteChangedFor(nameof(CopyCommand))]
        [NotifyCanExecuteChangedFor(nameof(PasteCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeletePermanentCommand))]
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
        /// Gets whether a folder listing is in progress.
        /// </summary>
        [ObservableProperty]
        private bool _isListing;

        /// <summary>
        /// Gets whether the in-pane Loading overlay should show.
        /// <para>
        /// Delayed after <see cref="IsListing"/> becomes true so fast local folders do not flash.
        /// </para>
        /// </summary>
        [ObservableProperty]
        private bool _showListingBusy;

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
        [NotifyCanExecuteChangedFor(nameof(ShowPropertiesCommand))]
        [NotifyCanExecuteChangedFor(nameof(CutCommand))]
        [NotifyCanExecuteChangedFor(nameof(CopyCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeletePermanentCommand))]
        private FileListEntry? _selectedEntry;

        /// <summary>
        /// Gets every selected File List row in the current folder.
        /// </summary>
        public IReadOnlyList<FileListEntry> SelectedEntries => _selectedEntries;

        /// <summary>
        /// Gets the most recent high-signal File List status-bar message (Copy path, Cut/Copy/Paste, Delete).
        /// </summary>
        [ObservableProperty]
        private StyledTextDisplay _lastStatusMessage = StyledTextDisplay.Empty;

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
        /// Jumps to the first or last row. Replaces the current selection with that row.
        /// </summary>
        /// <param name="toLast"><see langword="true"/> for the last row; otherwise the first.</param>
        /// <returns><see langword="true"/> when the list has at least one row.</returns>
        public bool TryJumpSelection(bool toLast)
        {
            if (Entries.Count == 0)
            {
                return false;
            }

            var target = Entries[toLast ? Entries.Count - 1 : 0];
            SetSelectedEntries([target], target);
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
        /// <para>
        /// Publishes a neutral sticky status on success, or an error status when the clipboard fails.
        /// </para>
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanCopyPath))]
        public async Task CopyPathAsync()
        {
            if (_selectedEntries.Count == 0)
            {
                return;
            }

            var pathCount = _selectedEntries.Count;
            var text = string.Join(Environment.NewLine, _selectedEntries.Select(entry => entry.FullPath));
            try
            {
                await _clipboard.SetTextAsync(text).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                LastStatusMessage = StatusBarText.Error(ex.Message);
                return;
            }

            LastStatusMessage = StatusBarText.Neutral($"Copied {pathCount} path(s).");
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
        /// Shows the OS property sheet for the focused File List entry.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanShowProperties))]
        public void ShowProperties()
        {
            if (SelectedEntry is null)
            {
                return;
            }

            _shellOpener.ShowProperties(SelectedEntry.FullPath);
        }

        /// <summary>
        /// Cuts the File List selection to the Explorer file clipboard (Preferred DropEffect Move).
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanOperateOnSelection))]
        public void Cut()
        {
            _WriteFileClipboard(cut: true);
        }

        /// <summary>
        /// Copies the File List selection to the Explorer file clipboard (Preferred DropEffect Copy).
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanOperateOnSelection))]
        public void Copy()
        {
            _WriteFileClipboard(cut: false);
        }

        /// <summary>
        /// Pastes clipboard files into <see cref="CurrentPath"/> (Copy or Move from Preferred DropEffect).
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanPaste))]
        public void Paste()
        {
            if (!_CanPaste())
            {
                return;
            }

            if (!_fileClipboard.TryGetPaste(out var paste) || paste.Paths.Count == 0)
            {
                return;
            }

            var itemCount = paste.Paths.Count;
            var hwnd = _ownerHwnd();
            var result = paste.PreferMove
                ? _shellOperations.Move(paste.Paths, CurrentPath, hwnd)
                : _shellOperations.Copy(paste.Paths, CurrentPath, hwnd);

            if (result == FileShellOperationResult.Succeeded && paste.PreferMove)
            {
                _fileClipboard.CompleteMovePaste();
            }

            Refresh();
            LastStatusMessage = _FormatShellVerbStatus(
                result,
                successMessage: $"Pasted {itemCount} item(s).",
                verb: "Paste"
            );
        }

        /// <summary>
        /// Deletes the File List selection to the Recycle Bin (shell UI confirms as needed).
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanOperateOnSelection))]
        public void Delete()
        {
            _DeleteSelection(recycle: true);
        }

        /// <summary>
        /// Permanently deletes the File List selection (shell UI confirms; no app dialog).
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanOperateOnSelection))]
        public void DeletePermanent()
        {
            _DeleteSelection(recycle: false);
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
        /// <para>
        /// Also invalidates any in-flight listing so a late catalog result is ignored.
        /// </para>
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            Interlocked.Increment(ref _listingGeneration);
            _CancelListingBusyDelay();
            ShowListingBusy = false;
            IsListing = false;
            _fileClipboard.Changed -= _OnFileClipboardChanged;
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

            // In-flight list is empty; the pending apply sorts with these settings.
            if (IsListing)
            {
                return;
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
        /// <para>
        /// When the folder must change, listing runs synchronously so the row can be selected before return.
        /// </para>
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
                // Locate needs the row immediately; load this folder synchronously and cancel any in-flight list.
                CurrentPath = resolvedDirectory;
                PathText = FileListPath.ToDisplayPath(resolvedDirectory);
                IsPathEditing = false;
                _RememberPath(PathText);
                _RebuildBreadcrumbs();
                _ReloadEntriesSynchronous();
                _UpdateNavigationFlags();
            }
            else if (IsListing)
            {
                // Same folder is still loading asynchronously — finish listing before selecting.
                _ReloadEntriesSynchronous();
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
            if (_suppressListingReload)
            {
                return;
            }

            _ReloadEntries(preserveSelection: true);
        }

        partial void OnExcludeMasksChanged(IReadOnlyList<string> value)
        {
            if (_suppressListingReload)
            {
                return;
            }

            _ReloadEntries(preserveSelection: true);
        }

        partial void OnExcludeMasksEnabledChanged(bool value)
        {
            if (_suppressListingReload)
            {
                return;
            }

            _ReloadEntries(preserveSelection: true);
        }

        /// <summary>
        /// Applies Exclude Masks dialog results (enable flag and one-mask-per-line text).
        /// </summary>
        /// <param name="enabled">Whether exclude masks are active.</param>
        /// <param name="editorText">Masks as typed in the dialog (one per line).</param>
        public void ApplyExcludeMasks(bool enabled, string? editorText)
        {
            _RunWithoutListingReload(() =>
            {
                ExcludeMasks = WildcardMask.NormalizeForStorage(editorText);
                ExcludeMasksEnabled = enabled;
            });

            _ReloadEntries(preserveSelection: true);
        }

        /// <summary>
        /// Restores mask, exclude-mask, suggestion, view-mode, and thumbnail-size fields from session.
        /// <para>
        /// When construction used <c>deferInitialListing</c>, this always performs the first catalog list
        /// (even when <paramref name="fileList"/> is null or only view settings change).
        /// </para>
        /// </summary>
        /// <param name="fileList">Persisted File List section, or <see langword="null"/> to keep defaults.</param>
        internal void ApplySession(FileListPrefs? fileList)
        {
            if (fileList is null)
            {
                _CompleteDeferredInitialListing();
                return;
            }

            var needsReload = false;
            _RunWithoutListingReload(() =>
            {
                if (!string.IsNullOrEmpty(fileList.FileMask))
                {
                    Mask = fileList.FileMask;
                    needsReload = true;
                }

                // Null means unset: keep the defaults. An empty list means the user cleared them.
                if (fileList.ExcludeMasks is not null)
                {
                    ExcludeMasks = [.. fileList.ExcludeMasks];
                    needsReload = true;
                }

                if (fileList.ExcludeMasksEnabled is { } excludeEnabled)
                {
                    ExcludeMasksEnabled = excludeEnabled;
                    needsReload = true;
                }

                if (fileList.MaskSuggestions is { Count: > 0 })
                {
                    MaskSuggestions.Clear();
                    foreach (var mask in fileList.MaskSuggestions)
                    {
                        MaskSuggestions.Add(mask);
                    }
                }

                if (fileList.ViewMode is { } viewMode)
                {
                    SetViewMode(viewMode);
                }

                if (fileList.ThumbnailSize is { } thumbnailSize)
                {
                    SetThumbnailSize(thumbnailSize);
                }
            });

            if (needsReload || _listingDeferred)
            {
                _listingDeferred = false;
                _ReloadEntries(preserveSelection: true);
            }
        }

        /// <summary>
        /// Runs <paramref name="action"/> without property-change listing reloads.
        /// </summary>
        /// <param name="action">Property updates that would otherwise each call <see cref="_ReloadEntries"/>.</param>
        private void _RunWithoutListingReload(Action action)
        {
            _suppressListingReload = true;
            try
            {
                action();
            }
            finally
            {
                _suppressListingReload = false;
            }
        }

        /// <summary>
        /// Starts the deferred constructor listing when session restore had nothing to apply.
        /// </summary>
        private void _CompleteDeferredInitialListing()
        {
            if (!_listingDeferred)
            {
                return;
            }

            _listingDeferred = false;
            _ReloadEntries();
        }

        /// <summary>
        /// Captures current mask, exclude-mask, suggestion, view-mode, and thumbnail-size fields for session save.
        /// <para>
        /// Omits Options-owned prefs (<see cref="FileListPrefs.RememberLastFolder"/>,
        /// <see cref="FileListPrefs.DoubleClickAddsToRenameList"/>); close-save merges those from
        /// <see cref="ConfigStore.FileList"/>.
        /// </para>
        /// </summary>
        /// <returns>File List session section matching the current view model.</returns>
        internal FileListPrefs CaptureSession()
        {
            return new FileListPrefs
            {
                LastOpenedDirectory = CurrentPath,
                FileMask = Mask,
                ExcludeMasks = [.. ExcludeMasks],
                ExcludeMasksEnabled = ExcludeMasksEnabled,
                MaskSuggestions = [.. MaskSuggestions],
                ViewMode = ViewMode,
                ThumbnailSize = ThumbnailSize,
            };
        }

        partial void OnViewModeChanged(FileListViewMode value)
        {
            // In-flight list is empty; the pending apply rebuilds with the new view mode.
            if (IsListing)
            {
                return;
            }

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

        private bool _CanShowProperties()
        {
            return SelectedEntry is not null;
        }

        /// <summary>
        /// Gets whether Cut / Copy / Delete can run on the current File List selection.
        /// </summary>
        private bool _CanOperateOnSelection()
        {
            return _selectedEntries.Count > 0 && _IsFilesystemFolderLocation();
        }

        /// <summary>
        /// Gets whether Paste can land clipboard files in <see cref="CurrentPath"/>.
        /// </summary>
        private bool _CanPaste()
        {
            return _IsFilesystemFolderLocation() && _fileClipboard.HasPasteableFiles;
        }

        /// <summary>
        /// Shared gate for File List mutate verbs that need a real filesystem folder.
        /// </summary>
        private bool _IsFilesystemFolderLocation()
        {
            return FileListPath.IsFilesystemFolderPath(CurrentPath);
        }

        /// <summary>
        /// Writes the current selection to the file clipboard and publishes status.
        /// <para>
        /// Cut ghosting updates via <see cref="IFileClipboard.Changed"/>.
        /// </para>
        /// </summary>
        /// <param name="cut">When <see langword="true"/>, Cut (Move); otherwise Copy.</param>
        private void _WriteFileClipboard(bool cut)
        {
            if (!_CanOperateOnSelection())
            {
                return;
            }

            var paths = _selectedEntries.Select(entry => entry.FullPath).ToList();
            var itemCount = paths.Count;
            try
            {
                if (cut)
                {
                    _fileClipboard.SetCut(paths);
                }
                else
                {
                    _fileClipboard.SetCopy(paths);
                }
            }
            catch (Exception ex)
            {
                LastStatusMessage = StatusBarText.Error(ex.Message);
                return;
            }

            LastStatusMessage = StatusBarText.Neutral(
                cut ? $"Cut {itemCount} item(s)." : $"Copied {itemCount} item(s)."
            );
        }

        /// <summary>
        /// Runs shell delete for the current selection, then refreshes and publishes sticky status.
        /// </summary>
        /// <param name="recycle">When <see langword="true"/>, Recycle Bin; otherwise permanent delete.</param>
        private void _DeleteSelection(bool recycle)
        {
            if (!_CanOperateOnSelection())
            {
                return;
            }

            var paths = _selectedEntries.Select(entry => entry.FullPath).ToList();
            var itemCount = paths.Count;
            var result = _shellOperations.Delete(paths, recycle, _ownerHwnd());
            Refresh();
            var successMessage = recycle
                ? $"Deleted {itemCount} item(s)."
                : $"Permanently deleted {itemCount} item(s).";
            LastStatusMessage = _FormatShellVerbStatus(
                result,
                successMessage,
                verb: recycle ? "Delete" : "Permanent delete"
            );
        }

        /// <summary>
        /// Maps a shell file-op outcome to a sticky File List status message.
        /// </summary>
        /// <param name="result">Shell operation result.</param>
        /// <param name="successMessage">Neutral text when <paramref name="result"/> succeeded.</param>
        /// <param name="verb">Short verb for cancelled / failed messages (e.g. Paste, Delete).</param>
        private static StyledTextDisplay _FormatShellVerbStatus(
            FileShellOperationResult result,
            string successMessage,
            string verb
        )
        {
            if (result == FileShellOperationResult.Succeeded)
            {
                return StatusBarText.Neutral(successMessage);
            }

            if (result == FileShellOperationResult.Cancelled)
            {
                return StatusBarText.Warning($"{verb} cancelled.");
            }

            return StatusBarText.Error($"{verb} failed.");
        }

        private void _OnFileClipboardChanged(object? sender, EventArgs e)
        {
            _ApplyCutMarks();
            PasteCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Syncs <see cref="FileListEntry.IsCutMarked"/> from the file clipboard cut set.
        /// </summary>
        private void _ApplyCutMarks()
        {
            var cutPaths = _fileClipboard.CutPaths;
            foreach (var entry in Entries)
            {
                entry.IsCutMarked = cutPaths.Contains(entry.FullPath);
            }
        }

        private void _NotifySelectionCommandsChanged()
        {
            OpenSelectedCommand.NotifyCanExecuteChanged();
            CopyPathCommand.NotifyCanExecuteChanged();
            ShowInExplorerCommand.NotifyCanExecuteChanged();
            ShowPropertiesCommand.NotifyCanExecuteChanged();
            CutCommand.NotifyCanExecuteChanged();
            CopyCommand.NotifyCanExecuteChanged();
            PasteCommand.NotifyCanExecuteChanged();
            DeleteCommand.NotifyCanExecuteChanged();
            DeletePermanentCommand.NotifyCanExecuteChanged();
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
            if (_listingDeferred)
            {
                _UpdateNavigationFlags();
                return;
            }

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
            _listingDeferred = false;
            var generation = _BeginListingReload(preserveSelection);
            _BeginListingBusy(generation);

            var path = CurrentPath;
            var mask = Mask;
            var excludeEnabled = ExcludeMasksEnabled;
            var excludeMasks = ExcludeMasks;
            var pathHistory = PathHistory.ToList();

            _ = Task.Factory.StartNew(
                () =>
                {
                    var result = _ListEntriesSafe(path, mask, excludeEnabled, excludeMasks, pathHistory);
                    AvaloniaUiThread.Post(() => _ApplyListingResult(generation, result, preserveSelection));
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default
            );
        }

        /// <summary>
        /// Clears listing UI state and bumps generation so in-flight applies are ignored.
        /// <para>
        /// Does not cancel OS/SMB I/O for a superseded generation; that work may still finish in the
        /// background and is discarded when <see cref="_ApplyListingResult"/> sees a stale generation.
        /// </para>
        /// </summary>
        /// <param name="preserveSelection">Whether to keep selection paths for restore after rebuild.</param>
        /// <returns>Generation token for this reload.</returns>
        private int _BeginListingReload(bool preserveSelection)
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
            return Interlocked.Increment(ref _listingGeneration);
        }

        /// <summary>
        /// Invokes the catalog list function, mapping unexpected exceptions to an unavailable failure.
        /// </summary>
        /// <param name="path">Folder to list.</param>
        /// <param name="mask">Include mask.</param>
        /// <param name="excludeEnabled">Whether exclude masks apply.</param>
        /// <param name="excludeMasks">Exclude mask patterns.</param>
        /// <param name="pathHistory">Recent paths for Network listing.</param>
        /// <returns>Catalog rows or a failure result.</returns>
        private FileListCatalogResult _ListEntriesSafe(
            string path,
            string mask,
            bool excludeEnabled,
            IReadOnlyList<string> excludeMasks,
            IEnumerable<string> pathHistory
        )
        {
            try
            {
                return _listEntries(path, mask, excludeEnabled, excludeMasks, pathHistory);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to list folder {Path}.", path);
                return FileListCatalogResult.Failed(FileListListingFailure.Unavailable);
            }
        }

        /// <summary>
        /// Applies a catalog result when it still matches the latest listing generation.
        /// </summary>
        /// <param name="generation">Generation captured when the load started.</param>
        /// <param name="result">Catalog rows or failure.</param>
        /// <param name="preserveSelection">Whether to restore selection paths after rebuild.</param>
        private void _ApplyListingResult(int generation, FileListCatalogResult result, bool preserveSelection)
        {
            if (_isDisposed || generation != Volatile.Read(ref _listingGeneration))
            {
                return;
            }

            if (result.Failure != FileListListingFailure.None)
            {
                ListingError = FileListCatalog.FormatListingError(result.Failure);
                _RebuildVisibleEntries(preserveSelection);
                _EndListingBusy();
                return;
            }

            _listedItems.AddRange(result.Items);
            FileListListingSort.Apply(_listedItems, SortMemberPath, IsSortAscending);
            _RebuildVisibleEntries(preserveSelection);
            _EndListingBusy();
        }

        /// <summary>
        /// Lists the current folder on the calling thread and cancels any in-flight async listing.
        /// <para>
        /// Used by <see cref="TryLocatePath"/> so selection can run immediately after a folder change.
        /// </para>
        /// </summary>
        /// <param name="preserveSelection">Whether to restore selection paths after rebuild.</param>
        private void _ReloadEntriesSynchronous(bool preserveSelection = false)
        {
            _listingDeferred = false;
            var generation = _BeginListingReload(preserveSelection);
            _EndListingBusy();

            var result = _ListEntriesSafe(CurrentPath, Mask, ExcludeMasksEnabled, ExcludeMasks, PathHistory);
            _ApplyListingResult(generation, result, preserveSelection);
        }

        /// <summary>
        /// Marks listing busy and schedules the Loading overlay after <see cref="_ListingBusyDelay"/>.
        /// </summary>
        /// <param name="generation">Generation for this reload.</param>
        private void _BeginListingBusy(int generation)
        {
            IsListing = true;
            ShowListingBusy = false;
            _CancelListingBusyDelay();
            var cts = new CancellationTokenSource();
            _listingBusyDelayCts = cts;
            var token = cts.Token;
            _ = Task.Run(
                async () =>
                {
                    try
                    {
                        await Task.Delay(_ListingBusyDelay, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }

                    AvaloniaUiThread.Post(() =>
                    {
                        if (
                            _isDisposed
                            || token.IsCancellationRequested
                            || generation != Volatile.Read(ref _listingGeneration)
                            || !IsListing
                        )
                        {
                            return;
                        }

                        ShowListingBusy = true;
                    });
                },
                CancellationToken.None
            );
        }

        /// <summary>
        /// Clears listing busy state and hides the Loading overlay.
        /// </summary>
        private void _EndListingBusy()
        {
            _CancelListingBusyDelay();
            ShowListingBusy = false;
            IsListing = false;
        }

        private void _CancelListingBusyDelay()
        {
            var cts = _listingBusyDelayCts;
            _listingBusyDelayCts = null;
            if (cts is null)
            {
                return;
            }

            cts.Cancel();
            cts.Dispose();
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

            _ApplyCutMarks();
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
            while (PathHistory.Count > _MaxRememberedPaths)
            {
                PathHistory.RemoveAt(PathHistory.Count - 1);
            }
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
