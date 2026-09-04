using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters;
using Mfr.Filters.Trimming;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Trimming
{
    /// <summary>
    /// Filter Configuration editor for count-based filters (Trim/Extract Left/Right).
    /// </summary>
    internal sealed partial class CountFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        public CountFilterEditorViewModel(AppliedFilterStepViewModel step)
            : base(step)
        {
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets a tooltip describing what the count means for the selected filter.
        /// </summary>
        public string CountToolTip =>
            Step.Filter switch
            {
                TrimLeftFilter => "Removes this many characters from the start of the target.",
                TrimRightFilter => "Removes this many characters from the end of the target.",
                ExtractLeftFilter => "Keeps this many characters from the start; drops the rest.",
                ExtractRightFilter => "Keeps this many characters from the end; drops the rest.",
                _ => "How many characters this filter uses.",
            };

        /// <summary>
        /// Gets or sets the number of characters.
        /// </summary>
        [ObservableProperty]
        private decimal _count;

        partial void OnCountChanged(decimal value) => _ApplyOptions();

        private void _SyncFromFilter()
        {
            if (Step.Filter is not ICountOptionsFilter countFilter)
            {
                return;
            }

            LoadWithoutApplying(() => Count = countFilter.Options.Count);
        }

        private void _ApplyOptions()
        {
            if (IsLoading || Step.Filter is not ICountOptionsFilter countFilter)
            {
                return;
            }

            var options = new CountFilterOptions(Count: ClampToInt(Count, 0, 9999));
            ApplyIfChanged(Step.Filter, countFilter.WithOptions(options));
        }
    }
}
