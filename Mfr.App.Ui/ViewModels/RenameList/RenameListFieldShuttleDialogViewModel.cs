using CommunityToolkit.Mvvm.Input;
using Mfr.Models.RenameList;
using Mfr.Models.RenameList.Fields.Basic;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Draft state for the unified Rename List field shuttle dialog (Columns and Sort tabs).
    /// </summary>
    public sealed partial class RenameListFieldShuttleDialogViewModel : ViewModelBase
    {
        private readonly OrderedDraft<RenameListFieldKey, RenameListVisibleColumn> _columns;
        private readonly OrderedDraft<RenameListFieldKey, RenameListSortKey> _sortKeys;
        private readonly IReadOnlyList<RenameListFieldKey> _relevantFieldKeys;
        private readonly IReadOnlyDictionary<RenameListFieldKey, int>? _rememberedColumnWidths;
        private readonly bool _canUseFilterChain;
        private bool _suppressSelectionSync;
        private bool _isAbModeEnabled;
        private RenameListFieldGroupOption? _browseSelectedGroup;

        /// <summary>
        /// Initializes the shuttle from the Rename List's current column layout and sort keys.
        /// </summary>
        /// <param name="visibleColumns">Current visible columns in grid order.</param>
        /// <param name="sortKeys">Current Auto-Sort keys in priority order.</param>
        /// <param name="initialTab">Tab to show when the dialog opens.</param>
        /// <param name="relevantFieldKeys">
        /// Snapshot of field keys inferred from the Filter Chain (may be empty).
        /// </param>
        /// <param name="canUseFilterChain">
        /// When <see langword="true"/>, Add/Set columns from filters are enabled (non-empty chain).
        /// </param>
        /// <param name="abModeEnabled">
        /// Initial A/B Mode draft (session value). When <see langword="true"/>, columns are normalized to
        /// originals-only and the Preview Fields subtab stays hidden until the draft is turned off.
        /// </param>
        /// <param name="rememberedColumnWidths">
        /// Snapshot of absolute widths for catalog-default columns (null when Options remembering is off).
        /// </param>
        public RenameListFieldShuttleDialogViewModel(
            IReadOnlyList<RenameListVisibleColumn> visibleColumns,
            IReadOnlyList<RenameListSortKey> sortKeys,
            RenameListFieldShuttleTab initialTab = RenameListFieldShuttleTab.Columns,
            IReadOnlyList<RenameListFieldKey>? relevantFieldKeys = null,
            bool canUseFilterChain = false,
            bool abModeEnabled = false,
            IReadOnlyDictionary<RenameListFieldKey, int>? rememberedColumnWidths = null
        )
        {
            ArgumentNullException.ThrowIfNull(visibleColumns);
            ArgumentNullException.ThrowIfNull(sortKeys);

            var columns = abModeEnabled ? RenameListVisibleColumn.NormalizeToOriginals(visibleColumns) : visibleColumns;
            _columns = new OrderedDraft<RenameListFieldKey, RenameListVisibleColumn>(columns, column => column.Key);
            _sortKeys = new OrderedDraft<RenameListFieldKey, RenameListSortKey>(sortKeys, key => key.FieldKey);
            _relevantFieldKeys = relevantFieldKeys ?? [];
            _rememberedColumnWidths = rememberedColumnWidths;
            _canUseFilterChain = canUseFilterChain;
            _isAbModeEnabled = abModeEnabled;

            Groups = _BuildGroups();
            SelectedGroup =
                Groups.FirstOrDefault(group => group.GroupId == BasicRenameListField.Group)
                ?? (Groups.Count > 0 ? Groups[0] : null);
            SelectedTabIndex = (int)initialTab;
            _RefreshLists();
        }

        /// <summary>
        /// Gets or sets the draft A/B Mode flag. Committed with columns/sort on OK; discarded on Cancel.
        /// <para>
        /// When turned on, the selected list is normalized to originals-only, the Preview Fields subtab is
        /// hidden, and preview keys cannot be added until the draft is turned off. When turned off, a
        /// preview companion is inserted after each original that supports preview.
        /// </para>
        /// </summary>
        public bool IsAbModeEnabled
        {
            get => _isAbModeEnabled;
            set
            {
                if (_isAbModeEnabled == value)
                {
                    return;
                }

                _isAbModeEnabled = value;
                OnPropertyChanged();
                if (value)
                {
                    IsPreviewColumnsTab = false;
                    _NormalizeSelectedColumnsToOriginals();
                }
                else
                {
                    _ExpandSelectedColumnsWithPreviewCompanions();
                }

                AddSelectedPreviewFieldCommand.NotifyCanExecuteChanged();
                AddAllPreviewFieldsCommand.NotifyCanExecuteChanged();
            }
        }

        /// <summary>
        /// Gets property groups available in the shuttle groups list.
        /// </summary>
        public IReadOnlyList<RenameListFieldGroupOption> Groups { get; }

        /// <summary>
        /// Gets or sets the selected property group for group browse.
        /// <para>
        /// While <see cref="IsFieldSearchActive"/>, returns <see langword="null"/> so the Groups list
        /// shows no highlight (browse selection is kept and restored when search clears).
        /// </para>
        /// </summary>
        public RenameListFieldGroupOption? SelectedGroup
        {
            get => IsFieldSearchActive ? null : _browseSelectedGroup;
            set
            {
                if (IsFieldSearchActive)
                {
                    // ListBox may push null when the bound value clears for search; keep browse group.
                    return;
                }

                if (_browseSelectedGroup == value)
                {
                    return;
                }

                _browseSelectedGroup = value;
                OnPropertyChanged();
                _ClearAvailableSelections();
                _RefreshLists();
            }
        }

        /// <summary>
        /// Gets or sets the shared field search filter for Columns and Sort available lists.
        /// <para>
        /// Empty or whitespace keeps group browse. Non-empty filters
        /// <see cref="RenameListFieldCatalog.All"/> (already-selected / preview / sortable gates still apply).
        /// Search text is kept after Add so the shuttle stays multi-add friendly.
        /// </para>
        /// </summary>
        public string SearchText
        {
            get;
            set
            {
                value ??= string.Empty;
                if (field == value)
                {
                    return;
                }

                var wasSearchActive = field.Trim().Length > 0;
                field = value;
                var isSearchActive = field.Trim().Length > 0;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsFieldSearchActive));
                if (wasSearchActive != isSearchActive)
                {
                    OnPropertyChanged(nameof(SelectedGroup));
                }

                ClearSearchCommand.NotifyCanExecuteChanged();
                _RefreshLists();
            }
        } = string.Empty;

        /// <summary>
        /// Gets whether available fields are filtered across groups (non-empty trimmed <see cref="SearchText"/>).
        /// </summary>
        public bool IsFieldSearchActive => SearchText.Trim().Length > 0;

        /// <summary>
        /// Clears <see cref="SearchText"/> and restores group browse (including the previous group highlight).
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanClearSearch))]
        public void ClearSearch()
        {
            SearchText = string.Empty;
        }

        private bool _CanClearSearch()
        {
            return IsFieldSearchActive;
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
                if (value && IsAbModeEnabled)
                {
                    value = false;
                }

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
        /// Gets original fields available to add on the Columns tab.
        /// <para>
        /// Selected group when search is idle; catalog matches when <see cref="IsFieldSearchActive"/>.
        /// </para>
        /// </summary>
        public IReadOnlyList<RenameListField> AvailableOriginalFields { get; private set; } = [];

        /// <summary>
        /// Gets preview fields available to add on the Columns tab.
        /// <para>
        /// Selected group when search is idle; catalog matches when <see cref="IsFieldSearchActive"/>.
        /// </para>
        /// </summary>
        public IReadOnlyList<RenameListField> AvailablePreviewFields { get; private set; } = [];

        /// <summary>
        /// Gets sortable fields available to add on the Sort tab.
        /// <para>
        /// Selected group when search is idle; catalog matches when <see cref="IsFieldSearchActive"/>.
        /// </para>
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
                SelectedAvailableOriginalFields = _AvailableListForAnchor(SelectedAvailableOriginalFields, value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedAvailableOriginalFields));
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
                SelectedAvailablePreviewFields = _AvailableListForAnchor(SelectedAvailablePreviewFields, value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedAvailablePreviewFields));
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
                SelectedAvailableSortFields = _AvailableListForAnchor(SelectedAvailableSortFields, value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedAvailableSortFields));
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

                _columns.SetSelection(value >= 0 ? [value] : [], value);
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

                _sortKeys.SetSelection(value >= 0 ? [value] : [], value);
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
            _AddColumns(SelectedAvailableOriginalFields.Select(field => field.OriginalKey));
        }

        /// <summary>
        /// Adds the selected available preview field to the visible-column list.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanAddSelectedPreviewField))]
        public void AddSelectedPreviewField()
        {
            _AddColumns(SelectedAvailablePreviewFields.Select(field => field.PreviewKey));
        }

        /// <summary>
        /// Adds all fields currently shown in <see cref="AvailableOriginalFields"/> to the visible-column list.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_HasAvailableOriginalFields))]
        public void AddAllOriginalFields()
        {
            var keys = AvailableOriginalFields.Select(field => field.OriginalKey);
            _AddColumns(keys);
        }

        /// <summary>
        /// Adds all fields currently shown in <see cref="AvailablePreviewFields"/> to the visible-column list.
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
            if (_columns.TryRemoveAtIndices(_columns.SelectedIndices) == 0)
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
            if (!_columns.TryMoveBlock(-1))
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
            if (!_columns.TryMoveBlock(1))
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
        /// Merges Filter Chain–relevant keys into the draft Selected fields list (missing keys only).
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanUseFilterChain))]
        public void AddColumnsFromFilters()
        {
            if (!_canUseFilterChain)
            {
                return;
            }

            var items = _ColumnsForRelevantKeys(_relevantFieldKeys);
            if (items.Count == 0)
            {
                return;
            }

            if (_columns.TryInsertMany(_columns.Items.Count, items) == 0)
            {
                return;
            }

            _RefreshLists();
        }

        /// <summary>
        /// Sets the draft Selected fields to catalog defaults, then appends remaining relevant keys.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Widths for keys that were already in the draft Selected list are preserved.
        /// </para>
        /// </remarks>
        [RelayCommand(CanExecute = nameof(_CanUseFilterChain))]
        public void SetColumnsFromFilters()
        {
            if (!_canUseFilterChain)
            {
                return;
            }

            var previousColumns = _columns.Items.ToList();
            var columns = RenameListVisibleColumn.BuildSetFromFiltersColumns(
                _relevantFieldKeys,
                previousColumns,
                _rememberedColumnWidths,
                originalsOnly: IsAbModeEnabled
            );

            _columns.Clear();
            if (_columns.TryInsertMany(0, columns) == 0)
            {
                _RefreshLists();
                return;
            }

            _RefreshLists();
        }

        /// <summary>
        /// Adds the selected available sort field to the sort-key list.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanAddSelectedSortField))]
        public void AddSelectedSortField()
        {
            _AddSortKeys(SelectedAvailableSortFields.Select(field => field.OriginalKey));
        }

        /// <summary>
        /// Removes the selected sort key.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanRemoveSelectedSortKey))]
        public void RemoveSelectedSortKey()
        {
            if (_sortKeys.TryRemoveAtIndices(_sortKeys.SelectedIndices) == 0)
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
            if (!_sortKeys.TryMoveBlock(-1))
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
            if (!_sortKeys.TryMoveBlock(1))
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
            ToggleSortDirectionAt(_sortKeys.SelectedIndex);
        }

        /// <summary>
        /// Toggles ascending/descending for the sort key at <paramref name="index"/> without changing row selection.
        /// </summary>
        /// <param name="index">Sort-row index to toggle.</param>
        public void ToggleSortDirectionAt(int index)
        {
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
            return SelectedAvailableOriginalFields.Count > 0;
        }

        private bool _CanAddSelectedPreviewField()
        {
            return !IsAbModeEnabled && SelectedAvailablePreviewFields.Count > 0;
        }

        private bool _HasAvailableOriginalFields()
        {
            return AvailableOriginalFields.Count > 0;
        }

        private bool _HasAvailablePreviewFields()
        {
            return !IsAbModeEnabled && AvailablePreviewFields.Count > 0;
        }

        private bool _CanRemoveSelectedColumn()
        {
            return _columns.SelectedIndices.Count > 0;
        }

        private bool _CanMoveSelectedColumnUp()
        {
            return _columns.CanMoveBlock(offset: -1);
        }

        private bool _CanMoveSelectedColumnDown()
        {
            return _columns.CanMoveBlock(offset: 1);
        }

        private bool _HasSelectedColumns()
        {
            return _columns.HasItems;
        }

        private bool _CanAddSelectedSortField()
        {
            return SelectedAvailableSortFields.Count > 0;
        }

        private bool _CanRemoveSelectedSortKey()
        {
            return _sortKeys.SelectedIndices.Count > 0;
        }

        private bool _CanMoveSelectedSortKeyUp()
        {
            return _sortKeys.CanMoveBlock(offset: -1);
        }

        private bool _CanMoveSelectedSortKeyDown()
        {
            return _sortKeys.CanMoveBlock(offset: 1);
        }

        private bool _CanToggleSelectedSortDirection()
        {
            return _sortKeys.CanRemove;
        }

        private bool _HasSelectedSortKeys()
        {
            return _sortKeys.HasItems;
        }

        private bool _CanUseFilterChain()
        {
            return _canUseFilterChain;
        }

        private void _AddColumns(IEnumerable<RenameListFieldKey> keys)
        {
            var insertIndex = _columns.GetInsertIndexBelow();
            var draftColumns = _KeysAllowedInSelectedColumns(keys)
                .Select(key => new RenameListVisibleColumn(key))
                .ToList();
            var items = RenameListVisibleColumn.WithRememberedWidths(draftColumns, _rememberedColumnWidths).ToList();
            if (items.Count == 0)
            {
                return;
            }

            if (_columns.TryInsertMany(insertIndex, items) == 0)
            {
                return;
            }

            _RefreshLists();
        }

        /// <summary>
        /// Rewrites the draft selected list to originals-only (preview→original + dedupe).
        /// </summary>
        private void _NormalizeSelectedColumnsToOriginals()
        {
            var normalized = RenameListVisibleColumn.NormalizeToOriginals(_columns.Items);
            if (normalized.SequenceEqual(_columns.Items))
            {
                return;
            }

            _columns.Clear();
            _ = _columns.TryInsertMany(0, normalized);
            _RefreshLists();
        }

        /// <summary>
        /// Inserts preview companions after each original when the A/B Mode draft is turned off.
        /// </summary>
        private void _ExpandSelectedColumnsWithPreviewCompanions()
        {
            var expanded = RenameListVisibleColumn.WithRememberedWidths(
                RenameListVisibleColumn.WithPreviewCompanions(_columns.Items),
                _rememberedColumnWidths
            );
            if (expanded.SequenceEqual(_columns.Items))
            {
                return;
            }

            _columns.Clear();
            _ = _columns.TryInsertMany(0, expanded);
            _RefreshLists();
        }

        /// <summary>
        /// Keys that may be inserted into Selected fields: drops preview keys while A/B Mode draft is on.
        /// </summary>
        private IEnumerable<RenameListFieldKey> _KeysAllowedInSelectedColumns(IEnumerable<RenameListFieldKey> keys)
        {
            return IsAbModeEnabled ? keys.Where(key => !key.IsPreview) : keys;
        }

        /// <summary>
        /// Builds draft columns for Filter Chain keys; originals-only when A/B Mode draft is on.
        /// </summary>
        private List<RenameListVisibleColumn> _ColumnsForRelevantKeys(IReadOnlyList<RenameListFieldKey> keys)
        {
            var keysToAdd = IsAbModeEnabled ? RenameListVisibleColumn.ToOriginalKeysFirstSeen(keys) : keys;
            var draftColumns = keysToAdd.Select(key => new RenameListVisibleColumn(key)).ToList();
            return [.. RenameListVisibleColumn.WithRememberedWidths(draftColumns, _rememberedColumnWidths)];
        }

        private void _AddSortKeys(IEnumerable<RenameListFieldKey> fieldKeys)
        {
            var insertIndex = _sortKeys.GetInsertIndexBelow();
            var items = fieldKeys.Select(fieldKey => new RenameListSortKey(fieldKey)).ToList();
            if (items.Count == 0)
            {
                return;
            }

            if (_sortKeys.TryInsertMany(insertIndex, items) == 0)
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

        /// <summary>
        /// Drops available-list highlights that are no longer in the current catalog pane.
        /// </summary>
        private void _PruneAvailableSelections()
        {
            SelectedAvailableOriginalFields = _PruneFields(SelectedAvailableOriginalFields, AvailableOriginalFields);
            SelectedAvailablePreviewFields = _PruneFields(SelectedAvailablePreviewFields, AvailablePreviewFields);
            SelectedAvailableSortFields = _PruneFields(SelectedAvailableSortFields, AvailableSortFields);

            if (SelectedAvailableOriginalField is { } original && !AvailableOriginalFields.Contains(original))
            {
                SelectedAvailableOriginalField = _LastOrNull(SelectedAvailableOriginalFields);
            }

            if (SelectedAvailablePreviewField is { } preview && !AvailablePreviewFields.Contains(preview))
            {
                SelectedAvailablePreviewField = _LastOrNull(SelectedAvailablePreviewFields);
            }

            if (SelectedAvailableSortField is { } sort && !AvailableSortFields.Contains(sort))
            {
                SelectedAvailableSortField = _LastOrNull(SelectedAvailableSortFields);
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
            var candidateFields = _CandidateAvailableFields();
            AvailableOriginalFields = [.. candidateFields.Where(field => !_columns.Contains(field.OriginalKey))];
            AvailablePreviewFields =
            [
                .. candidateFields.Where(field => field.SupportsPreview && !_columns.Contains(field.PreviewKey)),
            ];
            AvailableSortFields =
            [
                .. candidateFields.Where(field => field.IsSortable && !_sortKeys.Contains(field.OriginalKey)),
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

        /// <summary>
        /// Candidate fields for available lists: selected group when search is idle; catalog matches when active.
        /// </summary>
        private IReadOnlyList<RenameListField> _CandidateAvailableFields()
        {
            var query = SearchText.Trim();
            if (query.Length == 0)
            {
                return _FieldsInSelectedGroup();
            }

            return [.. RenameListFieldCatalog.All.Where(field => _FieldMatchesSearch(field, query))];
        }

        private IReadOnlyList<RenameListField> _FieldsInSelectedGroup()
        {
            var groupId = _browseSelectedGroup?.GroupId;
            if (string.IsNullOrEmpty(groupId))
            {
                return [];
            }

            return RenameListFieldCatalog.GetFieldsForGroup(groupId);
        }

        /// <summary>
        /// OR match on display name, property key, group label, group id, and tip (null-safe).
        /// </summary>
        private static bool _FieldMatchesSearch(RenameListField field, string query)
        {
            return field.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
                || field.PropertyKey.Contains(query, StringComparison.OrdinalIgnoreCase)
                || field.GroupDisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
                || field.GroupId.Contains(query, StringComparison.OrdinalIgnoreCase)
                || (field.Tip is { } tip && tip.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        private static IReadOnlyList<RenameListFieldGroupOption> _BuildGroups()
        {
            return
            [
                .. RenameListFieldCatalog
                    .All.GroupBy(field => field.GroupId)
                    .Select(group => new RenameListFieldGroupOption(group.Key, group.First().GroupDisplayName))
                    .OrderBy(group => group.DisplayName, StringComparer.OrdinalIgnoreCase),
            ];
        }
    }
}
