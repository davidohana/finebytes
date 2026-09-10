using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.Engine.Presets;
using Mfr.Filters;
using Mfr.Models.Filters;
using Mfr.Utils;

namespace Mfr.App.Ui.ViewModels.AppliedFilters
{
    /// <summary>
    /// Applied Filters pane: ordered filter stack edited before preview.
    /// </summary>
    public sealed partial class AppliedFiltersViewModel : ViewModelBase
    {
        private readonly FilterDefaultsStore _filterDefaults;
        private readonly List<AppliedFilterStepViewModel> _selectedSteps = [];
        private int _chainChangedBatchDepth;
        private bool _chainChangedQueued;

        /// <summary>
        /// Initializes an empty applied-filter list.
        /// </summary>
        /// <param name="filterDefaults">
        /// Per-type add defaults store. When null, uses an empty store that does not read AppData
        /// (production passes <see cref="FilterDefaultsStore.OpenDefault"/>).
        /// </param>
        public AppliedFiltersViewModel(FilterDefaultsStore? filterDefaults = null)
        {
            _filterDefaults = filterDefaults ?? FilterDefaultsStore.CreateEmpty();
            Steps = [];
            Steps.CollectionChanged += _OnStepsCollectionChanged;
        }

        /// <summary>
        /// Clears in-memory per-type add defaults after Reset Configuration deletes the file.
        /// </summary>
        internal void ClearFilterDefaultsCache()
        {
            _filterDefaults.Clear();
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
        /// Appends a concrete filter instance and selects it (Free Names Edit and similar).
        /// </summary>
        /// <param name="filter">Fully configured filter to add (not a catalog default).</param>
        /// <param name="preferredDisplayName">
        /// Desired list label; when already taken, appends <c>*</c> until unique (MFR7 Free Names).
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

            var displayName = _GenerateUniqueDisplayName(trimmedName);
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

            foreach (var step in Steps)
            {
                step.PropertyChanged -= _OnStepPropertyChanged;
            }

            Steps.Clear();
            SetSelectedSteps([]);
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
            var entry = FilterCatalog.Entries.Single(catalogEntry => catalogEntry.FilterType == step.Filter.GetType());
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
            var entry = FilterCatalog.Entries.Single(catalogEntry => catalogEntry.FilterType == step.Filter.GetType());
            _filterDefaults.SetDefault(step.Filter);
            FilterDefaultSaved?.Invoke(this, entry.DisplayName);
        }

        /// <summary>
        /// Builds a <see cref="FilterChain"/> matching the current stack.
        /// </summary>
        /// <returns>Enabled flags and filters in list order.</returns>
        public FilterChain ToChain()
        {
            return new FilterChain
            {
                Steps = [.. Steps.Select(step => new FilterChainStep(step.Enabled, step.Filter))],
            };
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
            RemoveStepsAtIndicesCommand.NotifyCanExecuteChanged();
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
            var displayName = _GenerateDisplayName(entry);
            return new AppliedFilterStepViewModel(displayName, filter);
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
        /// Builds a unique list label: catalog display name, then <c>(2)</c>, <c>(3)</c>, … for duplicates.
        /// </summary>
        private string _GenerateDisplayName(FilterCatalogEntry entry)
        {
            var sameTypeCount = Steps.Count(step => step.Filter.GetType() == entry.FilterType);
            if (sameTypeCount == 0)
            {
                return entry.DisplayName;
            }

            return $"{entry.DisplayName} ({sameTypeCount + 1})";
        }

        /// <summary>
        /// Ensures <paramref name="preferredDisplayName"/> is unique among step labels by appending <c>*</c>.
        /// </summary>
        private string _GenerateUniqueDisplayName(string preferredDisplayName)
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

            _WithSingleChainChanged(() =>
            {
                for (var index = Steps.Count - 1; index >= 0; index--)
                {
                    if (indexSet.Contains(index))
                    {
                        Steps.RemoveAt(index);
                    }
                }
            });

            SetSelectedSteps(_SelectStepsAfterRemove(anchorIndex));
        }

        private void _NotifySelectionCommandsChanged()
        {
            RemoveSelectedCommand.NotifyCanExecuteChanged();
            RemoveStepsAtIndicesCommand.NotifyCanExecuteChanged();
            MoveSelectedUpCommand.NotifyCanExecuteChanged();
            MoveSelectedDownCommand.NotifyCanExecuteChanged();
            ResetSelectedToDefaultsCommand.NotifyCanExecuteChanged();
            SaveSelectedAsDefaultCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(CanShowFilterOptions));
        }
    }
}
