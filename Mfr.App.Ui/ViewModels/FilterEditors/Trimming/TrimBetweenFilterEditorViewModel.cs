using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Trimming;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Trimming
{
    /// <summary>
    /// Filter Configuration editor for <see cref="TrimBetweenFilter"/>.
    /// </summary>
    internal sealed partial class TrimBetweenFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        private bool _isLoading;

        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        public TrimBetweenFilterEditorViewModel(AppliedFilterStepViewModel step)
            : base(step)
        {
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets the Left/Right choices for position anchors.
        /// </summary>
        public IReadOnlyList<Side> AnchorSides { get; } = [Side.Left, Side.Right];

        /// <summary>
        /// Gets or sets the 1-based start position value.
        /// </summary>
        [ObservableProperty]
        private decimal _startValue = 2;

        /// <summary>
        /// Gets or sets the start position anchor side.
        /// </summary>
        [ObservableProperty]
        private Side _startAnchor = Side.Left;

        /// <summary>
        /// Gets or sets the 1-based end position value.
        /// </summary>
        [ObservableProperty]
        private decimal _endValue = 4;

        /// <summary>
        /// Gets or sets the end position anchor side.
        /// </summary>
        [ObservableProperty]
        private Side _endAnchor = Side.Left;

        partial void OnStartValueChanged(decimal value) => _ApplyOptions();

        partial void OnStartAnchorChanged(Side value) => _ApplyOptions();

        partial void OnEndValueChanged(decimal value) => _ApplyOptions();

        partial void OnEndAnchorChanged(Side value) => _ApplyOptions();

        private void _SyncFromFilter()
        {
            if (Step.Filter is not TrimBetweenFilter filter)
            {
                return;
            }

            _isLoading = true;
            try
            {
                StartValue = filter.Options.Start.Value;
                StartAnchor = filter.Options.Start.Anchor;
                EndValue = filter.Options.End.Value;
                EndAnchor = filter.Options.End.Anchor;
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void _ApplyOptions()
        {
            if (_isLoading || Step.Filter is not TrimBetweenFilter filter)
            {
                return;
            }

            var start = new Position(Value: _ClampPosition(StartValue), Anchor: StartAnchor);
            var end = new Position(Value: _ClampPosition(EndValue), Anchor: EndAnchor);
            var options = new TrimBetweenFilterOptions(Start: start, End: end);
            ApplyIfChanged(filter, filter with { Options = options });
        }

        private static int _ClampPosition(decimal value)
        {
            return Math.Clamp((int)value, 1, 1000);
        }
    }
}
