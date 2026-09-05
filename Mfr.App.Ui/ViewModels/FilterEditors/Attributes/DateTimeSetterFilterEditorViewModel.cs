using System.Globalization;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Attributes;
using Mfr.Models.Media;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Attributes
{
    /// <summary>
    /// Filter Configuration editor for <see cref="DateTimeSetterFilter"/>.
    /// </summary>
    internal sealed partial class DateTimeSetterFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        private const string DateFormat = "yyyy-MM-dd";
        private const string TimeFormatWithSeconds = "HH':'mm':'ss";
        private const string TimeFormatMinutes = "HH':'mm";

        private static readonly string[] s_TimeFormats = [TimeFormatWithSeconds, TimeFormatMinutes];

        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        public DateTimeSetterFilterEditorViewModel(AppliedFilterStepViewModel step)
            : base(step)
        {
            _selectedTimestampField = TimestampFieldChoice.For(TimestampField.LastWrite);
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets the timestamp-field combo choices (MFR7 labels).
        /// </summary>
        public IReadOnlyList<TimestampFieldChoice> TimestampFields => TimestampFieldChoice.All;

        /// <summary>
        /// Gets or sets which filesystem timestamp to set.
        /// </summary>
        [ObservableProperty]
        private TimestampFieldChoice _selectedTimestampField;

        /// <summary>
        /// Gets or sets whether the calendar date is applied.
        /// </summary>
        [ObservableProperty]
        private bool _setDate;

        /// <summary>
        /// Gets or sets the calendar date text (<c>yyyy-MM-dd</c>).
        /// </summary>
        [ObservableProperty]
        private string _dateText = string.Empty;

        /// <summary>
        /// Gets or sets whether the time-of-day is applied.
        /// </summary>
        [ObservableProperty]
        private bool _setTime;

        /// <summary>
        /// Gets or sets the time-of-day text (<c>HH:mm:ss</c>).
        /// </summary>
        [ObservableProperty]
        private string _timeText = string.Empty;

        partial void OnSelectedTimestampFieldChanged(TimestampFieldChoice value) => _ApplyOptions();

        partial void OnSetDateChanged(bool value) => _ApplyOptions();

        partial void OnDateTextChanged(string value) => _ApplyOptions();

        partial void OnSetTimeChanged(bool value) => _ApplyOptions();

        partial void OnTimeTextChanged(string value) => _ApplyOptions();

        /// <summary>
        /// Sets enabled value fields to today and/or now.
        /// </summary>
        [RelayCommand]
        public void SetCurrent()
        {
            if (SetDate)
            {
                DateText = DateTime.Today.ToString(DateFormat, CultureInfo.InvariantCulture);
            }

            if (SetTime)
            {
                TimeText = DateTime.Now.ToString(TimeFormatWithSeconds, CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Commits <see cref="DateText"/>: apply when valid, otherwise restore the full applied date.
        /// </summary>
        /// <remarks>
        /// Call from the date TextBox <c>LostFocus</c> handler so partial edits cannot remain visible.
        /// </remarks>
        public void CommitDateText()
        {
            if (IsLoading || Step.Filter is not DateTimeSetterFilter filter)
            {
                return;
            }

            var date = filter.Options.Date;
            if (_TryParseDate(DateText, out var parsed))
            {
                date = parsed;
                _SetDateTextCanonical(date);
            }
            else
            {
                _SetDateTextCanonical(filter.Options.Date);
            }

            _ApplyResolved(filter, date, filter.Options.Time);
        }

        /// <summary>
        /// Commits <see cref="TimeText"/>: apply when valid, otherwise restore the full applied time.
        /// </summary>
        /// <remarks>
        /// Call from the time TextBox <c>LostFocus</c> handler so partial edits cannot remain visible.
        /// Accepted <c>HH:mm</c> input is rewritten to <c>HH:mm:ss</c>.
        /// </remarks>
        public void CommitTimeText()
        {
            if (IsLoading || Step.Filter is not DateTimeSetterFilter filter)
            {
                return;
            }

            var time = filter.Options.Time;
            if (_TryParseTime(TimeText, out var parsed))
            {
                time = parsed;
                _SetTimeTextCanonical(time);
            }
            else
            {
                _SetTimeTextCanonical(filter.Options.Time);
            }

            _ApplyResolved(filter, filter.Options.Date, time);
        }

        /// <summary>
        /// Copies current filter options into editor properties without live replace.
        /// </summary>
        private void _SyncFromFilter()
        {
            LoadWithoutApplying(() =>
            {
                if (Step.Filter is not DateTimeSetterFilter filter)
                {
                    return;
                }

                SelectedTimestampField = TimestampFieldChoice.For(filter.Options.TimestampField);
                SetDate = filter.Options.SetDate;
                DateText = filter.Options.Date.ToString(DateFormat, CultureInfo.InvariantCulture);
                SetTime = filter.Options.SetTime;
                TimeText = filter.Options.Time.ToString(TimeFormatWithSeconds, CultureInfo.InvariantCulture);
            });
        }

        /// <summary>
        /// Parses enabled date/time text and replaces the step filter when options change.
        /// </summary>
        /// <remarks>
        /// Text boxes commit on LostFocus; once the VM sees text it must be a full canonical value —
        /// partial or illegal input is restored to the last applied date/time.
        /// </remarks>
        private void _ApplyOptions()
        {
            if (IsLoading || SelectedTimestampField is null || Step.Filter is not DateTimeSetterFilter filter)
            {
                return;
            }

            // Resolve each enabled field independently so a bad sibling cannot block a valid edit.
            var date = filter.Options.Date;
            var time = filter.Options.Time;
            if (SetDate)
            {
                if (_TryParseDate(DateText, out var parsedDate))
                {
                    date = parsedDate;
                    _SetDateTextCanonical(date);
                }
                else
                {
                    _SetDateTextCanonical(filter.Options.Date);
                }
            }

            if (SetTime)
            {
                if (_TryParseTime(TimeText, out var parsedTime))
                {
                    time = parsedTime;
                    _SetTimeTextCanonical(time);
                }
                else
                {
                    _SetTimeTextCanonical(filter.Options.Time);
                }
            }

            _ApplyResolved(filter, date, time);
        }

        private void _ApplyResolved(DateTimeSetterFilter filter, DateOnly date, TimeOnly time)
        {
            if (SelectedTimestampField is null)
            {
                return;
            }

            var options = new DateTimeSetterOptions(
                TimestampField: SelectedTimestampField.Field,
                SetDate: SetDate,
                Date: date,
                SetTime: SetTime,
                Time: time
            );
            ApplyIfChanged(filter, filter with { Options = options });
        }

        private static bool _TryParseDate(string? text, out DateOnly date)
        {
            return DateOnly.TryParseExact(
                    (text ?? string.Empty).Trim(),
                    DateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out date
                ) && FileTimestampDateLimits.IsInRange(date);
        }

        private static bool _TryParseTime(string? text, out TimeOnly time)
        {
            return TimeOnly.TryParseExact(
                (text ?? string.Empty).Trim(),
                s_TimeFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out time
            );
        }

        private void _SetDateTextCanonical(DateOnly date)
        {
            var canonical = date.ToString(DateFormat, CultureInfo.InvariantCulture);
            if (DateText == canonical)
            {
                return;
            }

            LoadWithoutApplying(() => DateText = canonical);
            _NudgeBoundText(() => DateText, value => DateText = value, canonical);
        }

        private void _SetTimeTextCanonical(TimeOnly time)
        {
            var canonical = time.ToString(TimeFormatWithSeconds, CultureInfo.InvariantCulture);
            if (TimeText == canonical)
            {
                return;
            }

            LoadWithoutApplying(() => TimeText = canonical);
            _NudgeBoundText(() => TimeText, value => TimeText = value, canonical);
        }

        /// <summary>
        /// Re-pushes a restored value after the current TwoWay TextBox write settles.
        /// </summary>
        /// <remarks>
        /// Avalonia can ignore a same-stack source write while applying a TextBox edit; posting a
        /// clear-then-restore forces the bound control to show the reverted value.
        /// </remarks>
        private void _NudgeBoundText(Func<string> getText, Action<string> setText, string restored)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (IsLoading)
                {
                    return;
                }

                LoadWithoutApplying(() =>
                {
                    if (getText() != restored)
                    {
                        setText(restored);
                        return;
                    }

                    setText(string.Empty);
                    setText(restored);
                });
            });
        }
    }
}
