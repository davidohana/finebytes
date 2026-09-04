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
            var count = _TryGetCount(Step.Filter);
            if (count is null)
            {
                return;
            }

            _isLoading = true;
            try
            {
                Count = count.Value;
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
            var updated = _WithCount(Step.Filter, options);
            if (updated is null)
            {
                return;
            }

            ApplyIfChanged(Step.Filter, updated);
        }

        /// <summary>
        /// Reads the character count from a Trim/Extract Left/Right filter.
        /// </summary>
        /// <param name="filter">Current step filter.</param>
        /// <returns>The count, or <see langword="null"/> when <paramref name="filter"/> is not a count filter.</returns>
        private static int? _TryGetCount(BaseFilter filter)
        {
            return filter switch
            {
                TrimLeftFilter f => f.Options.Count,
                TrimRightFilter f => f.Options.Count,
                ExtractLeftFilter f => f.Options.Count,
                ExtractRightFilter f => f.Options.Count,
                _ => null,
            };
        }

        /// <summary>
        /// Returns a copy of <paramref name="filter"/> with <paramref name="options"/> applied.
        /// </summary>
        /// <param name="filter">Current step filter.</param>
        /// <param name="options">Replacement count options.</param>
        /// <returns>The updated filter, or <see langword="null"/> when <paramref name="filter"/> is not a count filter.</returns>
        private static BaseFilter? _WithCount(BaseFilter filter, CountFilterOptions options)
        {
            return filter switch
            {
                TrimLeftFilter f => f with { Options = options },
                TrimRightFilter f => f with { Options = options },
                ExtractLeftFilter f => f with { Options = options },
                ExtractRightFilter f => f with { Options = options },
                _ => null,
            };
        }
    }
}
