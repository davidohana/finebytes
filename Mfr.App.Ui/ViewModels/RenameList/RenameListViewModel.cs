using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.Collections;
using Mfr.App.Ui.ViewModels.FileList;
using Mfr.Models.Config;
using Mfr.Models.RenameList;
using EngineRenameList = Mfr.Engine.RenameList.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Rename List pane: hosts the preview grid for items queued to rename.
    /// </summary>
    public sealed partial class RenameListViewModel : ViewModelBase
    {
        private readonly FileListViewModel _fileListViewModel;
        private readonly EngineRenameList _renameList = new(includeHidden: false);
        private readonly List<RenameListEntry> _selectedEntries = [];
        private List<RenameListSortKey> _sortKeys = [];

        /// <summary>
        /// Initializes the Rename List and listens for File List changes that affect add commands.
        /// </summary>
        /// <param name="fileListViewModel">File List pane used as the add source.</param>
        public RenameListViewModel(FileListViewModel fileListViewModel)
        {
            ArgumentNullException.ThrowIfNull(fileListViewModel);
            _fileListViewModel = fileListViewModel;
            _fileListViewModel.PropertyChanged += _OnFileListPropertyChanged;
            _fileListViewModel.Entries.CollectionChanged += _OnFileListEntriesChanged;
            AddProgress.PropertyChanged += _OnAddProgressPropertyChanged;
            _ApplySessionScalarDefaults();
        }

        private void _ApplySessionScalarDefaults()
        {
            var section = new SessionStateRenameList();
            AddMode = section.AddMode;
            AddFolderContents = section.AddFolderContents;
            UseFixedWidthFont = section.UseFixedWidthFont;
        }

        /// <summary>
        /// Restores sort, columns, add-policy, and display prefs from a session section.
        /// </summary>
        /// <param name="renameList">
        /// Saved Rename List section, or <see langword="null"/> for first-launch defaults.
        /// </param>
        internal void ApplySessionSection(SessionStateRenameList? renameList)
        {
            var section = renameList ?? new SessionStateRenameList();
            ApplySession(renameList?.SortFields);
            ApplyVisibleColumnsFromSession(renameList?.VisibleColumns);
            AddMode = section.AddMode;
            AddFolderContents = section.AddFolderContents;
            UseFixedWidthFont = section.UseFixedWidthFont;
        }

        /// <summary>
        /// Captures sort, columns, add-policy, and display prefs for session save.
        /// </summary>
        /// <returns>Rename List session section matching the current view model.</returns>
        internal SessionStateRenameList CaptureSession()
        {
            return new SessionStateRenameList
            {
                SortFields = [.. CaptureSortFields()],
                VisibleColumns = [.. CaptureVisibleColumnsForSession()],
                AddMode = AddMode,
                AddFolderContents = AddFolderContents,
                UseFixedWidthFont = UseFixedWidthFont,
            };
        }

        /// <summary>
        /// Gets progress, cancel, and delayed-dialog state for the current add.
        /// </summary>
        public RenameListAddProgressViewModel AddProgress { get; } = new();

        /// <summary>
        /// Gets the rows shown in the Rename List grid.
        /// </summary>
        /// <para>
        /// A <see cref="RangeObservableCollection{T}"/> so large adds can append thousands of new rows with
        /// one grid refresh instead of one per row.
        /// </para>
        public RangeObservableCollection<RenameListEntry> Entries { get; } = [];

        /// <summary>
        /// Gets the currently selected Rename List rows.
        /// </summary>
        public IReadOnlyList<RenameListEntry> SelectedEntries => _selectedEntries;

        /// <summary>
        /// Gets the insert index under a file or internal drag (insert-before target), or null when unset.
        /// </summary>
        /// <para>
        /// When equal to <see cref="Entries"/>.Count, the mark means append after the last row.
        /// </para>
        public int? DropMarkIndex { get; private set; }

        /// <summary>
        /// Gets the count of items in the Rename List.
        /// </summary>
        public int ItemCount => Entries.Count;

        /// <summary>
        /// Gets whether an add operation is in progress.
        /// </summary>
        public bool IsAdding => AddProgress.IsAdding;

        /// <summary>
        /// Gets whether Auto-Sort is active (one or more sort keys).
        /// </summary>
        public bool IsAutoSort => _sortKeys.Count > 0;

        /// <summary>
        /// Gets the active Auto-Sort keys (empty when Auto-Sort is off).
        /// </summary>
        public IReadOnlyList<RenameListSortKey> SortKeys => _sortKeys;

        /// <summary>
        /// Auto-Sort tooltip: active keys when on, or prompt to enable with default keys when off.
        /// </summary>
        public string SortSummaryText => RenameListSortDisplay.FormatSummary(_sortKeys);

        /// <summary>
        /// Sort priority and direction glyphs keyed by column for Rename List headers.
        /// </summary>
        public RenameListColumnSortStates ColumnSortStates { get; private set; } = RenameListColumnSortStates.Inactive;

        /// <summary>
        /// Gets the most recent user-facing add failure message, or empty when none.
        /// </summary>
        [ObservableProperty]
        private string _lastAddError = string.Empty;

        /// <summary>
        /// Gets the most recent locate-in-File-List failure message, or empty when none.
        /// </summary>
        [ObservableProperty]
        private string _lastLocateError = string.Empty;

        /// <summary>
        /// Status-bar hint for the focused or selected Rename List cell.
        /// </summary>
        [ObservableProperty]
        private StatusHintDisplay _cellStatusHintDisplay = StatusHintDisplay.Empty;

        /// <summary>
        /// Replaces the Rename List selection.
        /// </summary>
        /// <param name="entries">Selected grid rows.</param>
        public void SetSelectedEntries(IReadOnlyList<RenameListEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);

            var entrySet = Entries.ToHashSet();
            _selectedEntries.Clear();
            foreach (var entry in entries)
            {
                if (entrySet.Contains(entry))
                {
                    _selectedEntries.Add(entry);
                }
            }

            OnPropertyChanged(nameof(SelectedEntries));
            RemoveSelectedCommand.NotifyCanExecuteChanged();
            RemoveAllButSelectedCommand.NotifyCanExecuteChanged();
            MoveSelectedUpCommand.NotifyCanExecuteChanged();
            MoveSelectedDownCommand.NotifyCanExecuteChanged();
            LocateInFileListCommand.NotifyCanExecuteChanged();
            _NotifyShowLoadErrorsChanged();
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

            SetSelectedEntries([Entries[toLast ? Entries.Count - 1 : 0]]);
            return true;
        }

        /// <summary>
        /// Sets or clears the drag insert marker (index to insert before, or append at end).
        /// </summary>
        /// <param name="index">
        /// Zero-based insert index under the pointer (<see cref="Entries"/>.Count = append), or null to clear.
        /// </param>
        /// <remarks>
        /// <para>
        /// Ignored while Auto-Sort is on (MFR7: external drops append and resort; no mark).
        /// </para>
        /// </remarks>
        public void SetDropMarkIndex(int? index)
        {
            if (IsAutoSort)
            {
                index = null;
            }

            if (index is { } i && (i < 0 || i > Entries.Count))
            {
                index = null;
            }

            if (DropMarkIndex == index)
            {
                return;
            }

            DropMarkIndex = index;
            OnPropertyChanged(nameof(DropMarkIndex));
        }

        /// <summary>
        /// Rebuilds <see cref="Entries"/> to match engine order.
        /// </summary>
        /// <remarks>
        /// <para>
        /// DataGrid ignores Move; ReplaceAll raises Reset so the grid refreshes.
        /// </para>
        /// </remarks>
        private void _SyncEntriesToEngineOrder()
        {
            var engineItemToEntry = Entries.ToDictionary(entry => entry.EngineItem);
            Entries.ReplaceAll(_renameList.RenameItems.Select(item => engineItemToEntry[item]));
            SetSelectedEntries([.. _selectedEntries]);
        }

        private void _NotifyListChanged()
        {
            OnPropertyChanged(nameof(ItemCount));
            ClearCommand.NotifyCanExecuteChanged();
            RemoveAllButSelectedCommand.NotifyCanExecuteChanged();
            _NotifyRefreshChanged();
        }

        /// <summary>
        /// Rebinds grid field cells after preview, metadata hydrate, or disk refresh.
        /// </summary>
        private void _RefreshFieldDisplay()
        {
            if (Entries.Count == 0)
            {
                return;
            }

            foreach (var entry in Entries)
            {
                entry.NotifyFieldsChanged();
                entry.NotifyRowErrorChanged();
            }
            _NotifyShowLoadErrorsChanged();
        }

        private bool _CanRemoveSelected()
        {
            return !IsAdding && _selectedEntries.Count > 0;
        }

        private bool _CanRemoveAllButSelected()
        {
            return !IsAdding && _selectedEntries.Count > 0 && _selectedEntries.Count < Entries.Count;
        }

        private bool _CanClear()
        {
            return !IsAdding && Entries.Count > 0;
        }

        private bool _CanLocateInFileList()
        {
            return !IsAdding && _GetFocusedSelectedEntry() is not null;
        }

        private RenameListEntry? _GetFocusedSelectedEntry()
        {
            return _selectedEntries.Count > 0 ? _selectedEntries[^1] : null;
        }

        private void _OnAddProgressPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is not nameof(RenameListAddProgressViewModel.IsAdding))
            {
                return;
            }

            OnPropertyChanged(nameof(IsAdding));
            AddSelectedCommand.NotifyCanExecuteChanged();
            AddAllCommand.NotifyCanExecuteChanged();
            RemoveSelectedCommand.NotifyCanExecuteChanged();
            RemoveAllButSelectedCommand.NotifyCanExecuteChanged();
            ClearCommand.NotifyCanExecuteChanged();
            MoveSelectedUpCommand.NotifyCanExecuteChanged();
            MoveSelectedDownCommand.NotifyCanExecuteChanged();
            LocateInFileListCommand.NotifyCanExecuteChanged();
            _NotifyShowLoadErrorsChanged();
            _NotifyRefreshChanged();
        }

        private void _OnFileListPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!_AffectsAddCommands(e.PropertyName))
            {
                return;
            }

            AddSelectedCommand.NotifyCanExecuteChanged();
            AddAllCommand.NotifyCanExecuteChanged();
        }

        private void _OnFileListEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            AddAllCommand.NotifyCanExecuteChanged();
        }

        private static bool _AffectsAddCommands(string? propertyName)
        {
            return propertyName
                is nameof(FileListViewModel.SelectedEntry)
                    or nameof(FileListViewModel.SelectedEntries)
                    or nameof(FileListViewModel.CurrentPath)
                    or nameof(FileListViewModel.Mask)
                    or nameof(FileListViewModel.ExcludeMasksEnabled)
                    or nameof(FileListViewModel.ExcludeMasks);
        }
    }
}
