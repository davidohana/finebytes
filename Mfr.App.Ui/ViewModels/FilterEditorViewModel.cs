using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters;

namespace Mfr.App.Ui.ViewModels
{
    /// <summary>
    /// Filter Configuration pane. Type-specific editors are implemented in later GUI phases.
    /// </summary>
    public sealed partial class FilterEditorViewModel : ViewModelBase
    {
        private AppliedFilterStepViewModel? _selectedStep;
        private bool _isSyncingApplyTo;

        /// <summary>
        /// Gets whether an Applied Filters row is driving the configuration pane.
        /// </summary>
        [ObservableProperty]
        private bool _hasSelectedStep;

        /// <summary>
        /// Gets the applied-filter title, e.g. <c>Applied Filter: Shrink Spaces</c>.
        /// </summary>
        [ObservableProperty]
        private string _titleText = string.Empty;

        /// <summary>
        /// Gets whether the selected filter supports Apply-To editing.
        /// </summary>
        [ObservableProperty]
        private bool _hasApplyTo;

        /// <summary>
        /// Gets the Apply-To choices for string-target filters.
        /// </summary>
        public IReadOnlyList<FilterApplyToOption> ApplyToOptions => FilterApplyToOption.All;

        /// <summary>
        /// Gets or sets the selected Apply-To target for the current string-target filter.
        /// </summary>
        [ObservableProperty]
        private FilterApplyToOption? _selectedApplyTo;

        /// <summary>
        /// Updates the pane from the Applied Filters selection (first row when multi-select).
        /// </summary>
        /// <param name="selectedSteps">Current Applied list selection.</param>
        internal void SyncSelection(IReadOnlyList<AppliedFilterStepViewModel> selectedSteps)
        {
            ArgumentNullException.ThrowIfNull(selectedSteps);

            var step = selectedSteps.Count > 0 ? selectedSteps[0] : null;
            _selectedStep = step;
            HasSelectedStep = step is not null;
            TitleText = step is null ? string.Empty : $"Applied Filter: {step.DisplayName}";

            _isSyncingApplyTo = true;
            try
            {
                HasApplyTo = step?.Filter is StringTargetFilter;
                SelectedApplyTo = step?.Filter is StringTargetFilter stringFilter
                    ? FilterApplyToOption.FromTarget(stringFilter.Target)
                    : null;
            }
            finally
            {
                _isSyncingApplyTo = false;
            }
        }

        partial void OnSelectedApplyToChanged(FilterApplyToOption? value)
        {
            if (_isSyncingApplyTo || _selectedStep is null || value is null)
            {
                return;
            }

            if (_selectedStep.Filter is not StringTargetFilter stringFilter)
            {
                return;
            }

            if (FilterApplyToOption.FromTarget(stringFilter.Target) == value)
            {
                return;
            }

            _selectedStep.SetFilter(stringFilter with { Target = value.Target });
        }
    }
}
