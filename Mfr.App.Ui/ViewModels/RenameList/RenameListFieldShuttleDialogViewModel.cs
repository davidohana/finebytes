using CommunityToolkit.Mvvm.Input;
using Mfr.Models.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Draft state for the unified Rename List field shuttle dialog (Columns and Sort tabs).
    /// </summary>
    public sealed partial class RenameListFieldShuttleDialogViewModel : ViewModelBase
    {
        private readonly OrderedDraft<RenameListFieldKey, RenameListVisibleColumn> _columns;
        private readonly OrderedDraft<RenameListFieldKey, RenameListSortKey> _sortKeys;
        private bool _suppressSelectionSync;

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

            _columns = new OrderedDraft<RenameListFieldKey, RenameListVisibleColumn>(
                visibleColumns,
                column => column.Key
            );
            _sortKeys = new OrderedDraft<RenameListFieldKey, RenameListSortKey>(sortKeys, key => key.FieldKey);

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
                    _selectedAvailableOriginalFields = [];
                    SelectedAvailableOriginalField = null;
                    OnPropertyChanged(nameof(SelectedAvailableOriginalFields));
                }
                else
                {
                    _selectedAvailablePreviewFields = [];
                    SelectedAvailablePreviewField = null;
                    OnPropertyChanged(nameof(SelectedAvailablePreviewFields));
                }

                AddSelectedOriginalFieldCommand.NotifyCanExecuteChanged();
                AddSelectedPreviewFieldCommand.NotifyCanExecuteChanged();
                AddAllOriginalFieldsCommand.NotifyCanExecuteChanged();
                AddAllPreviewFieldsCommand.NotifyCanExecuteChanged();
            }
        }

        /// <summary>
        /// Gets or sets whether the Columns tab is showing original available fields.
        /// </summary>
        public bool IsOriginalColumnsTab
        {
            get => !IsPreviewColumnsTab;
            set
            {
                if (value)
                {
                    IsPreviewColumnsTab = false;
                }
            }
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
            get => _columns.SelectedIndex;
            set
            {
                if (_suppressSelectionSync || _IsSingleColumnSelection(value))
                {
                    return;
                }

                _AssignColumnSelection(value >= 0 ? [value] : [], value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedColumnRowIndices));
                _NotifyColumnSelectionCommands();
            }
        }

        /// <summary>
        /// Gets or sets the selected row index in the selected-sort list.
        /// </summary>
        public int SelectedSortRowIndex
        {
            get => _sortKeys.SelectedIndex;
            set
            {
                if (_suppressSelectionSync || _IsSingleSortSelection(value))
                {
                    return;
                }

                _AssignSortSelection(value >= 0 ? [value] : [], value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedSortRowIndices));
                _NotifySortSelectionCommands();
            }
        }

        /// <summary>
        /// Gets whether OK can apply the draft (at least one visible column required).
        /// </summary>
        public bool CanConfirm => _columns.HasItems;

        /// <summary>
        /// Gets the draft visible columns to apply when OK is pressed.
        /// </summary>
        public IReadOnlyList<RenameListVisibleColumn> ResultColumns => _columns.Items;

        /// <summary>
        /// Gets the draft sort keys to apply when OK is pressed.
        /// </summary>
        public IReadOnlyList<RenameListSortKey> ResultSortKeys => _sortKeys.Items;

        /// <summary>
        /// Adds the selected available original field to the visible-column list.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanAddSelectedOriginalField))]
        public void AddSelectedOriginalField()
        {
            var fields =
                _selectedAvailableOriginalFields.Count > 0 ? _selectedAvailableOriginalFields
                : SelectedAvailableOriginalField is { } single ? [single]
                : Array.Empty<RenameListField>();

            _AddColumns(fields.Select(field => field.OriginalKey));
        }

        /// <summary>
        /// Adds the selected available preview field to the visible-column list.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanAddSelectedPreviewField))]
        public void AddSelectedPreviewField()
        {
            var fields =
                _selectedAvailablePreviewFields.Count > 0 ? _selectedAvailablePreviewFields
                : SelectedAvailablePreviewField is { } single ? [single]
                : Array.Empty<RenameListField>();

            _AddColumns(fields.Select(field => field.PreviewKey));
        }

        /// <summary>
        /// Adds all available original fields in the current group/tab to the visible-column list.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_HasAvailableOriginalFields))]
        public void AddAllOriginalFields()
        {
            var keys = AvailableOriginalFields.Select(field => field.OriginalKey);
            _AddColumns(keys);
        }

        /// <summary>
        /// Adds all available preview fields in the current group/tab to the visible-column list.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_HasAvailablePreviewFields))]
        public void AddAllPreviewFields()
        {
            var keys = AvailablePreviewFields.Select(field => field.PreviewKey);
            _AddColumns(keys);
        }

        /// <summary>
        /// Removes the selected visible column.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanRemoveSelectedColumn))]
        public void RemoveSelectedColumn()
        {
            var indices = _GetColumnMoveIndices();
            if (_columns.TryRemoveAtIndices(indices) == 0)
            {
                return;
            }

            _AssignColumnSelection(_columns.SelectedIndex >= 0 ? [_columns.SelectedIndex] : [], _columns.SelectedIndex);
            _RefreshLists();
        }

        /// <summary>
        /// Moves the selected visible column up.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanMoveSelectedColumnUp))]
        public void MoveSelectedColumnUp()
        {
            var indices = _GetColumnMoveIndices();
            if (!_columns.TryMoveBlock(indices, -1, out var newIndices))
            {
                return;
            }

            _AssignColumnSelection(newIndices, _columns.SelectedIndex);
            _RefreshLists();
        }

        /// <summary>
        /// Moves the selected visible column down.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanMoveSelectedColumnDown))]
        public void MoveSelectedColumnDown()
        {
            var indices = _GetColumnMoveIndices();
            if (!_columns.TryMoveBlock(indices, 1, out var newIndices))
            {
                return;
            }

            _AssignColumnSelection(newIndices, _columns.SelectedIndex);
            _RefreshLists();
        }

        /// <summary>
        /// Clears all selected visible columns.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_HasSelectedColumns))]
        public void ClearSelectedColumns()
        {
            _columns.Clear();
            _AssignColumnSelection([], -1);
            _RefreshLists();
        }

        /// <summary>
        /// Adds the selected available sort field to the sort-key list.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanAddSelectedSortField))]
        public void AddSelectedSortField()
        {
            var fields =
                _selectedAvailableSortFields.Count > 0 ? _selectedAvailableSortFields
                : SelectedAvailableSortField is { } single ? [single]
                : Array.Empty<RenameListField>();

            _AddSortKeys(fields.Select(field => field.OriginalKey));
        }

        /// <summary>
        /// Removes the selected sort key.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanRemoveSelectedSortKey))]
        public void RemoveSelectedSortKey()
        {
            var indices = _GetSortMoveIndices();
            if (_sortKeys.TryRemoveAtIndices(indices) == 0)
            {
                return;
            }

            _AssignSortSelection(
                _sortKeys.SelectedIndex >= 0 ? [_sortKeys.SelectedIndex] : [],
                _sortKeys.SelectedIndex
            );
            _RefreshLists();
        }

        /// <summary>
        /// Moves the selected sort key up in priority.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanMoveSelectedSortKeyUp))]
        public void MoveSelectedSortKeyUp()
        {
            var indices = _GetSortMoveIndices();
            if (!_sortKeys.TryMoveBlock(indices, -1, out var newIndices))
            {
                return;
            }

            _AssignSortSelection(newIndices, _sortKeys.SelectedIndex);
            _RefreshLists();
        }

        /// <summary>
        /// Moves the selected sort key down in priority.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanMoveSelectedSortKeyDown))]
        public void MoveSelectedSortKeyDown()
        {
            var indices = _GetSortMoveIndices();
            if (!_sortKeys.TryMoveBlock(indices, 1, out var newIndices))
            {
                return;
            }

            _AssignSortSelection(newIndices, _sortKeys.SelectedIndex);
            _RefreshLists();
        }

        /// <summary>
        /// Toggles ascending/descending for the selected sort key.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanToggleSelectedSortDirection))]
        public void ToggleSelectedSortDirection()
        {
            var index = _sortKeys.SelectedIndex;
            if (index < 0 || index >= _sortKeys.Items.Count)
            {
                return;
            }

            var existing = _sortKeys.Items[index];
            _sortKeys.TrySetItem(index, existing with { Descending = !existing.Descending });
            _RefreshLists();
        }

        /// <summary>
        /// Clears all selected sort keys (Auto-Sort off).
        /// </summary>
        [RelayCommand(CanExecute = nameof(_HasSelectedSortKeys))]
        public void ClearSelectedSortKeys()
        {
            _sortKeys.Clear();
            _AssignSortSelection([], -1);
            _RefreshLists();
        }

        private bool _CanAddSelectedOriginalField()
        {
            return _selectedAvailableOriginalFields.Count > 0 || SelectedAvailableOriginalField is not null;
        }

        private bool _CanAddSelectedPreviewField()
        {
            return _selectedAvailablePreviewFields.Count > 0 || SelectedAvailablePreviewField is not null;
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
            return _GetColumnMoveIndices().Count > 0;
        }

        private bool _CanMoveSelectedColumnUp()
        {
            return _CanMoveToward(_GetColumnMoveIndices(), _columns.Items.Count, offset: -1);
        }

        private bool _CanMoveSelectedColumnDown()
        {
            return _CanMoveToward(_GetColumnMoveIndices(), _columns.Items.Count, offset: 1);
        }

        private bool _HasSelectedColumns()
        {
            return _columns.HasItems;
        }

        private bool _CanAddSelectedSortField()
        {
            return _selectedAvailableSortFields.Count > 0 || SelectedAvailableSortField is not null;
        }

        private bool _CanRemoveSelectedSortKey()
        {
            return _GetSortMoveIndices().Count > 0;
        }

        private bool _CanMoveSelectedSortKeyUp()
        {
            return _CanMoveToward(_GetSortMoveIndices(), _sortKeys.Items.Count, offset: -1);
        }

        private bool _CanMoveSelectedSortKeyDown()
        {
            return _CanMoveToward(_GetSortMoveIndices(), _sortKeys.Items.Count, offset: 1);
        }

        private bool _CanToggleSelectedSortDirection()
        {
            return _sortKeys.CanRemove;
        }

        private bool _HasSelectedSortKeys()
        {
            return _sortKeys.HasItems;
        }

        private void _AddColumns(IEnumerable<RenameListFieldKey> keys)
        {
            var insertIndex = _GetInsertIndexBelow(
                _selectedColumnRowIndices,
                _columns.SelectedIndex,
                _columns.Items.Count
            );
            var items = keys.Select(key => new RenameListVisibleColumn(key)).ToList();
            if (items.Count == 0)
            {
                return;
            }

            var insertedCount = _columns.TryInsertMany(insertIndex, items);
            if (insertedCount == 0)
            {
                return;
            }

            _AssignColumnSelection(
                _BuildInsertedSelectionIndices(insertIndex, insertedCount, _columns.Items.Count),
                _columns.SelectedIndex
            );
            _RefreshLists();
        }

        private void _AddSortKeys(IEnumerable<RenameListFieldKey> fieldKeys)
        {
            var insertIndex = _GetInsertIndexBelow(
                _selectedSortRowIndices,
                _sortKeys.SelectedIndex,
                _sortKeys.Items.Count
            );
            var items = fieldKeys.Select(fieldKey => new RenameListSortKey(fieldKey)).ToList();
            if (items.Count == 0)
            {
                return;
            }

            var insertedCount = _sortKeys.TryInsertMany(insertIndex, items);
            if (insertedCount == 0)
            {
                return;
            }

            _AssignSortSelection(
                _BuildInsertedSelectionIndices(insertIndex, insertedCount, _sortKeys.Items.Count),
                _sortKeys.SelectedIndex
            );
            _RefreshLists();
        }

        private void _ClearAvailableSelections()
        {
            SelectedAvailableOriginalField = null;
            SelectedAvailablePreviewField = null;
            SelectedAvailableSortField = null;
            _selectedAvailableOriginalFields = [];
            _selectedAvailablePreviewFields = [];
            _selectedAvailableSortFields = [];
            OnPropertyChanged(nameof(SelectedAvailableOriginalFields));
            OnPropertyChanged(nameof(SelectedAvailablePreviewFields));
            OnPropertyChanged(nameof(SelectedAvailableSortFields));
        }

        /// <summary>
        /// Drops available-list highlights that are no longer in the current catalog pane.
        /// </summary>
        private void _PruneAvailableSelections()
        {
            _selectedAvailableOriginalFields = _PruneFields(_selectedAvailableOriginalFields, AvailableOriginalFields);
            _selectedAvailablePreviewFields = _PruneFields(_selectedAvailablePreviewFields, AvailablePreviewFields);
            _selectedAvailableSortFields = _PruneFields(_selectedAvailableSortFields, AvailableSortFields);

            if (SelectedAvailableOriginalField is { } original && !AvailableOriginalFields.Contains(original))
            {
                SelectedAvailableOriginalField = _LastOrNull(_selectedAvailableOriginalFields);
            }

            if (SelectedAvailablePreviewField is { } preview && !AvailablePreviewFields.Contains(preview))
            {
                SelectedAvailablePreviewField = _LastOrNull(_selectedAvailablePreviewFields);
            }

            if (SelectedAvailableSortField is { } sort && !AvailableSortFields.Contains(sort))
            {
                SelectedAvailableSortField = _LastOrNull(_selectedAvailableSortFields);
            }
        }

        private static IReadOnlyList<RenameListField> _PruneFields(
            IReadOnlyList<RenameListField> selected,
            IReadOnlyList<RenameListField> available
        )
        {
            if (selected.Count == 0)
            {
                return selected;
            }

            return [.. selected.Where(available.Contains)];
        }

        private void _RefreshLists()
        {
            var fieldsInGroup = _FieldsInSelectedGroup();
            AvailableOriginalFields = [.. fieldsInGroup.Where(field => !_columns.Contains(field.OriginalKey))];
            AvailablePreviewFields =
            [
                .. fieldsInGroup.Where(field => field.SupportsPreview && !_columns.Contains(field.PreviewKey)),
            ];
            AvailableSortFields =
            [
                .. fieldsInGroup.Where(field => field.IsSortable && !_sortKeys.Contains(field.OriginalKey)),
            ];
            SelectedColumnRows =
            [
                .. _columns.Items.Select((column, index) => new RenameListFieldShuttleColumnRow(index, column)),
            ];
            SelectedSortRows =
            [
                .. _sortKeys.Items.Select((key, index) => new RenameListFieldShuttleSortRow(index, key)),
            ];
            _PruneAvailableSelections();

            _suppressSelectionSync = true;
            try
            {
                OnPropertyChanged(nameof(AvailableOriginalFields));
                OnPropertyChanged(nameof(AvailablePreviewFields));
                OnPropertyChanged(nameof(AvailableSortFields));
                OnPropertyChanged(nameof(SelectedColumnRows));
                OnPropertyChanged(nameof(SelectedSortRows));
                OnPropertyChanged(nameof(CanConfirm));
            }
            finally
            {
                _suppressSelectionSync = false;
            }

            _NotifyColumnSelectionIndexChanged();
            _NotifySortSelectionIndexChanged();
            OnPropertyChanged(nameof(SelectedColumnRowIndices));
            OnPropertyChanged(nameof(SelectedSortRowIndices));
            OnPropertyChanged(nameof(SelectedAvailableOriginalFields));
            OnPropertyChanged(nameof(SelectedAvailablePreviewFields));
            OnPropertyChanged(nameof(SelectedAvailableSortFields));

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
        }

        private void _NotifyColumnSelectionIndexChanged()
        {
            OnPropertyChanged(nameof(SelectedColumnRowIndex));
            _NotifyColumnSelectionCommands();
        }

        private void _NotifySortSelectionIndexChanged()
        {
            OnPropertyChanged(nameof(SelectedSortRowIndex));
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

        private IReadOnlyList<RenameListField> _FieldsInSelectedGroup()
        {
            var groupId = SelectedGroup?.GroupId;
            if (string.IsNullOrEmpty(groupId))
            {
                return [];
            }

            return RenameListFieldCatalog.GetFieldsForGroup(groupId);
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
