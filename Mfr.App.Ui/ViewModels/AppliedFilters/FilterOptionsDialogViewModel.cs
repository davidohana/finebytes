using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.Filters;

namespace Mfr.App.Ui.ViewModels.AppliedFilters
{
    /// <summary>
    /// Draft state for the Filter Options dialog (name and basic Apply-To targets).
    /// </summary>
    public sealed partial class FilterOptionsDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes draft fields from the selected applied-filter step.
        /// </summary>
        /// <param name="step">Applied list row being edited.</param>
        public FilterOptionsDialogViewModel(AppliedFilterStepViewModel step)
        {
            ArgumentNullException.ThrowIfNull(step);

            Name = step.DisplayName;
            HasApplyTo = step.Filter is StringTargetFilter;
            SelectedApplyTo =
                step.Filter is StringTargetFilter stringFilter
                    ? FilterTargetOption.FromTarget(stringFilter.Target)
                    : null;
        }

        /// <summary>
        /// Gets the Apply-To choices for string-target filters.
        /// </summary>
        public IReadOnlyList<FilterTargetOption> ApplyToOptions => FilterTargetOption.All;

        /// <summary>
        /// Gets or sets the filter instance name shown in the Applied list.
        /// </summary>
        [ObservableProperty]
        private string _name = string.Empty;

        /// <summary>
        /// Gets whether Apply-To editing is available for the selected filter.
        /// </summary>
        [ObservableProperty]
        private bool _hasApplyTo;

        /// <summary>
        /// Gets or sets the selected Apply-To target for string-target filters.
        /// </summary>
        [ObservableProperty]
        private FilterTargetOption? _selectedApplyTo;
    }
}
