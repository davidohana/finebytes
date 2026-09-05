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
        private const int DateTextLength = 10;
        private const int TimeTextLengthMinutes = 5;
        private const int TimeTextLengthWithSeconds = 8;

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
        /// Gets or sets the time-of-day text (<c>HH:mm:ss</c> or <c>HH:mm</c>).
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
        private void _ApplyOptions()
        {
            if (IsLoading || SelectedTimestampField is null || Step.Filter is not DateTimeSetterFilter filter)
            {
                return;
            }

            // Resolve each enabled field independently so an incomplete sibling cannot block a valid edit
            // (e.g. date 2026-09-05 must still apply while time text is mid-edit "18:1").
            var date = filter.Options.Date;
            var time = filter.Options.Time;
            if (SetDate)
            {
                _ResolveDate(filter, ref date);
            }

            if (SetTime)
            {
                _ResolveTime(filter, ref time);
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

        /// <summary>
        /// Parses <see cref="DateText"/> into a supported file date, or reverts complete illegal input.
        /// </summary>
        private void _ResolveDate(DateTimeSetterFilter filter, ref DateOnly date)
        {
            var text = (DateText ?? string.Empty).Trim();
            if (
                DateOnly.TryParseExact(
                    text,
                    DateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsed
                ) && FileTimestampDateLimits.IsInRange(parsed)
            )
            {
                date = parsed;
                return;
            }

            if (text.Length >= DateTextLength)
            {
                _RevertBoundText(
                    () => DateText,
                    value => DateText = value,
                    filter.Options.Date.ToString(DateFormat, CultureInfo.InvariantCulture),
                    text
                );
            }
        }

        /// <summary>
        /// Parses <see cref="TimeText"/> as <c>HH:mm:ss</c> or <c>HH:mm</c>, or reverts complete illegal input.
        /// </summary>
        private void _ResolveTime(DateTimeSetterFilter filter, ref TimeOnly time)
        {
            var text = (TimeText ?? string.Empty).Trim();
            if (
                TimeOnly.TryParseExact(
                    text,
                    s_TimeFormats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsed
                )
            )
            {
                time = parsed;
                return;
            }

            var looksComplete = text.Length is TimeTextLengthMinutes or >= TimeTextLengthWithSeconds;
            if (looksComplete)
            {
                _RevertBoundText(
                    () => TimeText,
                    value => TimeText = value,
                    filter.Options.Time.ToString(TimeFormatWithSeconds, CultureInfo.InvariantCulture),
                    text
                );
            }
        }

        /// <summary>
        /// Restores bound TextBox text after complete illegal input, without clobbering a newer edit.
        /// </summary>
        /// <remarks>
        /// Avalonia can ignore a same-stack source write while applying a TextBox edit; posting a
        /// clear-then-restore forces the bound control to show the reverted value when it is still
        /// showing the rejected text. If the user typed again before the post runs, leave that text.
        /// </remarks>
        private void _RevertBoundText(Func<string> getText, Action<string> setText, string restored, string rejected)
        {
            LoadWithoutApplying(() => setText(restored));
            Dispatcher.UIThread.Post(() =>
            {
                if (IsLoading)
                {
                    return;
                }

                LoadWithoutApplying(() =>
                {
                    var current = getText();
                    if (current != restored && current != rejected)
                    {
                        return;
                    }

                    if (current == restored)
                    {
                        setText(string.Empty);
                        setText(restored);
                        return;
                    }

                    setText(restored);
                });
            });
        }
    }
}
