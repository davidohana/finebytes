using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;

namespace Mfr.App.Ui.ViewModels.FilterEditors
{
    /// <summary>
    /// Filter Configuration pane host for type-specific option editors.
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
        /// Gets the type-specific options editor for the selected filter, or <see langword="null"/> when none.
        /// </summary>
        [ObservableProperty]
        private FilterOptionsEditorViewModel? _optionsEditor;

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
            OptionsEditor = step is null ? null : FilterOptionsEditorFactory.Create(step);
        }
    }
}
