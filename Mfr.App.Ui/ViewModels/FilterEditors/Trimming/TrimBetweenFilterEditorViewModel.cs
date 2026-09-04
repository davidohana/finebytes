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

            LoadWithoutApplying(() =>
            {
                StartValue = filter.Options.Start.Value;
                StartAnchor = filter.Options.Start.Anchor;
                EndValue = filter.Options.End.Value;
                EndAnchor = filter.Options.End.Anchor;
            });
        }

        private void _ApplyOptions()
        {
            if (IsLoading || Step.Filter is not TrimBetweenFilter filter)
            {
                return;
            }

            var start = new Position(Value: ClampToInt(StartValue, 1, 1000), Anchor: StartAnchor);
            var end = new Position(Value: ClampToInt(EndValue, 1, 1000), Anchor: EndAnchor);
            var options = new TrimBetweenFilterOptions(Start: start, End: end);
            ApplyIfChanged(filter, filter with { Options = options });
        }
    }
}
