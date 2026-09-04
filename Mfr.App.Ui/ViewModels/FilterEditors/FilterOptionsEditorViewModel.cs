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
        /// Gets whether property setters should skip live option replace (sync-from-filter in progress).
        /// </summary>
        protected bool IsLoading { get; private set; }

        /// <summary>
        /// Runs <paramref name="load"/> without treating property changes as option applies.
        /// </summary>
        /// <param name="load">Copies current filter options into editor properties.</param>
        protected void LoadWithoutApplying(Action load)
        {
            ArgumentNullException.ThrowIfNull(load);
            IsLoading = true;
            try
            {
                load();
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Truncates <paramref name="value"/> to an integer in <paramref name="minInclusive"/>..<paramref name="maxInclusive"/>.
        /// </summary>
        /// <param name="value">NumericUpDown binding value.</param>
        /// <param name="minInclusive">Lowest allowed integer.</param>
        /// <param name="maxInclusive">Highest allowed integer.</param>
        /// <returns>Clamped integer.</returns>
        protected static int ClampToInt(decimal value, int minInclusive, int maxInclusive)
        {
            return Math.Clamp((int)value, minInclusive, maxInclusive);
        }

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
