using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Models.Filters;

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

        /// <summary>
        /// Replaces the step filter when <paramref name="updated"/> differs from <paramref name="current"/>.
        /// </summary>
        /// <param name="current">Filter currently stored on the step.</param>
        /// <param name="updated">Candidate replacement.</param>
        protected void ApplyIfChanged(BaseFilter current, BaseFilter updated)
        {
            if (Equals(current, updated))
            {
                return;
            }

            Step.SetFilter(updated);
        }
    }
}
