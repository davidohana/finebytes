using CommunityToolkit.Mvvm.Input;
using Mfr.Models.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Draft state for the unified Rename List field shuttle dialog (Columns and Sort tabs).
    /// </summary>
    public sealed partial class RenameListFieldShuttleDialogViewModel : ViewModelBase
    {
        private readonly List<RenameListVisibleColumn> _draftColumns;
        private readonly List<RenameListSortKey> _draftSortKeys;
        private readonly HashSet<RenameListFieldKey> _selectedColumnKeys;
        private readonly HashSet<RenameListSortColumn> _selectedSortColumns;
        private int _selectedColumnRowIndex = -1;
        private int _selectedSortRowIndex = -1;

        /// <summary>
        /// Initializes the shuttle from the Rename List's current column layout and sort keys.
        /// </summary>
        /// <param name="visibleColumns">Current visible columns in grid order.</param>
        /// <param name="sortKeys">Current Auto-Sort keys in priority order.</param>
        /// <param name="initialTab">Tab to show when the dialog opens.</param>
        public RenameListFieldShuttleDialogViewModel(
            IReadOnlyList<RenameListVisibleColumn> visibleColumns,
            IReadOnlyList<RenameListSortKey> sortKeys,
            RenameListFieldShuttleTab initialTab = RenameListFieldShuttleTab.Columns
        )
        {
            ArgumentNullException.ThrowIfNull(visibleColumns);
            ArgumentNullException.ThrowIfNull(sortKeys);

            _draftColumns = [.. visibleColumns];
            _draftSortKeys = [.. sortKeys];
            _selectedColumnKeys = [.. _draftColumns.Select(column => column.Key)];
            _selectedSortColumns = [.. _draftSortKeys.Select(key => key.Column)];

            Groups = _BuildGroups();
            SelectedGroup = Groups.Count > 0 ? Groups[0] : null;
            SelectedTabIndex = (int)initialTab;
            _RefreshLists();
        }

        /// <summary>
        /// Gets property groups available in the shuttle dropdown.
        /// </summary>
        public IReadOnlyList<RenameListFieldGroupOption> Groups { get; }

        /// <summary>
        /// Gets or sets the selected property group.
        /// </summary>
        public RenameListFieldGroupOption? SelectedGroup
        {
            get;
            set
            {
                if (field == value)
                {
                    return;
                }

                field = value;
                OnPropertyChanged();
                _ClearAvailableSelections();
                _RefreshLists();
            }
        }

        /// <summary>
        /// Gets or sets the top-level tab index (0 = Columns, 1 = Sort).
        /// </summary>
        public int SelectedTabIndex
        {
            get;
            set
            {
                if (field == value)
                {
                    return;
                }

                field = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets whether the Columns tab is showing preview (vs original) available fields.
        /// </summary>
        public bool IsPreviewColumnsTab
        {
            get;
            set
            {
                if (field == value)
                {
                    return;
                }

                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsOriginalColumnsTab));
                if (value)
                {
                    SelectedAvailableOriginalField = null;
                }
                else
                {
                    SelectedAvailablePreviewField = null;
                }

                AddSelectedOriginalFieldCommand.NotifyCanExecuteChanged();
                AddSelectedPreviewFieldCommand.NotifyCanExecuteChanged();
                AddAllOriginalFieldsCommand.NotifyCanExecuteChanged();
                AddAllPreviewFieldsCommand.NotifyCanExecuteChanged();
            }
        }

        /// <summary>
        /// Gets whether the Columns tab is showing original available fields.
        /// </summary>
        public bool IsOriginalColumnsTab => !IsPreviewColumnsTab;

        /// <summary>
        /// Selects the original-fields list on the Columns tab.
        /// </summary>
        [RelayCommand]
        public void SelectOriginalColumnsTab()
        {
            IsPreviewColumnsTab = false;
        }

        /// <summary>
        /// Selects the preview-fields list on the Columns tab.
        /// </summary>
        [RelayCommand]
        public void SelectPreviewColumnsTab()
        {
            IsPreviewColumnsTab = true;
        }

        /// <summary>
        /// Gets original fields available to add on the Columns tab for the selected group.
        /// </summary>
        public IReadOnlyList<RenameListField> AvailableOriginalFields { get; private set; } = [];

        /// <summary>
        /// Gets preview fields available to add on the Columns tab for the selected group.
        /// </summary>
        public IReadOnlyList<RenameListField> AvailablePreviewFields { get; private set; } = [];

        /// <summary>
        /// Gets sortable fields available to add on the Sort tab for the selected group.
        /// </summary>
        public IReadOnlyList<RenameListField> AvailableSortFields { get; private set; } = [];

        /// <summary>
        /// Gets selected visible columns in grid order.
        /// </summary>
        public IReadOnlyList<RenameListFieldShuttleColumnRow> SelectedColumnRows { get; private set; } = [];

        /// <summary>
        /// Gets selected sort keys in priority order.
        /// </summary>
        public IReadOnlyList<RenameListFieldShuttleSortRow> SelectedSortRows { get; private set; } = [];

        /// <summary>
        /// Gets or sets the selected available original field on the Columns tab.
        /// </summary>
        public RenameListField? SelectedAvailableOriginalField
        {
            get;
            set
            {
                if (ReferenceEquals(field, value))
                {
                    return;
                }

                field = value;
                OnPropertyChanged();
                AddSelectedOriginalFieldCommand.NotifyCanExecuteChanged();
            }
        }

        /// <summary>
        /// Gets or sets the selected available preview field on the Columns tab.
        /// </summary>
        public RenameListField? SelectedAvailablePreviewField
        {
            get;
            set
            {
                if (ReferenceEquals(field, value))
                {
                    return;
                }

                field = value;
                OnPropertyChanged();
                AddSelectedPreviewFieldCommand.NotifyCanExecuteChanged();
            }
        }

        /// <summary>
        /// Gets or sets the selected available sort field on the Sort tab.
        /// </summary>
        public RenameListField? SelectedAvailableSortField
        {
            get;
            set
            {
                if (ReferenceEquals(field, value))
                {
                    return;
                }

                field = value;
                OnPropertyChanged();
                AddSelectedSortFieldCommand.NotifyCanExecuteChanged();
            }
        }

        /// <summary>
        /// Gets or sets the selected row index in the selected-columns list.
        /// </summary>
        public int SelectedColumnRowIndex
        {
            get => _selectedColumnRowIndex;
            set
            {
                if (_selectedColumnRowIndex == value)
                {
                    return;
                }

                _selectedColumnRowIndex = value;
                OnPropertyChanged();
                _NotifyColumnSelectionCommands();
            }
        }

        /// <summary>
        /// Gets or sets the selected row index in the selected-sort list.
        /// </summary>
        public int SelectedSortRowIndex
        {
            get => _selectedSortRowIndex;
            set
            {
                if (_selectedSortRowIndex == value)
                {
                    return;
                }

                _selectedSortRowIndex = value;
                OnPropertyChanged();
                _NotifySortSelectionCommands();
            }
        }

        /// <summary>
        /// Gets whether OK can apply the draft (at least one visible column required).
        /// </summary>
        public bool CanConfirm => _draftColumns.Count > 0;

        /// <summary>
        /// Gets the draft visible columns to apply when OK is pressed.
        /// </summary>
        public IReadOnlyList<RenameListVisibleColumn> ResultColumns => _draftColumns;

        /// <summary>
        /// Gets the draft sort keys to apply when OK is pressed.
        /// </summary>
        public IReadOnlyList<RenameListSortKey> ResultSortKeys => _draftSortKeys;

        /// <summary>
        /// Adds the selected available original field to the visible-column list.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanAddSelectedOriginalField))]
        public void AddSelectedOriginalField()
        {
            if (SelectedAvailableOriginalField is null)
            {
                return;
            }

            _AddColumn(SelectedAvailableOriginalField.OriginalKey);
        }

        /// <summary>
        /// Adds the selected available preview field to the visible-column list.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanAddSelectedPreviewField))]
        public void AddSelectedPreviewField()
        {
            if (SelectedAvailablePreviewField is null)
            {
                return;
            }

            _AddColumn(SelectedAvailablePreviewField.PreviewKey);
        }

        /// <summary>
        /// Adds all available original fields in the current group/tab to the visible-column list.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_HasAvailableOriginalFields))]
        public void AddAllOriginalFields()
        {
            foreach (var catalogField in AvailableOriginalFields.ToList())
            {
                _AddColumn(catalogField.OriginalKey, refresh: false);
            }

            _RefreshLists();
        }

        /// <summary>
        /// Adds all available preview fields in the current group/tab to the visible-column list.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_HasAvailablePreviewFields))]
        public void AddAllPreviewFields()
        {
            foreach (var catalogField in AvailablePreviewFields.ToList())
            {
                _AddColumn(catalogField.PreviewKey, refresh: false);
            }

            _RefreshLists();
        }

        /// <summary>
        /// Removes the selected visible column.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanRemoveSelectedColumn))]
        public void RemoveSelectedColumn()
        {
            if (!_TryGetSelectedColumnIndex(out var index))
            {
                return;
            }

            var key = _draftColumns[index].Key;
            _draftColumns.RemoveAt(index);
            _selectedColumnKeys.Remove(key);
            _selectedColumnRowIndex = _ClampSelectionIndex(_selectedColumnRowIndex, _draftColumns.Count);
            _RefreshLists();
        }

        /// <summary>
        /// Moves the selected visible column up.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanMoveSelectedColumnUp))]
        public void MoveSelectedColumnUp()
        {
            if (!_TryGetSelectedColumnIndex(out var index) || index <= 0)
            {
                return;
            }

            _SwapColumns(index, index - 1);
            SelectedColumnRowIndex = index - 1;
        }

        /// <summary>
        /// Moves the selected visible column down.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanMoveSelectedColumnDown))]
        public void MoveSelectedColumnDown()
        {
            if (!_TryGetSelectedColumnIndex(out var index) || index >= _draftColumns.Count - 1)
            {
                return;
            }

            _SwapColumns(index, index + 1);
            SelectedColumnRowIndex = index + 1;
        }

        /// <summary>
        /// Clears all selected visible columns.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_HasSelectedColumns))]
        public void ClearSelectedColumns()
        {
            _draftColumns.Clear();
            _selectedColumnKeys.Clear();
            SelectedColumnRowIndex = -1;
            _RefreshLists();
        }

        /// <summary>
        /// Adds the selected available sort field to the sort-key list.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanAddSelectedSortField))]
        public void AddSelectedSortField()
        {
            if (SelectedAvailableSortField?.SortColumn is not { } sortColumn)
            {
                return;
            }

            _AddSortKey(sortColumn);
        }

        /// <summary>
        /// Removes the selected sort key.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanRemoveSelectedSortKey))]
        public void RemoveSelectedSortKey()
        {
            if (!_TryGetSelectedSortIndex(out var index))
            {
                return;
            }

            var column = _draftSortKeys[index].Column;
            _draftSortKeys.RemoveAt(index);
            _selectedSortColumns.Remove(column);
            _selectedSortRowIndex = _ClampSelectionIndex(_selectedSortRowIndex, _draftSortKeys.Count);
            _RefreshLists();
        }

        /// <summary>
        /// Moves the selected sort key up in priority.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanMoveSelectedSortKeyUp))]
        public void MoveSelectedSortKeyUp()
        {
            if (!_TryGetSelectedSortIndex(out var index) || index <= 0)
            {
                return;
            }

            _SwapSortKeys(index, index - 1);
            SelectedSortRowIndex = index - 1;
        }

        /// <summary>
        /// Moves the selected sort key down in priority.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanMoveSelectedSortKeyDown))]
        public void MoveSelectedSortKeyDown()
        {
            if (!_TryGetSelectedSortIndex(out var index) || index >= _draftSortKeys.Count - 1)
            {
                return;
            }

            _SwapSortKeys(index, index + 1);
            SelectedSortRowIndex = index + 1;
        }

        /// <summary>
        /// Toggles ascending/descending for the selected sort key.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanToggleSelectedSortDirection))]
        public void ToggleSelectedSortDirection()
        {
            if (!_TryGetSelectedSortIndex(out var index))
            {
                return;
            }

            var existing = _draftSortKeys[index];
            _draftSortKeys[index] = existing with { Descending = !existing.Descending };
            _RefreshLists();
        }

        /// <summary>
        /// Clears all selected sort keys (Auto-Sort off).
        /// </summary>
        [RelayCommand(CanExecute = nameof(_HasSelectedSortKeys))]
        public void ClearSelectedSortKeys()
        {
            _draftSortKeys.Clear();
            _selectedSortColumns.Clear();
            SelectedSortRowIndex = -1;
            _RefreshLists();
        }

        private bool _CanAddSelectedOriginalField()
        {
            return SelectedAvailableOriginalField is not null;
        }

        private bool _CanAddSelectedPreviewField()
        {
            return SelectedAvailablePreviewField is not null;
        }

        private bool _HasAvailableOriginalFields()
        {
            return AvailableOriginalFields.Count > 0;
        }

        private bool _HasAvailablePreviewFields()
        {
            return AvailablePreviewFields.Count > 0;
        }

        private bool _CanRemoveSelectedColumn()
        {
            return _TryGetSelectedColumnIndex(out _);
        }

        private bool _CanMoveSelectedColumnUp()
        {
            return _TryGetSelectedColumnIndex(out var index) && index > 0;
        }

        private bool _CanMoveSelectedColumnDown()
        {
            return _TryGetSelectedColumnIndex(out var index) && index < _draftColumns.Count - 1;
        }

        private bool _HasSelectedColumns()
        {
            return _draftColumns.Count > 0;
        }

        private bool _CanAddSelectedSortField()
        {
            return SelectedAvailableSortField is { SortColumn: not null };
        }

        private bool _CanRemoveSelectedSortKey()
        {
            return _TryGetSelectedSortIndex(out _);
        }

        private bool _CanMoveSelectedSortKeyUp()
        {
            return _TryGetSelectedSortIndex(out var index) && index > 0;
        }

        private bool _CanMoveSelectedSortKeyDown()
        {
            return _TryGetSelectedSortIndex(out var index) && index < _draftSortKeys.Count - 1;
        }

        private bool _CanToggleSelectedSortDirection()
        {
            return _TryGetSelectedSortIndex(out _);
        }

        private bool _HasSelectedSortKeys()
        {
            return _draftSortKeys.Count > 0;
        }

        private void _AddColumn(RenameListFieldKey key, bool refresh = true)
        {
            if (!_selectedColumnKeys.Add(key))
            {
                return;
            }

            _draftColumns.Add(new RenameListVisibleColumn(key));
            SelectedColumnRowIndex = _draftColumns.Count - 1;

            if (refresh)
            {
                _RefreshLists();
            }
        }

        private void _AddSortKey(RenameListSortColumn sortColumn)
        {
            if (!_selectedSortColumns.Add(sortColumn))
            {
                return;
            }

            _draftSortKeys.Add(new RenameListSortKey(sortColumn));
            SelectedSortRowIndex = _draftSortKeys.Count - 1;
            _RefreshLists();
        }

        private void _SwapColumns(int firstIndex, int secondIndex)
        {
            (_draftColumns[firstIndex], _draftColumns[secondIndex]) = (
                _draftColumns[secondIndex],
                _draftColumns[firstIndex]
            );
            _RefreshLists();
        }

        private void _SwapSortKeys(int firstIndex, int secondIndex)
        {
            (_draftSortKeys[firstIndex], _draftSortKeys[secondIndex]) = (
                _draftSortKeys[secondIndex],
                _draftSortKeys[firstIndex]
            );
            _RefreshLists();
        }

        private bool _TryGetSelectedColumnIndex(out int index)
        {
            index = _selectedColumnRowIndex;
            return index >= 0 && index < _draftColumns.Count;
        }

        private bool _TryGetSelectedSortIndex(out int index)
        {
            index = _selectedSortRowIndex;
            return index >= 0 && index < _draftSortKeys.Count;
        }

        private static int _ClampSelectionIndex(int index, int count)
        {
            if (count == 0)
            {
                return -1;
            }

            if (index < 0)
            {
                return -1;
            }

            if (index >= count)
            {
                return count - 1;
            }

            return index;
        }

        private void _ClearAvailableSelections()
        {
            SelectedAvailableOriginalField = null;
            SelectedAvailablePreviewField = null;
            SelectedAvailableSortField = null;
        }

        private void _RefreshLists()
        {
            var groupId = SelectedGroup?.GroupId;
            AvailableOriginalFields = _BuildAvailableOriginalFields(groupId);
            AvailablePreviewFields = _BuildAvailablePreviewFields(groupId);
            AvailableSortFields = _BuildAvailableSortFields(groupId);
            SelectedColumnRows =
            [
                .. _draftColumns.Select((column, index) => new RenameListFieldShuttleColumnRow(index, column)),
            ];
            SelectedSortRows =
            [
                .. _draftSortKeys.Select((key, index) => new RenameListFieldShuttleSortRow(index, key)),
            ];

            OnPropertyChanged(nameof(AvailableOriginalFields));
            OnPropertyChanged(nameof(AvailablePreviewFields));
            OnPropertyChanged(nameof(AvailableSortFields));
            OnPropertyChanged(nameof(SelectedColumnRows));
            OnPropertyChanged(nameof(SelectedSortRows));
            OnPropertyChanged(nameof(CanConfirm));

            AddSelectedOriginalFieldCommand.NotifyCanExecuteChanged();
            AddSelectedPreviewFieldCommand.NotifyCanExecuteChanged();
            AddAllOriginalFieldsCommand.NotifyCanExecuteChanged();
            AddAllPreviewFieldsCommand.NotifyCanExecuteChanged();
            RemoveSelectedColumnCommand.NotifyCanExecuteChanged();
            MoveSelectedColumnUpCommand.NotifyCanExecuteChanged();
            MoveSelectedColumnDownCommand.NotifyCanExecuteChanged();
            ClearSelectedColumnsCommand.NotifyCanExecuteChanged();
            AddSelectedSortFieldCommand.NotifyCanExecuteChanged();
            RemoveSelectedSortKeyCommand.NotifyCanExecuteChanged();
            MoveSelectedSortKeyUpCommand.NotifyCanExecuteChanged();
            MoveSelectedSortKeyDownCommand.NotifyCanExecuteChanged();
            ToggleSelectedSortDirectionCommand.NotifyCanExecuteChanged();
            ClearSelectedSortKeysCommand.NotifyCanExecuteChanged();
            _NotifyColumnSelectionCommands();
            _NotifySortSelectionCommands();
        }

        private void _NotifyColumnSelectionCommands()
        {
            RemoveSelectedColumnCommand.NotifyCanExecuteChanged();
            MoveSelectedColumnUpCommand.NotifyCanExecuteChanged();
            MoveSelectedColumnDownCommand.NotifyCanExecuteChanged();
        }

        private void _NotifySortSelectionCommands()
        {
            RemoveSelectedSortKeyCommand.NotifyCanExecuteChanged();
            MoveSelectedSortKeyUpCommand.NotifyCanExecuteChanged();
            MoveSelectedSortKeyDownCommand.NotifyCanExecuteChanged();
            ToggleSelectedSortDirectionCommand.NotifyCanExecuteChanged();
        }

        private IReadOnlyList<RenameListField> _BuildAvailableOriginalFields(string? groupId)
        {
            if (string.IsNullOrEmpty(groupId))
            {
                return [];
            }

            return
            [
                .. RenameListFieldCatalog
                    .GetFieldsForGroup(groupId)
                    .Where(field => !_selectedColumnKeys.Contains(field.OriginalKey)),
            ];
        }

        private IReadOnlyList<RenameListField> _BuildAvailablePreviewFields(string? groupId)
        {
            if (string.IsNullOrEmpty(groupId))
            {
                return [];
            }

            return
            [
                .. RenameListFieldCatalog
                    .GetFieldsForGroup(groupId)
                    .Where(field => field.SupportsPreview && !_selectedColumnKeys.Contains(field.PreviewKey)),
            ];
        }

        private IReadOnlyList<RenameListField> _BuildAvailableSortFields(string? groupId)
        {
            if (string.IsNullOrEmpty(groupId))
            {
                return [];
            }

            return
            [
                .. RenameListFieldCatalog
                    .GetFieldsForGroup(groupId)
                    .Where(field =>
                        field is { IsSortable: true, SortColumn: not null }
                        && !_selectedSortColumns.Contains(field.SortColumn.Value)
                    ),
            ];
        }

        private static IReadOnlyList<RenameListFieldGroupOption> _BuildGroups()
        {
            return
            [
                .. RenameListFieldCatalog
                    .All.GroupBy(field => field.GroupId)
                    .Select(group => new RenameListFieldGroupOption(group.Key, group.First().GroupDisplayName)),
            ];
        }
    }
}
