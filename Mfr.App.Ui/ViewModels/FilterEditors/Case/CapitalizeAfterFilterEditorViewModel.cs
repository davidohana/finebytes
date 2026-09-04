using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Case;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Case
{
    /// <summary>
    /// Filter Configuration editor for <see cref="CapitalizeAfterFilter"/>.
    /// </summary>
    internal sealed partial class CapitalizeAfterFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        private bool _isLoading;

        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        public CapitalizeAfterFilterEditorViewModel(AppliedFilterStepViewModel step)
            : base(step)
        {
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets or sets the characters after which the following letter is uppercased.
        /// </summary>
        [ObservableProperty]
        private string _capitalizeAfterChars = string.Empty;

        partial void OnCapitalizeAfterCharsChanged(string value) => _ApplyOptions();

        private void _SyncFromFilter()
        {
            if (Step.Filter is not CapitalizeAfterFilter filter)
            {
                return;
            }

            _isLoading = true;
            try
            {
                CapitalizeAfterChars = filter.Options.CapitalizeAfterChars;
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void _ApplyOptions()
        {
            if (_isLoading || Step.Filter is not CapitalizeAfterFilter filter)
            {
                return;
            }

            var options = new CapitalizeAfterOptions(CapitalizeAfterChars: CapitalizeAfterChars ?? string.Empty);
            ApplyIfChanged(filter, filter with { Options = options });
        }
    }
}
