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
        private readonly OrderedDraft<RenameListSortColumn, RenameListSortKey> _sortKeys;
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
            _sortKeys = new OrderedDraft<RenameListSortColumn, RenameListSortKey>(sortKeys, key => key.Column);

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
                if (_suppressSelectionSync || _columns.SelectedIndex == value)
                {
                    return;
                }

                // ListBox writes -1 when ItemsSource is rebuilt; keep the draft selection.
                if (value < 0 && _columns.HasItems)
                {
                    return;
                }

                _columns.SelectedIndex = value;
                OnPropertyChanged();
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
                if (_suppressSelectionSync || _sortKeys.SelectedIndex == value)
                {
                    return;
                }

                // ListBox writes -1 when ItemsSource is rebuilt; keep the draft selection.
                if (value < 0 && _sortKeys.HasItems)
                {
                    return;
                }

                _sortKeys.SelectedIndex = value;
                OnPropertyChanged();
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
            if (!_columns.TryRemoveSelected())
            {
                return;
            }

            _RefreshLists();
        }

        /// <summary>
        /// Moves the selected visible column up.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanMoveSelectedColumnUp))]
        public void MoveSelectedColumnUp()
        {
            if (!_columns.TryMoveSelected(-1))
            {
                return;
            }

            _RefreshLists();
        }

        /// <summary>
        /// Moves the selected visible column down.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanMoveSelectedColumnDown))]
        public void MoveSelectedColumnDown()
        {
            if (!_columns.TryMoveSelected(1))
            {
                return;
            }

            _RefreshLists();
        }

        /// <summary>
        /// Clears all selected visible columns.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_HasSelectedColumns))]
        public void ClearSelectedColumns()
        {
            _columns.Clear();
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
            if (!_sortKeys.TryRemoveSelected())
            {
                return;
            }

            _RefreshLists();
        }

        /// <summary>
        /// Moves the selected sort key up in priority.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanMoveSelectedSortKeyUp))]
        public void MoveSelectedSortKeyUp()
        {
            if (!_sortKeys.TryMoveSelected(-1))
            {
                return;
            }

            _RefreshLists();
        }

        /// <summary>
        /// Moves the selected sort key down in priority.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanMoveSelectedSortKeyDown))]
        public void MoveSelectedSortKeyDown()
        {
            if (!_sortKeys.TryMoveSelected(1))
            {
                return;
            }

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
            return _columns.CanRemove;
        }

        private bool _CanMoveSelectedColumnUp()
        {
            return _columns.CanMoveUp;
        }

        private bool _CanMoveSelectedColumnDown()
        {
            return _columns.CanMoveDown;
        }

        private bool _HasSelectedColumns()
        {
            return _columns.HasItems;
        }

        private bool _CanAddSelectedSortField()
        {
            return SelectedAvailableSortField is { SortColumn: not null };
        }

        private bool _CanRemoveSelectedSortKey()
        {
            return _sortKeys.CanRemove;
        }

        private bool _CanMoveSelectedSortKeyUp()
        {
            return _sortKeys.CanMoveUp;
        }

        private bool _CanMoveSelectedSortKeyDown()
        {
            return _sortKeys.CanMoveDown;
        }

        private bool _CanToggleSelectedSortDirection()
        {
            return _sortKeys.CanRemove;
        }

        private bool _HasSelectedSortKeys()
        {
            return _sortKeys.HasItems;
        }

        private void _AddColumn(RenameListFieldKey key, bool refresh = true)
        {
            if (!_columns.TryAdd(new RenameListVisibleColumn(key)))
            {
                return;
            }

            if (refresh)
            {
                _RefreshLists();
            }
        }

        private void _AddSortKey(RenameListSortColumn sortColumn)
        {
            if (!_sortKeys.TryAdd(new RenameListSortKey(sortColumn)))
            {
                return;
            }

            _RefreshLists();
        }

        private void _ClearAvailableSelections()
        {
            SelectedAvailableOriginalField = null;
            SelectedAvailablePreviewField = null;
            SelectedAvailableSortField = null;
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
                .. fieldsInGroup.Where(field =>
                    field is { IsSortable: true, SortColumn: not null } && !_sortKeys.Contains(field.SortColumn.Value)
                ),
            ];
            SelectedColumnRows =
            [
                .. _columns.Items.Select((column, index) => new RenameListFieldShuttleColumnRow(index, column)),
            ];
            SelectedSortRows =
            [
                .. _sortKeys.Items.Select((key, index) => new RenameListFieldShuttleSortRow(index, key)),
            ];

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
