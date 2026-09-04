using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters;
using Mfr.Filters.Trimming;
using Mfr.Models.Filters;

namespace Mfr.App.Ui.ViewModels.FilterEditors
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
            _isLoading = true;
            try
            {
                Count = Step.Filter switch
                {
                    TrimLeftFilter f => f.Options.Count,
                    TrimRightFilter f => f.Options.Count,
                    ExtractLeftFilter f => f.Options.Count,
                    ExtractRightFilter f => f.Options.Count,
                    _ => 0,
                };
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void _ApplyOptions()
        {
            if (_isLoading)
            {
                return;
            }

            var options = new CountFilterOptions(Count: (int)Math.Max(0, Count));
            BaseFilter updated = Step.Filter switch
            {
                TrimLeftFilter f => f with { Options = options },
                TrimRightFilter f => f with { Options = options },
                ExtractLeftFilter f => f with { Options = options },
                ExtractRightFilter f => f with { Options = options },
                var other => other,
            };

            ApplyIfChanged(Step.Filter, updated);
        }
    }
}
