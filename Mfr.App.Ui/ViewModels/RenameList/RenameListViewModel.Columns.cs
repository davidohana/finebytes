using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.Filters;
using Mfr.Models.Config;
using Mfr.Models.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Visible grid columns for <see cref="RenameListViewModel"/>.
    /// </summary>
    public sealed partial class RenameListViewModel
    {
        private List<RenameListVisibleColumn> _visibleColumns = [.. RenameListVisibleColumn.CreateDefaults()];

        /// <summary>
        /// When true, <see cref="VisibleColumns"/> stay originals-only and <see cref="ProjectedColumns"/>
        /// follows <see cref="AbSide"/>.
        /// </summary>
        [ObservableProperty]
        private bool _isAbModeEnabled;

        /// <summary>
        /// Toolbar A/B side: <see cref="RenameListPrefs.AbSideOriginal"/> or
        /// <see cref="RenameListPrefs.AbSidePreview"/>.
        /// </summary>
        [ObservableProperty]
        private string _abSide = RenameListPrefs.AbSidePreview;

        /// <summary>
        /// Gets visible grid columns in left-to-right order.
        /// <para>
        /// While A/B Mode is on, this list is originals-only (persisted layout). Use
        /// <see cref="ProjectedColumns"/> for on-screen / export column order.
        /// </para>
        /// </summary>
        public IReadOnlyList<RenameListVisibleColumn> VisibleColumns => _visibleColumns;

        /// <summary>
        /// Gets the columns the grid / export should show for the current A/B Mode and side.
        /// <para>
        /// A/B off: same as <see cref="VisibleColumns"/>. Original side: originals only. Preview side:
        /// each original followed by a derived preview companion when the field supports preview
        /// (catalog default width; not persisted).
        /// </para>
        /// </summary>
        public IReadOnlyList<RenameListVisibleColumn> ProjectedColumns
        {
            get
            {
                if (!IsAbModeEnabled)
                {
                    return _visibleColumns;
                }

                if (string.Equals(AbSide, RenameListPrefs.AbSideOriginal, StringComparison.Ordinal))
                {
                    return _visibleColumns;
                }

                return _DerivePreviewSideColumns(_visibleColumns);
            }
        }

        /// <summary>
        /// Raised when the view should open the unified field shuttle dialog.
        /// </summary>
        public event EventHandler<RenameListFieldShuttleTab>? FieldShuttleRequested;

        /// <summary>
        /// Appends Rename List columns inferred from the entire applied filter chain (missing keys only).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Merges at the end with <see cref="RenameListVisibleColumn.UseCatalogDefaultWidth"/>.
        /// No-op when every relevant key is already visible. Hydrates metadata like field-shuttle apply.
        /// While A/B Mode is on, normalizes the merged list to originals-only at this call site.
        /// </para>
        /// </remarks>
        [RelayCommand(CanExecute = nameof(_CanApplyRelevantColumns))]
        public async Task AddRelevantColumnsAsync()
        {
            if (!_CanApplyRelevantColumns())
            {
                return;
            }

            var relevantKeys = _RelevantKeysForColumnApply();
            var keyToIsVisible = _visibleColumns.Select(column => column.Key).ToHashSet();
            var merged = _visibleColumns.ToList();
            if (!_AppendMissingRelevantColumns(merged, relevantKeys, keyToIsVisible))
            {
                return;
            }

            await _ApplyVisibleColumnsWithHydrateAsync(_NormalizeColumnsIfAbMode(merged)).ConfigureAwait(true);
        }

        /// <summary>
        /// Replaces visible columns with catalog defaults, then appends remaining chain-relevant keys.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When the chain maps to nothing, applies defaults only. Hydrates metadata like field-shuttle apply.
        /// While A/B Mode is on, normalizes to originals-only at this call site (defaults include a preview key).
        /// </para>
        /// </remarks>
        [RelayCommand(CanExecute = nameof(_CanApplyRelevantColumns))]
        public async Task ReplaceWithRelevantColumnsAsync()
        {
            if (!_CanApplyRelevantColumns())
            {
                return;
            }

            var relevantKeys = _RelevantKeysForColumnApply();
            var columns = RenameListVisibleColumn.CreateDefaults().ToList();
            var keyToIsPresent = columns.Select(column => column.Key).ToHashSet();
            _AppendMissingRelevantColumns(columns, relevantKeys, keyToIsPresent);

            await _ApplyVisibleColumnsWithHydrateAsync(_NormalizeColumnsIfAbMode(columns)).ConfigureAwait(true);
        }

        /// <summary>
        /// Replaces the visible column list.
        /// </summary>
        /// <param name="columns">New columns in grid order; at least one required.</param>
        /// <exception cref="ArgumentNullException"><paramref name="columns"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="columns"/> is empty, duplicates a field key, or contains an unknown field key.
        /// </exception>
        public void SetVisibleColumns(IReadOnlyList<RenameListVisibleColumn> columns)
        {
            ArgumentNullException.ThrowIfNull(columns);
            if (columns.Count == 0)
            {
                throw new ArgumentException("At least one visible column is required.", nameof(columns));
            }

            var seenKeys = new HashSet<RenameListFieldKey>();
            foreach (var column in columns)
            {
                if (!RenameListFieldCatalog.TryGetField(column.Key, out _))
                {
                    throw new ArgumentException(
                        $"Unknown Rename List field '{column.Key.GroupId}/{column.Key.PropertyKey}'.",
                        nameof(columns)
                    );
                }

                if (!seenKeys.Add(column.Key))
                {
                    throw new ArgumentException(
                        "Visible columns cannot contain duplicate field keys.",
                        nameof(columns)
                    );
                }
            }

            var toApply = _NormalizeColumnsIfAbMode(columns);
            _visibleColumns = [.. toApply];
            _NotifyVisibleAndProjectedColumnsChanged();
        }

        /// <summary>
        /// Reorders visible columns to match a new left-to-right key sequence.
        /// </summary>
        /// <param name="orderedKeys">
        /// Field keys in grid order. When A/B Preview side shows derived companions, pass the projected
        /// key sequence; preview keys are mapped to stored originals before reorder.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="orderedKeys"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="orderedKeys"/> is empty, duplicates a key, or does not match the current visible columns.
        /// </exception>
        public void ReorderVisibleColumns(IReadOnlyList<RenameListFieldKey> orderedKeys)
        {
            ArgumentNullException.ThrowIfNull(orderedKeys);
            if (orderedKeys.Count == 0)
            {
                throw new ArgumentException("At least one visible column is required.", nameof(orderedKeys));
            }

            var keysToApply = _MapReorderKeysToStoredOriginals(orderedKeys);

            var uniqueKeyCount = keysToApply.ToHashSet().Count;
            var hasWrongCountOrDuplicates =
                keysToApply.Count != _visibleColumns.Count || uniqueKeyCount != keysToApply.Count;
            if (hasWrongCountOrDuplicates)
            {
                throw new ArgumentException(
                    "Reordered keys must include every currently visible column exactly once.",
                    nameof(orderedKeys)
                );
            }

            var keyToColumn = _visibleColumns.ToDictionary(column => column.Key);
            if (keysToApply.Any(key => !keyToColumn.ContainsKey(key)))
            {
                throw new ArgumentException(
                    "Reordered keys must match the currently visible columns.",
                    nameof(orderedKeys)
                );
            }

            if (keysToApply.SequenceEqual(_visibleColumns.Select(column => column.Key)))
            {
                return;
            }

            _visibleColumns = [.. keysToApply.Select(key => keyToColumn[key])];
            _NotifyVisibleAndProjectedColumnsChanged();
        }

        /// <summary>
        /// Removes one visible column by field key.
        /// </summary>
        /// <param name="key">Field key to hide.</param>
        /// <remarks>
        /// <para>No-op when the key is absent or hiding would leave zero columns.</para>
        /// </remarks>
        public void HideColumn(RenameListFieldKey key)
        {
            if (_visibleColumns.Count <= 1)
            {
                return;
            }

            var index = _visibleColumns.FindIndex(column => column.Key == key);
            if (index < 0)
            {
                return;
            }

            var columns = _visibleColumns.ToList();
            columns.RemoveAt(index);
            SetVisibleColumns(columns);
        }

        /// <summary>
        /// Opens the unified field shuttle dialog on the Columns tab (context menu and toolbar).
        /// </summary>
        [RelayCommand]
        public void OpenFieldShuttle()
        {
            _RequestFieldShuttle(RenameListFieldShuttleTab.Columns);
        }

        /// <summary>
        /// Opens the unified field shuttle dialog on the Sort tab (context and main menus).
        /// </summary>
        [RelayCommand]
        public void OpenEditSortFields()
        {
            _RequestFieldShuttle(RenameListFieldShuttleTab.Sort);
        }

        private void _RequestFieldShuttle(RenameListFieldShuttleTab tab)
        {
            FieldShuttleRequested?.Invoke(this, tab);
        }

        private bool _CanApplyRelevantColumns()
        {
            return !IsBusy && _appliedFilters is not null && _appliedFilters.Count > 0;
        }

        /// <summary>
        /// Gets whether Add/Replace by applied filters can run (non-empty applied chain, not busy).
        /// </summary>
        internal bool CanApplyRelevantColumns => _CanApplyRelevantColumns();

        private void _NotifyRelevantColumnsCommandsChanged()
        {
            AddRelevantColumnsCommand.NotifyCanExecuteChanged();
            ReplaceWithRelevantColumnsCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Collects field keys for the entire applied stack (disabled steps included).
        /// </summary>
        internal IReadOnlyList<RenameListFieldKey> CollectRelevantFieldKeys()
        {
            if (_appliedFilters is null || _appliedFilters.Count == 0)
            {
                return [];
            }

            var filters = _appliedFilters.Steps.Select(step => step.Filter);
            return FilterRelevantRenameListColumns.Collect(filters);
        }

        /// <summary>
        /// Appends catalog-default-width columns for keys not already in <paramref name="keyToIsPresent"/>.
        /// </summary>
        /// <returns><see langword="true"/> when at least one column was appended.</returns>
        private static bool _AppendMissingRelevantColumns(
            List<RenameListVisibleColumn> columns,
            IReadOnlyList<RenameListFieldKey> relevantKeys,
            HashSet<RenameListFieldKey> keyToIsPresent
        )
        {
            var addedAny = false;
            foreach (var key in relevantKeys)
            {
                if (!keyToIsPresent.Add(key))
                {
                    continue;
                }

                columns.Add(new RenameListVisibleColumn(key));
                addedAny = true;
            }

            return addedAny;
        }

        /// <summary>
        /// Restores visible columns from session data.
        /// </summary>
        /// <param name="columns">
        /// Saved columns in grid order, or <see langword="null"/> for MFR7 defaults.
        /// </param>
        internal void ApplyVisibleColumns(IReadOnlyList<RenameListVisibleColumn>? columns)
        {
            if (columns is null)
            {
                _visibleColumns = [.. _NormalizeColumnsIfAbMode(RenameListVisibleColumn.CreateDefaults())];
                _NotifyVisibleAndProjectedColumnsChanged();
                return;
            }

            var seenKeys = new HashSet<RenameListFieldKey>();
            var validColumns = new List<RenameListVisibleColumn>();
            foreach (var column in columns)
            {
                if (!RenameListFieldCatalog.TryGetField(column.Key, out _) || !seenKeys.Add(column.Key))
                {
                    continue;
                }

                validColumns.Add(column);
            }

            if (validColumns.Count == 0)
            {
                _visibleColumns = [.. _NormalizeColumnsIfAbMode(RenameListVisibleColumn.CreateDefaults())];
                _NotifyVisibleAndProjectedColumnsChanged();
                return;
            }

            SetVisibleColumns(validColumns);
        }

        /// <summary>
        /// Restores visible columns from persisted column specs (session or preset).
        /// </summary>
        /// <param name="columns">
        /// Saved columns in grid order, or <see langword="null"/> for MFR7 defaults.
        /// </param>
        internal void ApplyVisibleColumnSpecs(IReadOnlyList<RenameListVisibleColumnSpec>? columns)
        {
            if (columns is null)
            {
                ApplyVisibleColumns(null);
                return;
            }

            var visibleColumns = columns.Select(column => new RenameListVisibleColumn(
                column.Key,
                column.Width ?? RenameListVisibleColumn.UseCatalogDefaultWidth
            ));

            ApplyVisibleColumns([.. visibleColumns]);
        }

        /// <summary>
        /// Captures the current visible columns as persisted specs (session or preset).
        /// </summary>
        /// <returns>Column specs in grid order.</returns>
        internal IReadOnlyList<RenameListVisibleColumnSpec> CaptureVisibleColumnSpecs()
        {
            return
            [
                .. _visibleColumns.Select(column => new RenameListVisibleColumnSpec(
                    column.Key,
                    column.Width == RenameListVisibleColumn.UseCatalogDefaultWidth ? null : column.Width
                )),
            ];
        }

        /// <summary>
        /// Updates the pixel width for one visible column after a grid resize.
        /// </summary>
        /// <param name="key">Field key for the resized column.</param>
        /// <param name="width">New width in pixels.</param>
        /// <remarks>
        /// <para>
        /// Does not raise <see cref="VisibleColumns"/> change notifications to avoid rebuilding columns
        /// mid-resize. No-op when <paramref name="key"/> is not in the persisted visible list (e.g. derived
        /// Preview-side companions while A/B Mode is on).
        /// </para>
        /// </remarks>
        internal void UpdateVisibleColumnWidth(RenameListFieldKey key, int width)
        {
            var index = _visibleColumns.FindIndex(column => column.Key == key);
            if (index < 0)
            {
                return;
            }

            var column = _visibleColumns[index];
            if (column.Width == width)
            {
                return;
            }

            var updated = _visibleColumns.ToList();
            updated[index] = column with { Width = width };
            _visibleColumns = updated;
        }

        partial void OnIsAbModeEnabledChanged(bool value)
        {
            if (value)
            {
                _NormalizeVisibleColumnsForAbMode();
            }

            OnPropertyChanged(nameof(ProjectedColumns));
        }

        partial void OnAbSideChanged(string value)
        {
            var normalized = RenameListPrefs.NormalizeAbSide(value);
            if (!string.Equals(normalized, value, StringComparison.Ordinal))
            {
                AbSide = normalized;
                return;
            }

            OnPropertyChanged(nameof(ProjectedColumns));
        }

        /// <summary>
        /// Rewrites persisted <see cref="_visibleColumns"/> to originals-only when A/B Mode turns on.
        /// </summary>
        private void _NormalizeVisibleColumnsForAbMode()
        {
            var normalized = RenameListVisibleColumn.NormalizeToOriginals(_visibleColumns);
            if (normalized.SequenceEqual(_visibleColumns))
            {
                return;
            }

            _visibleColumns = [.. normalized];
            OnPropertyChanged(nameof(VisibleColumns));
        }

        /// <summary>
        /// Returns <paramref name="columns"/> unchanged when A/B Mode is off; otherwise originals-only.
        /// </summary>
        private IReadOnlyList<RenameListVisibleColumn> _NormalizeColumnsIfAbMode(
            IReadOnlyList<RenameListVisibleColumn> columns
        )
        {
            if (!IsAbModeEnabled)
            {
                return columns;
            }

            return RenameListVisibleColumn.NormalizeToOriginals(columns);
        }

        /// <summary>
        /// Chain-relevant keys for Add/Replace; originals-only when A/B Mode is on so preview companions
        /// are not treated as missing columns.
        /// </summary>
        private IReadOnlyList<RenameListFieldKey> _RelevantKeysForColumnApply()
        {
            var relevantKeys = CollectRelevantFieldKeys();
            if (!IsAbModeEnabled)
            {
                return relevantKeys;
            }

            return _ToOriginalKeysFirstSeen(relevantKeys);
        }

        /// <summary>
        /// Maps a Preview-side projected key sequence to the stored originals-only order (first-seen).
        /// </summary>
        private IReadOnlyList<RenameListFieldKey> _MapReorderKeysToStoredOriginals(
            IReadOnlyList<RenameListFieldKey> orderedKeys
        )
        {
            var isPreviewSideProjection =
                IsAbModeEnabled && string.Equals(AbSide, RenameListPrefs.AbSidePreview, StringComparison.Ordinal);
            if (!isPreviewSideProjection)
            {
                return orderedKeys;
            }

            return _ToOriginalKeysFirstSeen(orderedKeys);
        }

        /// <summary>
        /// Maps each key to its original form and drops later duplicates, preserving first-seen order.
        /// </summary>
        private static List<RenameListFieldKey> _ToOriginalKeysFirstSeen(IReadOnlyList<RenameListFieldKey> keys)
        {
            var mappedKeys = new List<RenameListFieldKey>();
            var keyToIsSeen = new HashSet<RenameListFieldKey>();
            foreach (var key in keys)
            {
                var originalKey = key.IsPreview ? RenameListFieldKey.Original(key.GroupId, key.PropertyKey) : key;
                if (!keyToIsSeen.Add(originalKey))
                {
                    continue;
                }

                mappedKeys.Add(originalKey);
            }

            return mappedKeys;
        }

        /// <summary>
        /// Builds Preview-side projection: each original followed by a catalog-default-width preview companion
        /// when the field supports preview.
        /// </summary>
        private static List<RenameListVisibleColumn> _DerivePreviewSideColumns(List<RenameListVisibleColumn> originals)
        {
            var projected = new List<RenameListVisibleColumn>(capacity: originals.Count * 2);
            foreach (var column in originals)
            {
                projected.Add(column);
                if (!RenameListFieldCatalog.TryGetField(column.Key, out var field) || !field.SupportsPreview)
                {
                    continue;
                }

                projected.Add(new RenameListVisibleColumn(field.PreviewKey));
            }

            return projected;
        }

        private void _NotifyVisibleAndProjectedColumnsChanged()
        {
            OnPropertyChanged(nameof(VisibleColumns));
            OnPropertyChanged(nameof(ProjectedColumns));
        }
    }

    /// <summary>
    /// Initial tab for the unified Rename List field shuttle dialog.
    /// </summary>
    public enum RenameListFieldShuttleTab
    {
        /// <summary>
        /// Visible column selection and ordering.
        /// </summary>
        Columns = 0,

        /// <summary>
        /// Auto-Sort key selection and ordering.
        /// </summary>
        Sort = 1,
    }
}
