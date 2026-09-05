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
        private const int AmountMin = -10_000_000;
        private const int AmountMax = 10_000_000;

        private static readonly IReadOnlyList<TimestampFieldChoice> s_TimestampFields =
        [
            new(TimestampField.Creation, "Creation"),
            new(TimestampField.LastWrite, "Last Write"),
            new(TimestampField.LastAccess, "Last Access"),
        ];

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
            _selectedTimestampField = _ChoiceFor(TimestampField.LastWrite);
            _selectedUnit = TimeShiftUnit.Days;
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets the timestamp-field combo choices (MFR7 labels).
        /// </summary>
        public IReadOnlyList<TimestampFieldChoice> TimestampFields => s_TimestampFields;

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
                SelectedTimestampField = _ChoiceFor(filter.Options.TimestampField);
                Amount = filter.Options.Amount;
                SelectedUnit = _UnitOrDefault(filter.Options.Unit);
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
                Amount: ClampToInt(Amount, AmountMin, AmountMax),
                Unit: SelectedUnit
            );
            ApplyIfChanged(filter, filter with { Options = options });
        }

        /// <summary>
        /// Maps a <see cref="TimestampField"/> to its combo row; unknown values fall back to Last Write.
        /// </summary>
        private static TimestampFieldChoice _ChoiceFor(TimestampField field)
        {
            return s_TimestampFields.FirstOrDefault(c => c.Field == field)
                ?? s_TimestampFields.First(c => c.Field == TimestampField.LastWrite);
        }

        /// <summary>
        /// Returns <paramref name="unit"/> when listed in the combo; otherwise Days.
        /// </summary>
        private static TimeShiftUnit _UnitOrDefault(TimeShiftUnit unit)
        {
            return s_Units.Contains(unit) ? unit : TimeShiftUnit.Days;
        }
    }
}
