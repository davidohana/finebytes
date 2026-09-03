using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        private readonly List<AppliedFilterStepViewModel> _selectedSteps = [];

        /// <summary>
        /// Initializes an empty applied-filter list.
        /// </summary>
        public AppliedFiltersViewModel()
        {
            Steps = [];
            Steps.CollectionChanged += _OnStepsCollectionChanged;
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
        /// Gets whether exactly one step is selected for Filter Options.
        /// </summary>
        public bool CanShowFilterOptions => _selectedSteps.Count == 1;

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

        private void _RaiseChainChanged()
        {
            ChainChanged?.Invoke(this, EventArgs.Empty);
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
            for (var offset = 0; offset < entries.Count; offset++)
            {
                var step = _CreateStep(entries[offset]);
                Steps.Insert(insertIndex + offset, step);
                inserted.Add(step);
            }

            SetSelectedSteps(inserted);
        }

        private AppliedFilterStepViewModel _CreateStep(FilterCatalogEntry entry)
        {
            var filter = FilterCatalog.CreateDefault(entry);
            var displayName = _GenerateDisplayName(entry);
            return new AppliedFilterStepViewModel(displayName, filter);
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

        private void _MoveSelected(int offset)
        {
            if (_selectedSteps.Count == 0)
            {
                return;
            }

            var selected = _selectedSteps.ToHashSet();
            if (!ListReorder.TryMoveSelectedTowardNeighbor(Steps, selected, offset))
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

            if (!ListReorder.TryMoveIndicesTo(Steps, sourceIndices, targetIndex, out var newIndices))
            {
                return;
            }

            var moved = newIndices.Select(index => Steps[index]).ToList();
            SetSelectedSteps(moved);
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

            for (var index = Steps.Count - 1; index >= 0; index--)
            {
                if (indexSet.Contains(index))
                {
                    Steps.RemoveAt(index);
                }
            }

            SetSelectedSteps(_SelectStepsAfterRemove(anchorIndex));
        }

        private void _NotifySelectionCommandsChanged()
        {
            RemoveSelectedCommand.NotifyCanExecuteChanged();
            RemoveStepsAtIndicesCommand.NotifyCanExecuteChanged();
            MoveSelectedUpCommand.NotifyCanExecuteChanged();
            MoveSelectedDownCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(CanShowFilterOptions));
        }
    }
}
