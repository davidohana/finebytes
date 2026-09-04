using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Misc;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Misc
{
    /// <summary>
    /// Filter Configuration editor for <see cref="FixLeadingZerosFilter"/>.
    /// </summary>
    internal sealed partial class FixLeadingZerosFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        private bool _isLoading;

        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        public FixLeadingZerosFilterEditorViewModel(AppliedFilterStepViewModel step)
            : base(step)
        {
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets or sets the target numeric width (minimum digit count).
        /// </summary>
        [ObservableProperty]
        private decimal _width = 2;

        /// <summary>
        /// Gets or sets whether extra leading zeros are stripped before padding.
        /// </summary>
        [ObservableProperty]
        private bool _removeExtraZeros;

        /// <summary>
        /// Gets or sets the maximum number of digit groups to fix (<c>0</c> = all).
        /// </summary>
        [ObservableProperty]
        private decimal _maxCount = 1;

        /// <summary>
        /// Gets or sets whether only whole-word digit groups are fixed.
        /// </summary>
        [ObservableProperty]
        private bool _wholeWordOnly = true;

        partial void OnWidthChanged(decimal value) => _ApplyOptions();

        partial void OnRemoveExtraZerosChanged(bool value) => _ApplyOptions();

        partial void OnMaxCountChanged(decimal value) => _ApplyOptions();

        partial void OnWholeWordOnlyChanged(bool value) => _ApplyOptions();

        private void _SyncFromFilter()
        {
            if (Step.Filter is not FixLeadingZerosFilter filter)
            {
                return;
            }

            _isLoading = true;
            try
            {
                Width = filter.Options.Width;
                RemoveExtraZeros = filter.Options.RemoveExtraZeros;
                MaxCount = filter.Options.MaxCount;
                WholeWordOnly = filter.Options.WholeWordOnly;
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void _ApplyOptions()
        {
            if (_isLoading || Step.Filter is not FixLeadingZerosFilter filter)
            {
                return;
            }

            var options = new FixLeadingZerosOptions(
                Width: _ClampWidth(Width),
                RemoveExtraZeros: RemoveExtraZeros,
                MaxCount: _ClampMaxCount(MaxCount),
                WholeWordOnly: WholeWordOnly
            );
            ApplyIfChanged(filter, filter with { Options = options });
        }

        private static int _ClampWidth(decimal value)
        {
            return Math.Clamp((int)value, 1, 30);
        }

        private static int _ClampMaxCount(decimal value)
        {
            return Math.Clamp((int)value, 0, 9999);
        }
    }
}
