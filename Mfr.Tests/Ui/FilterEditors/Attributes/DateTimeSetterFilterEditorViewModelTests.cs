using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors.Attributes;
using Mfr.Filters.Attributes;

namespace Mfr.Tests.Ui.FilterEditors.Attributes
{
    /// <summary>
    /// Unit tests for <see cref="DateTimeSetterFilterEditorViewModel"/>.
    /// </summary>
    public sealed class DateTimeSetterFilterEditorViewModelTests
    {
        /// <summary>
        /// Verifies Date/Time Setter option edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Date_time_setter_options_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Date/Time Setter", new DateTimeSetterFilter());
            var editor = new DateTimeSetterFilterEditorViewModel(step);

            Assert.True(editor.SetDate);
            Assert.True(editor.SetTime);
            Assert.Equal(TimestampField.LastWrite, editor.SelectedTimestampField.Field);
            Assert.Equal(
                DateTime.Today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                editor.DateText
            );
            Assert.False(string.IsNullOrWhiteSpace(editor.TimeText));

            editor.SelectedTimestampField = editor.TimestampFields.Single(c => c.Field == TimestampField.Creation);
            editor.DateText = "2020-12-25";
            editor.TimeText = "09:00:15";
            editor.SetTime = false;

            var options = ((DateTimeSetterFilter)step.Filter).Options;
            Assert.Equal(TimestampField.Creation, options.TimestampField);
            Assert.True(options.SetDate);
            Assert.Equal(new DateOnly(2020, 12, 25), options.Date);
            Assert.False(options.SetTime);
            Assert.Equal(new TimeOnly(9, 0, 15), options.Time);

            editor.SetTime = true;
            editor.SetCurrentCommand.Execute(null);
            options = ((DateTimeSetterFilter)step.Filter).Options;
            Assert.Equal(DateOnly.FromDateTime(DateTime.Today), options.Date);
            Assert.Equal(
                editor.TimeText,
                options.Time.ToString("HH':'mm':'ss", System.Globalization.CultureInfo.InvariantCulture)
            );
        }

        /// <summary>
        /// Verifies illegal clock times are not applied and the text reverts to the last valid value.
        /// </summary>
        [Fact]
        public void Date_time_setter_rejects_illegal_time_and_reverts_text()
        {
            var step = new AppliedFilterStepViewModel(
                "Date/Time Setter",
                new DateTimeSetterFilter(
                    Options: new DateTimeSetterOptions(
                        TimestampField: TimestampField.LastWrite,
                        SetDate: true,
                        Date: new DateOnly(2020, 12, 25),
                        SetTime: true,
                        Time: new TimeOnly(23, 19, 1)
                    )
                )
            );
            var editor = new DateTimeSetterFilterEditorViewModel(step) { TimeText = "25:19:01" };

            Assert.Equal("23:19:01", editor.TimeText);
            var options = ((DateTimeSetterFilter)step.Filter).Options;
            Assert.Equal(new TimeOnly(23, 19, 1), options.Time);
            Assert.Equal(new DateOnly(2020, 12, 25), options.Date);
        }

        /// <summary>
        /// Verifies non-calendar dates and pre-file-time years are not applied and text reverts.
        /// </summary>
        [Fact]
        public void Date_time_setter_rejects_illegal_date_and_reverts_text()
        {
            var step = new AppliedFilterStepViewModel(
                "Date/Time Setter",
                new DateTimeSetterFilter(
                    Options: new DateTimeSetterOptions(
                        TimestampField: TimestampField.LastWrite,
                        SetDate: true,
                        Date: new DateOnly(2020, 12, 25),
                        SetTime: true,
                        Time: new TimeOnly(9, 0, 15)
                    )
                )
            );
            var editor = new DateTimeSetterFilterEditorViewModel(step) { DateText = "2024-02-30" };
            Assert.Equal("2020-12-25", editor.DateText);
            Assert.Equal(new DateOnly(2020, 12, 25), ((DateTimeSetterFilter)step.Filter).Options.Date);

            editor.DateText = "1600-12-31";
            Assert.Equal("2020-12-25", editor.DateText);
            Assert.Equal(new DateOnly(2020, 12, 25), ((DateTimeSetterFilter)step.Filter).Options.Date);

            editor.DateText = "3026-09-05";
            Assert.Equal("2020-12-25", editor.DateText);
            Assert.Equal(new DateOnly(2020, 12, 25), ((DateTimeSetterFilter)step.Filter).Options.Date);

            editor.DateText = "2101-01-01";
            Assert.Equal("2020-12-25", editor.DateText);
            Assert.Equal(new DateOnly(2020, 12, 25), ((DateTimeSetterFilter)step.Filter).Options.Date);

            editor.DateText = "2100-12-31";
            Assert.Equal("2100-12-31", editor.DateText);
            Assert.Equal(new DateOnly(2100, 12, 31), ((DateTimeSetterFilter)step.Filter).Options.Date);
        }

        /// <summary>
        /// Verifies a valid date still applies when time text is incomplete (HH:mm mid-edit toward HH:mm:ss).
        /// </summary>
        [Fact]
        public void Date_time_setter_applies_date_while_time_text_is_incomplete()
        {
            var step = new AppliedFilterStepViewModel(
                "Date/Time Setter",
                new DateTimeSetterFilter(
                    Options: new DateTimeSetterOptions(
                        TimestampField: TimestampField.LastWrite,
                        SetDate: true,
                        Date: new DateOnly(2020, 12, 25),
                        SetTime: true,
                        Time: new TimeOnly(19, 58, 0)
                    )
                )
            );
            var editor = new DateTimeSetterFilterEditorViewModel(step) { TimeText = "18:1", DateText = "2026-09-05" };

            var options = ((DateTimeSetterFilter)step.Filter).Options;
            Assert.Equal(new DateOnly(2026, 9, 5), options.Date);
            Assert.Equal(new TimeOnly(19, 58, 0), options.Time);
            Assert.Equal("18:1", editor.TimeText);
        }

        /// <summary>
        /// Verifies <c>HH:mm</c> time text is accepted (seconds default to zero).
        /// </summary>
        [Fact]
        public void Date_time_setter_accepts_hh_mm_time_text()
        {
            var step = new AppliedFilterStepViewModel(
                "Date/Time Setter",
                new DateTimeSetterFilter(
                    Options: new DateTimeSetterOptions(
                        TimestampField: TimestampField.LastWrite,
                        SetDate: true,
                        Date: new DateOnly(2020, 12, 25),
                        SetTime: true,
                        Time: new TimeOnly(19, 58, 0)
                    )
                )
            );
            var editor = new DateTimeSetterFilterEditorViewModel(step) { TimeText = "18:14" };

            Assert.Equal(new TimeOnly(18, 14, 0), ((DateTimeSetterFilter)step.Filter).Options.Time);
            Assert.Equal("18:14", editor.TimeText);
        }

        /// <summary>
        /// Verifies far-future years like 3026 are rejected even when time text is incomplete.
        /// </summary>
        [Fact]
        public void Date_time_setter_rejects_far_future_date_with_incomplete_time()
        {
            var step = new AppliedFilterStepViewModel(
                "Date/Time Setter",
                new DateTimeSetterFilter(
                    Options: new DateTimeSetterOptions(
                        TimestampField: TimestampField.LastWrite,
                        SetDate: true,
                        Date: new DateOnly(2026, 9, 5),
                        SetTime: true,
                        Time: new TimeOnly(19, 58, 0)
                    )
                )
            );
            var editor = new DateTimeSetterFilterEditorViewModel(step) { TimeText = "18:1", DateText = "3026-09-05" };

            Assert.Equal("2026-09-05", editor.DateText);
            var options = ((DateTimeSetterFilter)step.Filter).Options;
            Assert.Equal(new DateOnly(2026, 9, 5), options.Date);
            Assert.Equal(new TimeOnly(19, 58, 0), options.Time);
        }

        /// <summary>
        /// Verifies date-only edits still apply when time is unchecked even if time text is illegal.
        /// </summary>
        [Fact]
        public void Date_time_setter_set_date_ignores_illegal_time_when_time_unchecked()
        {
            var step = new AppliedFilterStepViewModel(
                "Date/Time Setter",
                new DateTimeSetterFilter(
                    Options: new DateTimeSetterOptions(
                        TimestampField: TimestampField.LastWrite,
                        SetDate: true,
                        Date: new DateOnly(2020, 12, 25),
                        SetTime: false,
                        Time: new TimeOnly(9, 0, 15)
                    )
                )
            );
            var editor = new DateTimeSetterFilterEditorViewModel(step)
            {
                TimeText = "25:19:01",
                DateText = "2019-01-01",
            };

            var options = ((DateTimeSetterFilter)step.Filter).Options;
            Assert.Equal(new DateOnly(2019, 1, 1), options.Date);
            Assert.False(options.SetTime);
            Assert.Equal(new TimeOnly(9, 0, 15), options.Time);
            Assert.Equal("25:19:01", editor.TimeText);
        }
    }
}
