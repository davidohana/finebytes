using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.Models.Filters;

namespace Mfr.App.Ui.ViewModels.AppliedFilters
{
    /// <summary>
    /// One row in the Applied Filters list.
    /// </summary>
    public sealed partial class AppliedFilterStepViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new applied-filter step.
        /// </summary>
        /// <param name="displayName">Unique list label (catalog display name plus duplicate suffix).</param>
        /// <param name="filter">Filter configuration for this step.</param>
        public AppliedFilterStepViewModel(string displayName, BaseFilter filter)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
            ArgumentNullException.ThrowIfNull(filter);

            DisplayName = displayName;
            Filter = filter;
            ApplyToLabel = FilterTargetLabels.GetApplyToLabel(filter);
        }

        /// <summary>
        /// Gets whether this step participates when the chain runs.
        /// </summary>
        [ObservableProperty]
        private bool _enabled = true;

        /// <summary>
        /// Gets the unique list label for this step.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the Apply-To subtitle for string-target filters.
        /// </summary>
        [ObservableProperty]
        private string _applyToLabel;

        /// <summary>
        /// Gets the filter configuration for this step.
        /// </summary>
        public BaseFilter Filter { get; private set; }

        /// <summary>
        /// Replaces the filter configuration and refreshes <see cref="ApplyToLabel"/>.
        /// </summary>
        /// <param name="filter">New filter instance for this step.</param>
        internal void SetFilter(BaseFilter filter)
        {
            ArgumentNullException.ThrowIfNull(filter);
            Filter = filter;
            ApplyToLabel = FilterTargetLabels.GetApplyToLabel(filter);
        }
    }
}
