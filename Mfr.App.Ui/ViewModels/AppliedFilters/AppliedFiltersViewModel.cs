using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.App.Ui.Services.Help;
using Mfr.App.Ui.ViewModels.Presets;
using Mfr.Engine.Presets;
using Mfr.Filters;
using Mfr.Models.Filters;
using Mfr.Models.RenameList;
using Mfr.Utils;

namespace Mfr.App.Ui.ViewModels.AppliedFilters
{
    /// <summary>
    /// Applied Filters pane: ordered filter stack edited before preview.
    /// </summary>
    public sealed partial class AppliedFiltersViewModel : ViewModelBase
    {
        private readonly FilterDefaultsStore _filterDefaults;
        private readonly FilterHelpHost _filterHelp;
        private readonly List<AppliedFilterStepViewModel> _selectedSteps = [];
        private Func<IReadOnlyList<RenameListVisibleColumnSpec>>? _captureRenameListColumns;
        private Action<IReadOnlyList<RenameListVisibleColumnSpec>>? _applyRenameListColumns;
        private int _chainChangedBatchDepth;
        private bool _chainChangedQueued;

        /// <summary>
        /// Initializes an empty applied-filter list.
        /// </summary>
        /// <param name="filterDefaults">
        /// Per-type add defaults store. When null, uses an empty store that does not read AppData
        /// (production passes <see cref="FilterDefaultsStore.OpenDefault"/>).
        /// </param>
        /// <param name="presetManager">
        /// Named presets store. When null, uses an empty manager that does not read AppData
        /// (production passes <see cref="PresetManager.OpenDefault"/>).
        /// </param>
        /// <param name="filterHelp">
        /// Opens per-filter Help HTML. When null, uses a host with default app <c>help/</c> roots.
        /// </param>
        public AppliedFiltersViewModel(
            FilterDefaultsStore? filterDefaults = null,
            PresetManager? presetManager = null,
            FilterHelpHost? filterHelp = null
        )
        {
            _filterDefaults = filterDefaults ?? FilterDefaultsStore.CreateEmpty();
            PresetManager = presetManager ?? PresetManager.CreateEmpty();
            _filterHelp = filterHelp ?? new FilterHelpHost();
            Steps = [];
            Steps.CollectionChanged += _OnStepsCollectionChanged;
        }

        /// <summary>
        /// Gets the named presets manager for this pane.
        /// </summary>
        public PresetManager PresetManager { get; }

        /// <summary>
        /// Gets the last preset loaded or saved into this pane, or <see langword="null"/> when none.
        /// </summary>
        public FilterPreset? LastLoaded { get; private set; }

        /// <summary>
        /// Gets the most recent high-signal Applied Filters status-bar message (preset load/save).
        /// </summary>
        [ObservableProperty]
        private StyledTextDisplay _lastStatusMessage = StyledTextDisplay.Empty;

        /// <summary>
        /// Clears in-memory per-type add defaults after Reset Configuration deletes the file.
        /// </summary>
        internal void ClearFilterDefaultsCache()
        {
            _filterDefaults.Clear();
        }

        /// <summary>
        /// Wires Rename List column capture/apply from the composition root (main window).
        /// <para>
        /// When unset, <see cref="CaptureRenameListColumns"/> returns an empty list and
        /// <see cref="LoadPreset"/> skips applying columns (pane-only tests).
        /// </para>
        /// </summary>
        /// <param name="capture">Returns the current Rename List visible column specs.</param>
        /// <param name="apply">Applies non-null column specs to the Rename List.</param>
        internal void SetRenameListColumnSource(
            Func<IReadOnlyList<RenameListVisibleColumnSpec>> capture,
            Action<IReadOnlyList<RenameListVisibleColumnSpec>> apply
        )
        {
            ArgumentNullException.ThrowIfNull(capture);
            ArgumentNullException.ThrowIfNull(apply);

            _captureRenameListColumns = capture;
            _applyRenameListColumns = apply;
        }

        /// <summary>
        /// Captures current Rename List visible columns for Save Preset.
        /// </summary>
        /// <returns>
        /// Current columns when a source is wired; otherwise an empty list.
        /// </returns>
        internal IReadOnlyList<RenameListVisibleColumnSpec> CaptureRenameListColumns()
        {
            return _captureRenameListColumns?.Invoke() ?? [];
        }

        /// <summary>
        /// Records the last loaded or saved preset (used to prefill Save Preset).
        /// </summary>
        /// <param name="preset">Preset that was just loaded or saved, or <see langword="null"/> to clear.</param>
        internal void SetLastLoaded(FilterPreset? preset)
        {
            LastLoaded = preset;
            OnPropertyChanged(nameof(LastLoaded));
        }

        /// <summary>
        /// Upserts a named preset from the current chain.
        /// <para>
        /// Keeps <see cref="FilterPreset.Id"/> when overwriting an existing name; otherwise assigns a new
        /// id. Sets last-loaded to the saved preset. Caller is responsible for overwrite confirmation when
        /// the name already exists.
        /// </para>
        /// </summary>
        /// <param name="name">Preset display name (trimmed; blank is rejected).</param>
        /// <param name="description">Optional description; blank becomes <see langword="null"/>.</param>
        /// <param name="visibleColumns">
        /// Columns to store when the Save Rename List columns checkbox is checked; otherwise
        /// <see langword="null"/>.
        /// </param>
        /// <returns>The saved preset, or <see langword="null"/> when <paramref name="name"/> is blank.</returns>
        public FilterPreset? SavePreset(
            string name,
            string? description,
            IReadOnlyList<RenameListVisibleColumnSpec>? visibleColumns
        )
        {
            ArgumentNullException.ThrowIfNull(name);

            var trimmedName = name.Trim();
            if (trimmedName.Length == 0)
            {
                return null;
            }

            var id = PresetManager.NameToPreset.TryGetValue(trimmedName, out var existing)
                ? existing.Id
                : Guid.NewGuid();
            var saved = new FilterPreset
            {
                Id = id,
                Name = trimmedName,
                Description = description.TrimmedOrNull(),
                Chain = ToChain(),
                VisibleColumns = visibleColumns,
            };
            PresetManager.Upsert(saved);
            PresetManager.SavePresets();
            SetLastLoaded(saved);
            LastStatusMessage = StatusBarText.Neutral($"Saved preset \"{trimmedName}\".");
            return saved;
        }

        /// <summary>
        /// Imports selected sample presets without replacing presets that have the same exact name.
        /// </summary>
        /// <param name="names">Exact sample names to import.</param>
        /// <returns>The number of presets added and skipped because their names already exist.</returns>
        public (int AddedCount, int SkippedCount) ImportSamplePresets(IReadOnlyList<string> names)
        {
            ArgumentNullException.ThrowIfNull(names);

            var nameToSample = SamplePresetCatalog.Presets.ToDictionary(preset => preset.Name, StringComparer.Ordinal);
            var addedCount = 0;
            var skippedCount = 0;

            foreach (var name in names)
            {
                if (!nameToSample.TryGetValue(name, out var sample))
                {
                    continue;
                }

                if (PresetManager.NameToPreset.ContainsKey(name))
                {
                    skippedCount++;
                    continue;
                }

                PresetManager.Upsert(SamplePresetCatalog.CreateIndependentCopy(sample));
                addedCount++;
            }

            if (addedCount > 0)
            {
                PresetManager.SavePresets();
            }

            return (addedCount, skippedCount);
        }

        /// <summary>
        /// Gets whether loading a preset should confirm before replacing the current Applied Filters chain.
        /// </summary>
        /// <param name="shouldConfirm">
        /// Whether policy requires a replace-on-load confirm (typically from
        /// <see cref="Models.Config.ConfirmationPolicy"/>).
        /// </param>
        /// <returns>
        /// <see langword="true"/> when confirmation is required and the stack is non-empty.
        /// </returns>
        public bool NeedsConfirmReplaceOnLoad(bool shouldConfirm)
        {
            return shouldConfirm && Steps.Count > 0;
        }

        /// <summary>
        /// Replaces the Applied Filters stack from <paramref name="preset"/> and records it as last-loaded.
        /// <para>
        /// When <see cref="FilterPreset.VisibleColumns"/> is present and a Rename List column source is
        /// wired, applies those columns. Caller owns confirm-replace / corrupt-open UI. Empty chains are
        /// allowed. Omitted/<see langword="null"/> columns leave the Rename List unchanged.
        /// Sticky status is set only after column apply so a throw stays dialog-only.
        /// </para>
        /// </summary>
        /// <param name="preset">Preset to load (uses its <see cref="FilterPreset.Chain"/>).</param>
        public void LoadPreset(FilterPreset preset)
        {
            ArgumentNullException.ThrowIfNull(preset);

            ReplaceFromChain(preset.Chain);
            SetLastLoaded(preset);
            if (preset.VisibleColumns is not null)
            {
                _applyRenameListColumns?.Invoke(preset.VisibleColumns);
            }

            LastStatusMessage = StatusBarText.Neutral($"Loaded preset \"{preset.Name}\".");
        }

        /// <summary>
        /// Deletes a named preset from the manager and persists.
        /// <para>
        /// When the deleted preset was last-loaded, clears last-loaded.
        /// </para>
        /// </summary>
        /// <param name="name">Exact preset name key in <see cref="PresetManager.NameToPreset"/>.</param>
        /// <returns><see langword="true"/> when a preset was removed; otherwise <see langword="false"/>.</returns>
        public bool DeletePreset(string name)
        {
            ArgumentNullException.ThrowIfNull(name);

            return DeletePresets([name]) > 0;
        }

        /// <summary>
        /// Deletes named presets from the manager and persists once.
        /// <para>
        /// When any deleted preset was last-loaded, clears last-loaded.
        /// </para>
        /// </summary>
        /// <param name="names">Exact preset name keys to remove.</param>
        /// <returns>Number of presets removed.</returns>
        public int DeletePresets(IReadOnlyList<string> names)
        {
            ArgumentNullException.ThrowIfNull(names);

            var removedCount = 0;
            var clearedLastLoaded = false;
            foreach (var name in names.Distinct(StringComparer.Ordinal))
            {
                if (!PresetManager.Remove(name))
                {
                    continue;
                }

                removedCount++;
                if (
                    !clearedLastLoaded
                    && LastLoaded is not null
                    && string.Equals(LastLoaded.Name, name, StringComparison.Ordinal)
                )
                {
                    SetLastLoaded(null);
                    clearedLastLoaded = true;
                }
            }

            if (removedCount > 0)
            {
                PresetManager.SavePresets();
            }

            return removedCount;
        }

        /// <summary>
        /// Moves selected presets one step toward <paramref name="offset"/> and persists.
        /// </summary>
        /// <param name="selectedNames">Exact names of presets to move.</param>
        /// <param name="offset">Direction (-1 up, +1 down).</param>
        /// <returns><see langword="true"/> when at least one preset changed position.</returns>
        public bool TryMovePresetsTowardNeighbor(IReadOnlyCollection<string> selectedNames, int offset)
        {
            ArgumentNullException.ThrowIfNull(selectedNames);

            if (!PresetManager.TryMoveSelectedTowardNeighbor(selectedNames, offset))
            {
                return false;
            }

            PresetManager.SavePresets();
            return true;
        }

        /// <summary>
        /// Moves presets at <paramref name="sourceIndices"/> to <paramref name="targetIndex"/> and persists.
        /// </summary>
        /// <param name="sourceIndices">Indices of presets to move.</param>
        /// <param name="targetIndex">Destination index before the move.</param>
        /// <param name="newIndices">Indices of the moved presets after a successful move.</param>
        /// <returns><see langword="true"/> when the list order changed.</returns>
        public bool TryMovePresetsTo(
            IReadOnlyList<int> sourceIndices,
            int targetIndex,
            out IReadOnlyList<int> newIndices
        )
        {
            ArgumentNullException.ThrowIfNull(sourceIndices);

            if (!PresetManager.TryMoveIndicesTo(sourceIndices, targetIndex, out newIndices))
            {
                return false;
            }

            PresetManager.SavePresets();
            return true;
        }

        /// <summary>
        /// Renames a preset in place (same <see cref="FilterPreset.Id"/>, chain, description, columns).
        /// <para>
        /// Blank names are rejected. Exact same name is a no-op. Names already used by another preset
        /// are refused (no overwrite). When the renamed preset was last-loaded, updates last-loaded.
        /// </para>
        /// </summary>
        /// <param name="currentName">Exact current name key.</param>
        /// <param name="newName">Desired display name (trimmed).</param>
        /// <returns>Outcome and the resulting preset when unchanged or renamed.</returns>
        public PresetRenameResult RenamePreset(string currentName, string newName)
        {
            ArgumentNullException.ThrowIfNull(currentName);
            ArgumentNullException.ThrowIfNull(newName);

            var trimmedName = newName.Trim();
            if (trimmedName.Length == 0)
            {
                return new PresetRenameResult(PresetRenameStatus.BlankName, Preset: null);
            }

            if (!PresetManager.NameToPreset.TryGetValue(currentName, out var existing))
            {
                return new PresetRenameResult(PresetRenameStatus.NotFound, Preset: null);
            }

            if (string.Equals(currentName, trimmedName, StringComparison.Ordinal))
            {
                return new PresetRenameResult(PresetRenameStatus.Unchanged, existing);
            }

            if (PresetManager.NameToPreset.ContainsKey(trimmedName))
            {
                return new PresetRenameResult(PresetRenameStatus.NameTaken, Preset: null);
            }

            var renamed = existing with { Name = trimmedName };
            if (!PresetManager.TryRename(currentName, renamed))
            {
                return new PresetRenameResult(PresetRenameStatus.NotFound, Preset: null);
            }

            PresetManager.SavePresets();
            if (LastLoaded is not null && string.Equals(LastLoaded.Name, currentName, StringComparison.Ordinal))
            {
                SetLastLoaded(renamed);
            }

            return new PresetRenameResult(PresetRenameStatus.Success, renamed);
        }

        /// <summary>
        /// Gets applied filter steps in stack order.
        /// </summary>
        public ObservableCollection<AppliedFilterStepViewModel> Steps { get; }

        /// <summary>
        /// Gets the current multi-selection.
        /// </summary>
        public IReadOnlyList<AppliedFilterStepViewModel> SelectedSteps => _selectedSteps;

        /// <summary>
        /// Gets the number of applied filters.
        /// </summary>
        public int Count => Steps.Count;

        /// <summary>
        /// Raised when <see cref="ToChain"/> would change (stack membership, order, enabled, or filter options).
        /// </summary>
        public event EventHandler? ChainChanged;

        /// <summary>
        /// Raised after Filter Options are accepted so hosts can refresh dependent panes.
        /// </summary>
        public event EventHandler? FilterOptionsApplied;

        /// <summary>
        /// Raised after the selected step’s options are saved as the per-type add default.
        /// <para>Payload is the catalog display name (for the confirmation dialog).</para>
        /// </summary>
        public event EventHandler<string>? FilterDefaultSaved;

        /// <summary>
        /// Raised when Help was requested but the HTML file was not found under configured Help roots.
        /// <para>Payload is the expected help file name (e.g. <c>SpaceCharacter.html</c>).</para>
        /// </summary>
        public event EventHandler<string>? FilterHelpMissing;

        /// <summary>
        /// Replaces the current multi-selection.
        /// </summary>
        /// <param name="steps">Selected steps in list order.</param>
        public void SetSelectedSteps(IReadOnlyList<AppliedFilterStepViewModel> steps)
        {
            ArgumentNullException.ThrowIfNull(steps);

            _selectedSteps.Clear();
            foreach (var step in steps)
            {
                if (Steps.Contains(step))
                {
                    _selectedSteps.Add(step);
                }
            }

            OnPropertyChanged(nameof(SelectedSteps));
            _NotifySelectionCommandsChanged();
        }

        /// <summary>
        /// Inserts a catalog filter at the current selection (MFR7 insert-before-selected).
        /// </summary>
        /// <param name="entry">Catalog row to add.</param>
        [RelayCommand]
        public void Add(FilterCatalogEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            InsertFromCatalogAt([entry], _GetInsertIndex());
        }

        /// <summary>
        /// Appends a catalog filter from the palette with defaults.
        /// </summary>
        /// <param name="entry">Catalog row to add.</param>
        [RelayCommand]
        public void Append(FilterCatalogEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            InsertFromCatalogAt([entry], Steps.Count);
        }

        /// <summary>
        /// Appends a concrete filter instance and selects it (Edit as Name List and similar).
        /// </summary>
        /// <param name="filter">Fully configured filter to add (not a catalog default).</param>
        /// <param name="preferredDisplayName">
        /// Desired list label; when already taken, appends <c>*</c> until unique (MFR7 Free Names / Edit as Name List).
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="filter"/> is null, or <paramref name="preferredDisplayName"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="preferredDisplayName"/> is whitespace-only.</exception>
        public void AddAndSelect(BaseFilter filter, string preferredDisplayName)
        {
            ArgumentNullException.ThrowIfNull(filter);
            ArgumentNullException.ThrowIfNull(preferredDisplayName);

            var trimmedName = preferredDisplayName.Trim();
            if (trimmedName.Length == 0)
            {
                throw new ArgumentException(
                    "Display name cannot be empty or whitespace.",
                    nameof(preferredDisplayName)
                );
            }

            var displayName = _AppendStarsUntilDisplayNameUnique(trimmedName);
            var step = new AppliedFilterStepViewModel(displayName, filter);
            _WithSingleChainChanged(() => Steps.Add(step));
            SetSelectedSteps([step]);
        }

        /// <summary>
        /// Removes the selected steps from the stack.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_HasSelection))]
        public void RemoveSelected()
        {
            if (_selectedSteps.Count == 0)
            {
                return;
            }

            var indices = _selectedSteps.Select(Steps.IndexOf).Where(index => index >= 0).ToList();
            RemoveStepsAtIndices(indices);
        }

        /// <summary>
        /// Removes every step from the stack.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_HasSteps))]
        public void Clear()
        {
            if (Steps.Count == 0)
            {
                return;
            }

            _DetachAndClearSteps();
            SetSelectedSteps([]);
        }

        /// <summary>
        /// Removes every step that is not selected, keeping the current selection.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanRemoveAllButSelected))]
        public void RemoveAllButSelected()
        {
            if (_selectedSteps.Count == 0 || _selectedSteps.Count >= Steps.Count)
            {
                return;
            }

            var selected = _selectedSteps.ToHashSet();
            var removeIndices = new HashSet<int>();
            for (var index = 0; index < Steps.Count; index++)
            {
                if (!selected.Contains(Steps[index]))
                {
                    removeIndices.Add(index);
                }
            }

            _WithSingleChainChanged(() => _RemoveAtIndices(removeIndices));

            SetSelectedSteps([.. Steps.Where(selected.Contains)]);
        }

        /// <summary>
        /// Enables every applied filter step (MFR7 Check All Filters).
        /// </summary>
        [RelayCommand(CanExecute = nameof(_HasSteps))]
        public void CheckAllFilters()
        {
            _SetAllEnabled(enabled: true);
        }

        /// <summary>
        /// Disables every applied filter step (MFR7 Uncheck All Filters).
        /// </summary>
        [RelayCommand(CanExecute = nameof(_HasSteps))]
        public void UncheckAllFilters()
        {
            _SetAllEnabled(enabled: false);
        }

        /// <summary>
        /// Toggles <see cref="AppliedFilterStepViewModel.Enabled"/> on every step (MFR7 Invert Check).
        /// </summary>
        [RelayCommand(CanExecute = nameof(_HasSteps))]
        public void InvertCheck()
        {
            if (Steps.Count == 0)
            {
                return;
            }

            _WithSingleChainChanged(() =>
            {
                foreach (var step in Steps)
                {
                    step.Enabled = !step.Enabled;
                }
            });
        }

        /// <summary>
        /// Moves the selected steps one position up.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanMoveSelectedUp))]
        public void MoveSelectedUp()
        {
            _MoveSelected(offset: -1);
        }

        /// <summary>
        /// Moves the selected steps one position down.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanMoveSelectedDown))]
        public void MoveSelectedDown()
        {
            _MoveSelected(offset: 1);
        }

        /// <summary>
        /// Gets whether exactly one step is selected for Filter Options / reset.
        /// </summary>
        public bool CanShowFilterOptions => _HasSingleSelection();

        /// <summary>
        /// Applies Filter Options dialog edits to the selected step.
        /// </summary>
        /// <param name="draft">Accepted dialog state.</param>
        public void ApplyFilterOptions(FilterOptionsDialogViewModel draft)
        {
            ArgumentNullException.ThrowIfNull(draft);

            if (_selectedSteps.Count != 1)
            {
                return;
            }

            var step = _selectedSteps[0];
            if (!string.IsNullOrWhiteSpace(draft.Name))
            {
                step.SetDisplayName(draft.Name.Trim());
            }

            if (step.Filter is StringTargetFilter stringFilter)
            {
                var newTarget = draft.BuildTarget();
                if (newTarget is not null)
                {
                    step.SetFilter(stringFilter with { Target = newTarget, ApplyScope = draft.BuildApplyScope() });
                }
            }

            FilterOptionsApplied?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Restores the sole selected step to catalog defaults without removing it from the list.
        /// <para>
        /// Replaces the filter via <see cref="FilterCatalog.CreateDefault"/> (options, Apply To, and
        /// scope). Keeps <see cref="AppliedFilterStepViewModel.DisplayName"/> and
        /// <see cref="AppliedFilterStepViewModel.Enabled"/>. Requires exactly one selected step (same as
        /// Filter Options / the Filter Configuration pane).
        /// </para>
        /// </summary>
        [RelayCommand(CanExecute = nameof(_HasSingleSelection))]
        public void ResetSelectedToDefaults()
        {
            if (_selectedSteps.Count != 1)
            {
                return;
            }

            var step = _selectedSteps[0];
            var entry = _CatalogEntryFor(step.Filter);
            var defaults = FilterCatalog.CreateDefault(entry);
            if (Equals(step.Filter, defaults))
            {
                return;
            }

            _WithSingleChainChanged(() => step.SetFilter(defaults));
            FilterOptionsApplied?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Saves the sole selected step’s filter options as the add default for that filter type.
        /// <para>
        /// Persists via <see cref="FilterDefaultsStore"/> (options, Apply To, scope). Does not change
        /// the current step. Requires exactly one selected step.
        /// </para>
        /// </summary>
        [RelayCommand(CanExecute = nameof(_HasSingleSelection))]
        public void SaveSelectedAsDefault()
        {
            if (_selectedSteps.Count != 1)
            {
                return;
            }

            var step = _selectedSteps[0];
            var entry = _CatalogEntryFor(step.Filter);
            _filterDefaults.SetDefault(step.Filter);
            FilterDefaultSaved?.Invoke(this, entry.DisplayName);
        }

        /// <summary>
        /// Opens Help HTML for the sole selected filter type (MFR7 Filter Configuration <c>?</c>).
        /// <para>
        /// Requires exactly one selected step (Help file is <c>{Type}.html</c> by convention).
        /// Opens via <see cref="FilterHelpHost"/> when the file exists; otherwise raises
        /// <see cref="FilterHelpMissing"/>.
        /// </para>
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanOpenSelectedFilterHelp))]
        public void OpenSelectedFilterHelp()
        {
            if (!_TryGetSelectedHelpFileName(out var helpFileName))
            {
                return;
            }

            if (_filterHelp.TryOpen(helpFileName, out _))
            {
                return;
            }

            FilterHelpMissing?.Invoke(this, helpFileName);
        }

        /// <summary>
        /// Builds a <see cref="FilterChain"/> matching the current stack.
        /// </summary>
        /// <returns>Enabled flags and filters in list order.</returns>
        public FilterChain ToChain()
        {
            return new FilterChain
            {
                Steps =
                [
                    .. Steps.Select(step => new FilterChainStep(step.Enabled, step.Filter, Name: step.DisplayName)),
                ],
            };
        }

        /// <summary>
        /// Replaces the Applied Filters stack from a preset or session chain.
        /// <para>
        /// Clears existing steps, rebuilds from <paramref name="chain"/>, restores each step’s
        /// <see cref="FilterChainStep.Name"/> when set (otherwise synthesizes a catalog display name),
        /// copies each step’s <c>Enabled</c> flag, raises <see cref="ChainChanged"/> once, and selects
        /// the first step when any exist.
        /// </para>
        /// </summary>
        /// <param name="chain">Source chain (always replaces; never merges).</param>
        public void ReplaceFromChain(FilterChain chain)
        {
            ArgumentNullException.ThrowIfNull(chain);

            if (chain.Steps.Count == 0 && Steps.Count == 0)
            {
                SetSelectedSteps([]);
                return;
            }

            _WithSingleChainChanged(() =>
            {
                _DetachAndClearSteps();
                foreach (var chainStep in chain.Steps)
                {
                    var entry = _CatalogEntryFor(chainStep.Filter);
                    var displayName = _ResolveDisplayName(chainStep.Name, entry);
                    var step = new AppliedFilterStepViewModel(displayName, chainStep.Filter)
                    {
                        Enabled = chainStep.Enabled,
                    };
                    Steps.Add(step);
                }
            });

            if (Steps.Count == 0)
            {
                SetSelectedSteps([]);
                return;
            }

            SetSelectedSteps([Steps[0]]);
        }

        /// <summary>
        /// Unsubscribes step change handlers then clears the stack.
        /// <para>
        /// Required before <c>Steps.Clear()</c>: that raises
        /// <see cref="NotifyCollectionChangedAction.Reset"/> without <c>OldItems</c>, so the
        /// collection-changed handler cannot detach.
        /// </para>
        /// </summary>
        private void _DetachAndClearSteps()
        {
            foreach (var step in Steps)
            {
                step.PropertyChanged -= _OnStepPropertyChanged;
            }

            Steps.Clear();
        }

        private void _OnStepsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
            {
                foreach (AppliedFilterStepViewModel step in e.NewItems)
                {
                    step.PropertyChanged += _OnStepPropertyChanged;
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems is not null)
            {
                foreach (AppliedFilterStepViewModel step in e.OldItems)
                {
                    step.PropertyChanged -= _OnStepPropertyChanged;
                }
            }

            OnPropertyChanged(nameof(Count));
            ClearCommand.NotifyCanExecuteChanged();
            CheckAllFiltersCommand.NotifyCanExecuteChanged();
            UncheckAllFiltersCommand.NotifyCanExecuteChanged();
            InvertCheckCommand.NotifyCanExecuteChanged();
            _NotifySelectionCommandsChanged();
            _RaiseChainChanged();
        }

        private void _OnStepPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (
                e.PropertyName
                is nameof(AppliedFilterStepViewModel.Enabled)
                    or nameof(AppliedFilterStepViewModel.Filter)
            )
            {
                _RaiseChainChanged();
            }
        }

        /// <summary>
        /// Raises <see cref="ChainChanged"/>, or queues a single raise while a batch is open.
        /// </summary>
        private void _RaiseChainChanged()
        {
            if (_chainChangedBatchDepth > 0)
            {
                _chainChangedQueued = true;
                return;
            }

            ChainChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Runs a stack mutation and raises <see cref="ChainChanged"/> once if anything changed.
        /// </summary>
        /// <param name="action">Mutations that may fire multiple <see cref="ObservableCollection{T}.CollectionChanged"/> events.</param>
        private void _WithSingleChainChanged(Action action)
        {
            _chainChangedBatchDepth++;
            try
            {
                action();
            }
            finally
            {
                _chainChangedBatchDepth--;
                if (_chainChangedBatchDepth == 0 && _chainChangedQueued)
                {
                    _chainChangedQueued = false;
                    ChainChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Inserts catalog filters at <paramref name="insertIndex"/> (drag-drop from Available Filters).
        /// </summary>
        /// <param name="entries">Catalog rows to insert in order.</param>
        /// <param name="insertIndex">Destination index in <c>[0, Count]</c>.</param>
        public void InsertFromCatalogAt(IReadOnlyList<FilterCatalogEntry> entries, int insertIndex)
        {
            ArgumentNullException.ThrowIfNull(entries);

            if (entries.Count == 0)
            {
                return;
            }

            insertIndex = Math.Clamp(insertIndex, 0, Steps.Count);
            var inserted = new List<AppliedFilterStepViewModel>();
            _WithSingleChainChanged(() =>
            {
                for (var offset = 0; offset < entries.Count; offset++)
                {
                    var step = _CreateStep(entries[offset]);
                    Steps.Insert(insertIndex + offset, step);
                    inserted.Add(step);
                }
            });

            SetSelectedSteps(inserted);
        }

        private AppliedFilterStepViewModel _CreateStep(FilterCatalogEntry entry)
        {
            var filter = _ResolveAddDefault(entry);
            var displayName = _GenerateCatalogDisplayName(entry);
            return new AppliedFilterStepViewModel(displayName, filter);
        }

        /// <summary>
        /// Resolves the catalog row for an applied filter instance.
        /// </summary>
        private static FilterCatalogEntry _CatalogEntryFor(BaseFilter filter)
        {
            return FilterCatalog.Entries.Single(catalogEntry => catalogEntry.FilterType == filter.GetType());
        }

        /// <summary>
        /// Resolves the filter instance used when adding from the palette (user default, else factory).
        /// </summary>
        private BaseFilter _ResolveAddDefault(FilterCatalogEntry entry)
        {
            if (
                _filterDefaults.TryGetDefault(entry.Type, out var userDefault)
                && userDefault.GetType() == entry.FilterType
            )
            {
                return userDefault;
            }

            return FilterCatalog.CreateDefault(entry);
        }

        private int _GetInsertIndex()
        {
            if (_selectedSteps.Count == 0)
            {
                return Steps.Count;
            }

            var firstSelectedIndex = _FindFirstSelectedIndex(_selectedSteps.ToHashSet());
            return firstSelectedIndex >= 0 ? firstSelectedIndex : Steps.Count;
        }

        /// <summary>
        /// Uses a stored step name when present; otherwise synthesizes a catalog label.
        /// </summary>
        /// <param name="storedName">Optional name from <see cref="FilterChainStep.Name"/>.</param>
        /// <param name="entry">Catalog entry for the filter type.</param>
        /// <returns>List label for the new step.</returns>
        private string _ResolveDisplayName(string? storedName, FilterCatalogEntry entry)
        {
            if (!string.IsNullOrWhiteSpace(storedName))
            {
                return storedName.Trim();
            }

            return _GenerateCatalogDisplayName(entry);
        }

        /// <summary>
        /// Builds a palette-add list label: catalog display name, then <c>(2)</c>, <c>(3)</c>, … by type count.
        /// </summary>
        private string _GenerateCatalogDisplayName(FilterCatalogEntry entry)
        {
            var sameTypeCount = Steps.Count(step => step.Filter.GetType() == entry.FilterType);
            if (sameTypeCount == 0)
            {
                return entry.DisplayName;
            }

            return $"{entry.DisplayName} ({sameTypeCount + 1})";
        }

        /// <summary>
        /// Ensures <paramref name="preferredDisplayName"/> is unique among step labels by appending <c>*</c>
        /// (MFR7 Free Names / Edit as Name List; distinct from palette <c>(n)</c> numbering).
        /// </summary>
        private string _AppendStarsUntilDisplayNameUnique(string preferredDisplayName)
        {
            var displayName = preferredDisplayName;
            while (Steps.Any(step => string.Equals(step.DisplayName, displayName, StringComparison.Ordinal)))
            {
                displayName += "*";
            }

            return displayName;
        }

        private void _MoveSelected(int offset)
        {
            if (_selectedSteps.Count == 0)
            {
                return;
            }

            var selected = _selectedSteps.ToHashSet();
            var moved = false;
            _WithSingleChainChanged(() =>
            {
                moved = ListReorder.TryMoveSelectedTowardNeighbor(Steps, selected, offset);
            });
            if (!moved)
            {
                return;
            }

            SetSelectedSteps([.. Steps.Where(selected.Contains)]);
        }

        private int _FindFirstSelectedIndex(IReadOnlyCollection<AppliedFilterStepViewModel> selected)
        {
            for (var index = 0; index < Steps.Count; index++)
            {
                if (selected.Contains(Steps[index]))
                {
                    return index;
                }
            }

            return -1;
        }

        private IReadOnlyList<AppliedFilterStepViewModel> _SelectStepsAfterRemove(int anchorIndex)
        {
            if (Steps.Count == 0 || anchorIndex < 0)
            {
                return [];
            }

            var nextIndex = Math.Min(anchorIndex, Steps.Count - 1);
            return [Steps[nextIndex]];
        }

        private bool _HasSelection()
        {
            return _selectedSteps.Count > 0;
        }

        private bool _HasSingleSelection()
        {
            return _selectedSteps.Count == 1;
        }

        private bool _HasSteps()
        {
            return Steps.Count > 0;
        }

        private bool _CanRemoveAllButSelected()
        {
            return _selectedSteps.Count > 0 && _selectedSteps.Count < Steps.Count;
        }

        /// <summary>
        /// Sets <see cref="AppliedFilterStepViewModel.Enabled"/> on every step with one <see cref="ChainChanged"/>.
        /// </summary>
        /// <param name="enabled">Value written to each step.</param>
        private void _SetAllEnabled(bool enabled)
        {
            if (Steps.Count == 0)
            {
                return;
            }

            _WithSingleChainChanged(() =>
            {
                foreach (var step in Steps)
                {
                    step.Enabled = enabled;
                }
            });
        }

        private bool _CanRemoveStepsAtIndices(IReadOnlyList<int> indices)
        {
            if (indices is null || indices.Count == 0)
            {
                return false;
            }

            return indices.Any(index => index >= 0 && index < Steps.Count);
        }

        private bool _CanMoveSelectedUp()
        {
            return _selectedSteps.Count > 0
                && ListReorder.CanMoveSelectedTowardNeighbor(Steps, _selectedSteps.ToHashSet(), offset: -1);
        }

        private bool _CanMoveSelectedDown()
        {
            return _selectedSteps.Count > 0
                && ListReorder.CanMoveSelectedTowardNeighbor(Steps, _selectedSteps.ToHashSet(), offset: 1);
        }

        /// <summary>
        /// Moves selected steps to <paramref name="targetIndex"/> (drag-drop insert index).
        /// </summary>
        /// <param name="sourceIndices">Indices of rows to move.</param>
        /// <param name="targetIndex">Destination index in <c>[0, Count]</c> before the move.</param>
        public void MoveStepsTo(IReadOnlyList<int> sourceIndices, int targetIndex)
        {
            ArgumentNullException.ThrowIfNull(sourceIndices);

            IReadOnlyList<int> newIndices = [];
            var moved = false;
            _WithSingleChainChanged(() =>
            {
                moved = ListReorder.TryMoveIndicesTo(Steps, sourceIndices, targetIndex, out newIndices);
            });
            if (!moved)
            {
                return;
            }

            SetSelectedSteps([.. newIndices.Select(index => Steps[index])]);
        }

        /// <summary>
        /// Removes applied steps by list index (drag-back to Available Filters).
        /// </summary>
        /// <param name="indices">Row indices to remove.</param>
        [RelayCommand(CanExecute = nameof(_CanRemoveStepsAtIndices))]
        public void RemoveStepsAtIndices(IReadOnlyList<int> indices)
        {
            ArgumentNullException.ThrowIfNull(indices);

            var sortedIndices = indices
                .Where(index => index >= 0 && index < Steps.Count)
                .OrderBy(index => index)
                .ToList();
            if (sortedIndices.Count == 0)
            {
                return;
            }

            var indexSet = sortedIndices.ToHashSet();
            var anchorIndex = sortedIndices[0];

            _WithSingleChainChanged(() => _RemoveAtIndices(indexSet));

            SetSelectedSteps(_SelectStepsAfterRemove(anchorIndex));
        }

        /// <summary>
        /// Removes steps at the given indices, highest index first (safe while mutating the list).
        /// </summary>
        /// <param name="indexSet">Indices to remove.</param>
        private void _RemoveAtIndices(HashSet<int> indexSet)
        {
            for (var index = Steps.Count - 1; index >= 0; index--)
            {
                if (indexSet.Contains(index))
                {
                    Steps.RemoveAt(index);
                }
            }
        }

        private void _NotifySelectionCommandsChanged()
        {
            RemoveSelectedCommand.NotifyCanExecuteChanged();
            RemoveAllButSelectedCommand.NotifyCanExecuteChanged();
            RemoveStepsAtIndicesCommand.NotifyCanExecuteChanged();
            MoveSelectedUpCommand.NotifyCanExecuteChanged();
            MoveSelectedDownCommand.NotifyCanExecuteChanged();
            ResetSelectedToDefaultsCommand.NotifyCanExecuteChanged();
            SaveSelectedAsDefaultCommand.NotifyCanExecuteChanged();
            OpenSelectedFilterHelpCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(CanShowFilterOptions));
        }

        /// <summary>
        /// True when exactly one step is selected (Help basename is always <c>{Type}.html</c>).
        /// </summary>
        private bool _CanOpenSelectedFilterHelp()
        {
            return _TryGetSelectedHelpFileName(out _);
        }

        /// <summary>
        /// Resolves the Help HTML basename for the sole selected step.
        /// </summary>
        private bool _TryGetSelectedHelpFileName(out string helpFileName)
        {
            helpFileName = string.Empty;
            if (_selectedSteps.Count != 1)
            {
                return false;
            }

            helpFileName = _CatalogEntryFor(_selectedSteps[0].Filter).HelpFileName;
            return true;
        }
    }
}
