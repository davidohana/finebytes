using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors.Attributes;
using Mfr.Filters.Attributes;

namespace Mfr.Tests.Ui.FilterEditors.Attributes
{
    /// <summary>
    /// Unit tests for <see cref="TimeShifterFilterEditorViewModel"/>.
    /// </summary>
    public sealed class TimeShifterFilterEditorViewModelTests
    {
        /// <summary>
        /// Verifies Time Shifter option edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Time_shifter_options_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Time Shifter", new TimeShifterFilter());

            var editor = new TimeShifterFilterEditorViewModel(step);

            Assert.Equal(TimestampField.LastWrite, editor.SelectedTimestampField.Field);

            Assert.Equal(1m, editor.Amount);

            Assert.Equal(TimeShiftUnit.Days, editor.SelectedUnit);

            editor.SelectedTimestampField = editor.TimestampFields.Single(c => c.Field == TimestampField.Creation);

            editor.Amount = -2;

            editor.SelectedUnit = TimeShiftUnit.Hours;

            var options = ((TimeShifterFilter)step.Filter).Options;

            Assert.Equal(TimestampField.Creation, options.TimestampField);

            Assert.Equal(-2, options.Amount);

            Assert.Equal(TimeShiftUnit.Hours, options.Unit);
        }
    }
}
