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
        private readonly Dictionary<RenameListFieldKey, int> _rememberedColumnWidths = [];

        /// <summary>
        /// When true, <see cref="VisibleColumns"/> stay originals-only and <see cref="ProjectedColumns"/>
        /// follows <see cref="AbSide"/>.
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SetAbSideCommand))]
        [NotifyCanExecuteChangedFor(nameof(ToggleAbSideCommand))]
        private bool _isAbModeEnabled;

        /// <summary>
        /// Toolbar A/B side: <see cref="RenameListPrefs.AbSideOriginal"/> or
        /// <see cref="RenameListPrefs.AbSidePreview"/>.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAbSideOriginal))]
        [NotifyPropertyChangedFor(nameof(IsAbSidePreview))]
        private string _abSide = RenameListPrefs.AbSidePreview;

        /// <summary>
        /// Gets whether the A/B toolbar/menu Original side is selected.
        /// </summary>
        public bool IsAbSideOriginal => string.Equals(AbSide, RenameListPrefs.AbSideOriginal, StringComparison.Ordinal);

        /// <summary>
        /// Gets whether the A/B toolbar/menu Preview side is selected.
        /// </summary>
        public bool IsAbSidePreview => string.Equals(AbSide, RenameListPrefs.AbSidePreview, StringComparison.Ordinal);

        /// <summary>
        /// Gets visible grid columns in left-to-right order.
        /// <para>
        /// While A/B Mode is on, this list is originals-only (persisted layout). Use
        /// <see cref="ProjectedColumns"/> for on-screen / export column order.
        /// </para>
        /// </summary>
        public IReadOnlyList<RenameListVisibleColumn> VisibleColumns => _visibleColumns;

        /// <summary>
        /// Gets the columns the grid / export should show for the current Before/After Mode and side.
        /// <para>
        /// Mode off: same as <see cref="VisibleColumns"/>. Before: originals only. After: same fields in the
        /// same order, using each field's preview key when <see cref="RenameListField.SupportsPreview"/>
        /// (same column count; widths follow the stored originals).
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

                return _DeriveAfterSideColumns(_visibleColumns);
            }
        }

        /// <summary>
        /// Raised when the view should open the unified field shuttle dialog.
        /// </summary>
        public event EventHandler<RenameListFieldShuttleTab>? FieldShuttleRequested;

        /// <summary>
        /// Appends Rename List columns inferred from the entire Filter Chain (missing keys only).
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

            var relevantKeys = CollectRelevantFieldKeysForApply();
            var keyToIsVisible = _visibleColumns.Select(column => column.Key).ToHashSet();
            var merged = _visibleColumns.ToList();
            if (!_AppendMissingRelevantColumns(merged, relevantKeys, keyToIsVisible))
            {
                return;
            }

            await _ApplyVisibleColumnsWithHydrateAsync(
                    _ApplyRememberedWidthsIfEnabled(_NormalizeColumnsIfAbMode(merged))
                )
                .ConfigureAwait(true);
        }

        /// <summary>
        /// Replaces visible columns with catalog defaults, then appends remaining chain-relevant keys.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When the chain maps to nothing, applies defaults only. Hydrates metadata like field-shuttle apply.
        /// While A/B Mode is on, normalizes to originals-only at this call site (defaults include a preview key).
        /// Widths for keys that were already visible are preserved.
        /// </para>
        /// </remarks>
        [RelayCommand(CanExecute = nameof(_CanApplyRelevantColumns))]
        public async Task ReplaceWithRelevantColumnsAsync()
        {
            if (!_CanApplyRelevantColumns())
            {
                return;
            }

            var relevantKeys = CollectRelevantFieldKeysForApply();
            await _ApplyVisibleColumnsWithHydrateAsync(_BuildDefaultsThenRelevantColumns(relevantKeys))
                .ConfigureAwait(true);
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
        /// <param name="key">Field key to hide (After-side preview keys map to the stored original).</param>
        /// <remarks>
        /// <para>No-op when the key is absent or hiding would leave zero columns.</para>
        /// </remarks>
        public void HideColumn(RenameListFieldKey key)
        {
            if (_visibleColumns.Count <= 1)
            {
                return;
            }

            var storedKey = _StoredColumnKey(key);
            var index = _visibleColumns.FindIndex(column => column.Key == storedKey);
            if (index < 0)
            {
                return;
            }

            var columns = _visibleColumns.ToList();
            columns.RemoveAt(index);
            SetVisibleColumns(columns);
        }

        /// <summary>
        /// Toggles Before/After Mode (originals-only layout + Before/After side control).
        /// <para>
        /// Enabling normalizes persisted columns to originals-only. When the remembered side is After,
        /// hydrates metadata for the After projection before the grid sticks (same path as side flip).
        /// Disabling inserts a preview companion after each original that supports preview.
        /// </para>
        /// </summary>
        [RelayCommand]
        public void ToggleAbMode()
        {
            if (IsAbModeEnabled)
            {
                IsAbModeEnabled = false;
                if (IsBusy)
                {
                    return;
                }

                var requirement = _CombinedMetadataRequirement(_visibleColumns, _sortKeys);
                if (!_NeedsHydrate(requirement))
                {
                    return;
                }

                _ = _HydrateForColumnsAndSortAsync(_visibleColumns, _sortKeys);
                return;
            }

            IsAbModeEnabled = true;
            if (!IsAbSidePreview || IsBusy)
            {
                return;
            }

            var projected = _DeriveAfterSideColumns(_visibleColumns);
            var requirementOn = _CombinedMetadataRequirement(projected, _sortKeys);
            if (!_NeedsHydrate(requirementOn))
            {
                return;
            }

            _ = _HydrateThenSetAbSideAsync(AbSide, projected);
        }

        /// <summary>
        /// Toggles the Before/After toolbar side (original values ↔ preview values for the same fields).
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanSetAbSide))]
        public void ToggleAbSide()
        {
            SetAbSide(IsAbSidePreview ? RenameListPrefs.AbSideOriginal : RenameListPrefs.AbSidePreview);
        }

        /// <summary>
        /// Sets the Before/After toolbar side (Before = originals, After = preview values).
        /// <para>
        /// When Before/After Mode is on and the side becomes After, hydrates metadata for
        /// <see cref="ProjectedColumns"/> before the side sticks (same path as column apply).
        /// Side is remembered while the mode is off so re-enabling restores it.
        /// Menu radios and Ctrl+[ / Ctrl+] use <see cref="SetAbSideCommand"/>, which is disabled
        /// while the mode is off; direct calls still update the remembered side.
        /// </para>
        /// </summary>
        /// <param name="side">
        /// <see cref="RenameListPrefs.AbSideOriginal"/> or <see cref="RenameListPrefs.AbSidePreview"/>.
        /// </param>
        [RelayCommand(CanExecute = nameof(_CanSetAbSide))]
        public void SetAbSide(string side)
        {
            var normalized = RenameListPrefs.NormalizeAbSide(side);
            if (string.Equals(AbSide, normalized, StringComparison.Ordinal))
            {
                return;
            }

            var flippingToPreview =
                IsAbModeEnabled && string.Equals(normalized, RenameListPrefs.AbSidePreview, StringComparison.Ordinal);
            if (!flippingToPreview)
            {
                AbSide = normalized;
                return;
            }

            if (IsBusy)
            {
                return;
            }

            var projected = _DeriveAfterSideColumns(_visibleColumns);
            var requirement = _CombinedMetadataRequirement(projected, _sortKeys);
            if (_NeedsHydrate(requirement))
            {
                _ = _HydrateThenSetAbSideAsync(normalized, projected);
                return;
            }

            AbSide = normalized;
        }

        private bool _CanSetAbSide()
        {
            return IsAbModeEnabled;
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
            return !IsBusy && _filterChain is not null && _filterChain.Count > 0;
        }

        /// <summary>
        /// Gets whether Add/Set columns from filters can run (non-empty applied chain, not busy).
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
            if (_filterChain is null || _filterChain.Count == 0)
            {
                return [];
            }

            var filters = _filterChain.Steps.Select(step => step.Filter);
            return FilterRelevantRenameListColumns.Collect(filters);
        }

        /// <summary>
        /// Appends catalog-default-width columns for keys not already in <paramref name="keyToIsPresent"/>.
        /// <para>Callers that honor Options remembering apply remembered widths after this append.</para>
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
        /// Builds catalog defaults, appends missing relevant keys, A/B-normalizes, and keeps widths.
        /// </summary>
        private List<RenameListVisibleColumn> _BuildDefaultsThenRelevantColumns(
            IReadOnlyList<RenameListFieldKey> relevantKeys
        )
        {
            var columns = RenameListVisibleColumn.CreateDefaults().ToList();
            var keyToIsPresent = columns.Select(column => column.Key).ToHashSet();
            _AppendMissingRelevantColumns(columns, relevantKeys, keyToIsPresent);
            var normalized = _NormalizeColumnsIfAbMode(columns);
            var preserved = RenameListVisibleColumn.WithPreservedWidths(normalized, _visibleColumns);
            return [.. _ApplyRememberedWidthsIfEnabled(preserved)];
        }

        /// <summary>
        /// Gets the live remembered absolute widths (session capture and field-shuttle input).
        /// </summary>
        internal IReadOnlyDictionary<RenameListFieldKey, int> RememberedColumnWidths => _rememberedColumnWidths;

        /// <summary>
        /// Applies remembered widths to catalog-default columns when Options remembering is on.
        /// </summary>
        private IReadOnlyList<RenameListVisibleColumn> _ApplyRememberedWidthsIfEnabled(
            IReadOnlyList<RenameListVisibleColumn> columns
        )
        {
            if (!ConfigStore.Options.RememberColumnWidths || _rememberedColumnWidths.Count == 0)
            {
                return columns;
            }

            return RenameListVisibleColumn.WithRememberedWidths(columns, _rememberedColumnWidths);
        }

        /// <summary>
        /// Fills catalog-default entries on the current visible list from the remembered map (session restore).
        /// </summary>
        private void _ApplyRememberedWidthsToCurrentVisibleColumns()
        {
            var applied = _ApplyRememberedWidthsIfEnabled(_visibleColumns);
            if (ReferenceEquals(applied, _visibleColumns) || applied.SequenceEqual(_visibleColumns))
            {
                return;
            }

            _visibleColumns = [.. applied];
            _NotifyVisibleAndProjectedColumnsChanged();
        }

        /// <summary>
        /// Loads remembered widths from session specs (unknown keys and non-positive widths skipped).
        /// </summary>
        private void _ApplyRememberedColumnWidthSpecs(IReadOnlyList<RenameListVisibleColumnSpec>? specs)
        {
            _rememberedColumnWidths.Clear();
            if (specs is null)
            {
                return;
            }

            foreach (var spec in specs)
            {
                if (spec.Width is not > 0 || !RenameListFieldCatalog.TryGetField(spec.Key, out _))
                {
                    continue;
                }

                _rememberedColumnWidths[spec.Key] = spec.Width.Value;
            }
        }

        /// <summary>
        /// Captures remembered widths for session save; seeds from visible absolute widths when remembering.
        /// </summary>
        private List<RenameListVisibleColumnSpec> _CaptureRememberedColumnWidthSpecs()
        {
            if (ConfigStore.Options.RememberColumnWidths)
            {
                foreach (var column in _visibleColumns)
                {
                    if (column.Width > 0)
                    {
                        _rememberedColumnWidths[column.Key] = column.Width;
                    }
                }
            }

            return
            [
                .. _rememberedColumnWidths
                    .Where(pair => pair.Value > 0)
                    .Select(pair => new RenameListVisibleColumnSpec(pair.Key, pair.Value)),
            ];
        }

        /// <summary>
        /// Upserts a remembered width when Options remembering is on.
        /// </summary>
        private void _RememberColumnWidthIfEnabled(RenameListFieldKey key, int width)
        {
            if (!ConfigStore.Options.RememberColumnWidths || width <= 0)
            {
                return;
            }

            _rememberedColumnWidths[key] = width;
        }

        /// <summary>
        /// Restores visible columns from session or preset data.
        /// <para>
        /// Does not overlay <see cref="RememberedColumnWidths"/> — session restore does that in
        /// <see cref="ApplySessionSection"/> so preset loads keep catalog defaults when widths are omitted.
        /// </para>
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
        /// mid-resize. While Before/After Mode After side shows a preview key, width updates apply to the
        /// matching stored original. No-op when the key is not in the persisted visible list.
        /// </para>
        /// </remarks>
        internal void UpdateVisibleColumnWidth(RenameListFieldKey key, int width)
        {
            var storedKey = _StoredColumnKey(key);
            var index = _visibleColumns.FindIndex(column => column.Key == storedKey);
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
            _RememberColumnWidthIfEnabled(storedKey, width);
        }

        /// <summary>
        /// When true, Before/After Mode column normalize/expand is skipped (caller replaces columns).
        /// </summary>
        private bool _suppressAbModeColumnRewrite;

        /// <summary>
        /// Turns Before/After Mode off without inserting preview companions.
        /// <para>
        /// Use when the caller immediately replaces <see cref="VisibleColumns"/> (undo prepare, shuttle OK).
        /// Toggle-off still expands companions via <see cref="IsAbModeEnabled"/>.
        /// </para>
        /// </summary>
        private void _DisableAbModeWithoutCompanionExpand()
        {
            if (!IsAbModeEnabled)
            {
                return;
            }

            _suppressAbModeColumnRewrite = true;
            try
            {
                IsAbModeEnabled = false;
            }
            finally
            {
                _suppressAbModeColumnRewrite = false;
            }
        }

        partial void OnIsAbModeEnabledChanged(bool value)
        {
            if (_suppressAbModeColumnRewrite)
            {
                OnPropertyChanged(nameof(ProjectedColumns));
                return;
            }

            if (value)
            {
                _NormalizeVisibleColumnsForAbMode();
            }
            else
            {
                _ExpandVisibleColumnsWithPreviewCompanions();
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
        /// Inserts preview companions after each original when A/B Mode turns off.
        /// </summary>
        private void _ExpandVisibleColumnsWithPreviewCompanions()
        {
            var expanded = RenameListVisibleColumn.WithPreviewCompanions(_visibleColumns);
            expanded = _ApplyRememberedWidthsIfEnabled(expanded);
            if (expanded.SequenceEqual(_visibleColumns))
            {
                return;
            }

            _visibleColumns = [.. expanded];
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
        /// Chain-relevant keys for Add/Replace and the field shuttle; originals-only when A/B Mode is on so
        /// preview companions are not treated as missing columns.
        /// </summary>
        /// <returns>Relevant field keys, normalized for the current A/B Mode.</returns>
        internal IReadOnlyList<RenameListFieldKey> CollectRelevantFieldKeysForApply()
        {
            var relevantKeys = CollectRelevantFieldKeys();
            if (!IsAbModeEnabled)
            {
                return relevantKeys;
            }

            return RenameListVisibleColumn.ToOriginalKeysFirstSeen(relevantKeys);
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

            return RenameListVisibleColumn.ToOriginalKeysFirstSeen(orderedKeys);
        }

        /// <summary>
        /// Maps a grid field key to the persisted originals-only key (After-side preview → original).
        /// </summary>
        private RenameListFieldKey _StoredColumnKey(RenameListFieldKey key)
        {
            return IsAbModeEnabled ? key.AsOriginal() : key;
        }

        /// <summary>
        /// Builds After-side projection: same fields/order/widths as <paramref name="originals"/>, swapping to
        /// the preview key when the field supports preview.
        /// </summary>
        private static List<RenameListVisibleColumn> _DeriveAfterSideColumns(List<RenameListVisibleColumn> originals)
        {
            var projected = new List<RenameListVisibleColumn>(capacity: originals.Count);
            foreach (var column in originals)
            {
                if (!RenameListFieldCatalog.TryGetField(column.Key, out var field) || !field.SupportsPreview)
                {
                    projected.Add(column);
                    continue;
                }

                projected.Add(new RenameListVisibleColumn(field.PreviewKey, column.Width));
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
