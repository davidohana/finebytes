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
        /// Verifies illegal clock times are not applied and the text reverts to the full applied value.
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
            Assert.Equal(
                FileTimestampDateLimits.Max.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                editor.DateText
            );
            Assert.Equal(FileTimestampDateLimits.Max, ((DateTimeSetterFilter)step.Filter).Options.Date);
        }

        /// <summary>
        /// Verifies partial date text is restored to the full applied date (never left as <c>5</c>).
        /// </summary>
        [Fact]
        public void Date_time_setter_restores_full_date_for_partial_text()
        {
            var step = new AppliedFilterStepViewModel(
                "Date/Time Setter",
                new DateTimeSetterFilter(
                    Options: new DateTimeSetterOptions(
                        TimestampField: TimestampField.LastWrite,
                        SetDate: true,
                        Date: new DateOnly(2020, 12, 25),
                        SetTime: true,
                        Time: new TimeOnly(11, 11, 11)
                    )
                )
            );
            var editor = new DateTimeSetterFilterEditorViewModel(step) { DateText = "5" };

            Assert.Equal("2020-12-25", editor.DateText);
            Assert.Equal(new DateOnly(2020, 12, 25), ((DateTimeSetterFilter)step.Filter).Options.Date);
            Assert.Equal("11:11:11", editor.TimeText);
        }

        /// <summary>
        /// Verifies incomplete time text restores the full applied time while a valid date still applies.
        /// </summary>
        [Fact]
        public void Date_time_setter_restores_full_time_for_incomplete_text()
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
            Assert.Equal("19:58:00", editor.TimeText);
            Assert.Equal("2026-09-05", editor.DateText);
        }

        /// <summary>
        /// Verifies illegal complete <c>HH:mm</c> times revert like illegal <c>HH:mm:ss</c>.
        /// </summary>
        [Fact]
        public void Date_time_setter_rejects_illegal_hh_mm_and_reverts_text()
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
            var editor = new DateTimeSetterFilterEditorViewModel(step) { TimeText = "25:00" };

            Assert.Equal("23:19:01", editor.TimeText);
            Assert.Equal(new TimeOnly(23, 19, 1), ((DateTimeSetterFilter)step.Filter).Options.Time);
        }

        /// <summary>
        /// Verifies <c>HH:mm</c> time text is accepted and rewritten as <c>HH:mm:ss</c>.
        /// </summary>
        [Fact]
        public void Date_time_setter_accepts_hh_mm_and_shows_full_time()
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
            Assert.Equal("18:14:00", editor.TimeText);
        }

        /// <summary>
        /// Verifies far-future years like 3026 are rejected and incomplete time is restored to full.
        /// </summary>
        [Fact]
        public void Date_time_setter_rejects_far_future_date_and_restores_incomplete_time()
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
            Assert.Equal("19:58:00", editor.TimeText);
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
            // Time is unchecked: text is not validated/rewritten on date apply.
            Assert.Equal("25:19:01", editor.TimeText);
        }

        /// <summary>
        /// Verifies <see cref="DateTimeSetterFilterEditorViewModel.CommitDateText"/> restores partial input.
        /// </summary>
        [Fact]
        public void Date_time_setter_commit_date_restores_partial_text()
        {
            var step = new AppliedFilterStepViewModel(
                "Date/Time Setter",
                new DateTimeSetterFilter(
                    Options: new DateTimeSetterOptions(
                        TimestampField: TimestampField.LastWrite,
                        SetDate: true,
                        Date: new DateOnly(2020, 12, 25),
                        SetTime: true,
                        Time: new TimeOnly(11, 11, 11)
                    )
                )
            );
            var editor = new DateTimeSetterFilterEditorViewModel(step) { DateText = "5" };
            editor.CommitDateText();

            Assert.Equal("2020-12-25", editor.DateText);
            Assert.Equal(new DateOnly(2020, 12, 25), ((DateTimeSetterFilter)step.Filter).Options.Date);
        }
    }
}
