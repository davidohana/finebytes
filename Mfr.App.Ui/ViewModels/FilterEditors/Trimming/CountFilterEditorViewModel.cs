using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Trimming
{
    /// <summary>
    /// Filter Configuration editor for count-based filters (Trim/Extract Left/Right).
    /// </summary>
    internal sealed partial class CountFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        private bool _isLoading;

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

            _isLoading = true;
            try
            {
                Count = countFilter.Options.Count;
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void _ApplyOptions()
        {
            if (_isLoading || Step.Filter is not ICountOptionsFilter countFilter)
            {
                return;
            }

            var options = new CountFilterOptions(Count: (int)Math.Max(0, Count));
            ApplyIfChanged(Step.Filter, countFilter.WithOptions(options));
        }
    }
}
