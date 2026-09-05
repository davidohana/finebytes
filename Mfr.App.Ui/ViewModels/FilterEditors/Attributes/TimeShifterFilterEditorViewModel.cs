using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Attributes;
using Mfr.Models.Media;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Attributes
{
    /// <summary>
    /// Filter Configuration editor for <see cref="TimeShifterFilter"/>.
    /// </summary>
    internal sealed partial class TimeShifterFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        /// <summary>Signed amount spinner lower bound (must match AXAML Minimum).</summary>
        public const decimal AmountMin = -10_000_000m;

        /// <summary>Signed amount spinner upper bound (must match AXAML Maximum).</summary>
        public const decimal AmountMax = 10_000_000m;

        /// <summary>
        /// Unit combo order: MFR7 Days-first list, with Months/Years instead of Milliseconds.
        /// </summary>
        private static readonly IReadOnlyList<TimeShiftUnit> s_Units =
        [
            TimeShiftUnit.Days,
            TimeShiftUnit.Hours,
            TimeShiftUnit.Minutes,
            TimeShiftUnit.Seconds,
            TimeShiftUnit.Months,
            TimeShiftUnit.Years,
        ];

        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        public TimeShifterFilterEditorViewModel(AppliedFilterStepViewModel step)
            : base(step)
        {
            _selectedTimestampField = TimestampFieldChoice.For(TimestampField.LastWrite);
            _selectedUnit = TimeShiftUnit.Days;
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets the timestamp-field combo choices (MFR7 labels).
        /// </summary>
        public IReadOnlyList<TimestampFieldChoice> TimestampFields => TimestampFieldChoice.All;

        /// <summary>
        /// Gets the shift-unit combo choices.
        /// </summary>
        public IReadOnlyList<TimeShiftUnit> Units => s_Units;

        /// <summary>
        /// Gets or sets which filesystem timestamp to shift.
        /// </summary>
        [ObservableProperty]
        private TimestampFieldChoice _selectedTimestampField;

        /// <summary>
        /// Gets or sets the signed shift amount (negative shifts backward).
        /// </summary>
        [ObservableProperty]
        private decimal _amount = 1;

        /// <summary>
        /// Gets or sets how to interpret <see cref="Amount"/>.
        /// </summary>
        [ObservableProperty]
        private TimeShiftUnit _selectedUnit = TimeShiftUnit.Days;

        partial void OnSelectedTimestampFieldChanged(TimestampFieldChoice value) => _ApplyOptions();

        partial void OnAmountChanged(decimal value) => _ApplyOptions();

        partial void OnSelectedUnitChanged(TimeShiftUnit value) => _ApplyOptions();

        /// <summary>
        /// Copies current filter options into editor properties without live replace.
        /// </summary>
        private void _SyncFromFilter()
        {
            if (Step.Filter is not TimeShifterFilter filter)
            {
                return;
            }

            LoadWithoutApplying(() =>
            {
                SelectedTimestampField = TimestampFieldChoice.For(filter.Options.TimestampField);
                Amount = filter.Options.Amount;
                SelectedUnit = filter.Options.Unit;
            });
        }

        /// <summary>
        /// Writes clamped amount and selections into a new <see cref="TimeShifterOptions"/> on the step filter.
        /// </summary>
        private void _ApplyOptions()
        {
            if (IsLoading || SelectedTimestampField is null || Step.Filter is not TimeShifterFilter filter)
            {
                return;
            }

            var options = new TimeShifterOptions(
                TimestampField: SelectedTimestampField.Field,
                Amount: ClampToInt(Amount, (int)AmountMin, (int)AmountMax),
                Unit: SelectedUnit
            );
            ApplyIfChanged(filter, filter with { Options = options });
        }
    }
}
