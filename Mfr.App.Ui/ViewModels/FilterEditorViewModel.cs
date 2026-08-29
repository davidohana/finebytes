using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;

namespace Mfr.App.Ui.ViewModels
{
    /// <summary>
    /// Filter Configuration pane. Type-specific editors are implemented in later GUI phases.
    /// </summary>
    public sealed partial class FilterEditorViewModel : ViewModelBase
    {
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
        /// Updates the pane from the Applied Filters selection (first row when multi-select).
        /// </summary>
        /// <param name="selectedSteps">Current Applied list selection.</param>
        internal void SyncSelection(IReadOnlyList<AppliedFilterStepViewModel> selectedSteps)
        {
            ArgumentNullException.ThrowIfNull(selectedSteps);

            var step = selectedSteps.Count > 0 ? selectedSteps[0] : null;
            HasSelectedStep = step is not null;
            TitleText = step is null ? string.Empty : $"Applied Filter: {step.DisplayName}";
        }
    }
}
