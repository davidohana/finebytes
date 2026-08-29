using Mfr.App.Ui.ViewModels.AppliedFilters;

namespace Mfr.App.Ui.ViewModels.FilterEditors
{
    /// <summary>
    /// Base type for type-specific Filter Configuration option editors.
    /// </summary>
    public abstract class FilterOptionsEditorViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes an options editor for one applied-filter step.
        /// </summary>
        /// <param name="step">Applied list row being edited.</param>
        protected FilterOptionsEditorViewModel(AppliedFilterStepViewModel step)
        {
            ArgumentNullException.ThrowIfNull(step);
            Step = step;
        }

        /// <summary>
        /// Gets the applied list row being edited.
        /// </summary>
        protected AppliedFilterStepViewModel Step { get; }
    }
}
