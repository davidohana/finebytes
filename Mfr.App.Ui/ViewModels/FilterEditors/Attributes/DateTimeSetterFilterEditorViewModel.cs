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
        private const int TimeTextLengthWithSeconds = 8;

        private static readonly string[] s_TimeFormats = [TimeFormatWithSeconds, TimeFormatMinutes];

        private static readonly IReadOnlyList<TimestampFieldChoice> s_TimestampFields =
        [
            new(TimestampField.Creation, "Creation"),
            new(TimestampField.LastWrite, "Last Write"),
            new(TimestampField.LastAccess, "Last Access"),
        ];

        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        public DateTimeSetterFilterEditorViewModel(AppliedFilterStepViewModel step)
            : base(step)
        {
            _selectedTimestampField = s_TimestampFields[1];
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets the timestamp-field combo choices (MFR7 labels).
        /// </summary>
        public IReadOnlyList<TimestampFieldChoice> TimestampFields => s_TimestampFields;

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

        private void _SyncFromFilter()
        {
            LoadWithoutApplying(() =>
            {
                if (Step.Filter is not DateTimeSetterFilter filter)
                {
                    return;
                }

                SelectedTimestampField = _ChoiceFor(filter.Options.TimestampField);
                SetDate = filter.Options.SetDate;
                DateText = filter.Options.Date.ToString(DateFormat, CultureInfo.InvariantCulture);
                SetTime = filter.Options.SetTime;
                TimeText = filter.Options.Time.ToString(TimeFormatWithSeconds, CultureInfo.InvariantCulture);
            });
        }

        private void _ApplyOptions()
        {
            if (IsLoading || SelectedTimestampField is null || Step.Filter is not DateTimeSetterFilter filter)
            {
                return;
            }

            // Resolve each enabled field independently so an incomplete sibling cannot block a valid edit
            // (e.g. date 2026-09-05 must still apply while time text is mid-edit "18:14").
            var date = filter.Options.Date;
            var time = filter.Options.Time;
            if (SetDate)
            {
                _ = _TryResolveDate(filter, ref date);
            }

            if (SetTime)
            {
                _ = _TryResolveTime(filter, ref time);
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
        /// <returns><see langword="false"/> when the text is incomplete or was just reverted.</returns>
        private bool _TryResolveDate(DateTimeSetterFilter filter, ref DateOnly date)
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
                return true;
            }

            if (text.Length >= DateTextLength)
            {
                _RevertDateText(filter);
            }

            return false;
        }

        /// <summary>
        /// Parses <see cref="TimeText"/> as <c>HH:mm:ss</c> or <c>HH:mm</c>, or reverts complete illegal input.
        /// </summary>
        /// <returns><see langword="false"/> when the text is incomplete or was just reverted.</returns>
        private bool _TryResolveTime(DateTimeSetterFilter filter, ref TimeOnly time)
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
                return true;
            }

            if (text.Length >= TimeTextLengthWithSeconds)
            {
                _RevertTimeText(filter);
            }

            return false;
        }

        private void _RevertDateText(DateTimeSetterFilter filter)
        {
            var restored = filter.Options.Date.ToString(DateFormat, CultureInfo.InvariantCulture);
            LoadWithoutApplying(() => DateText = restored);
            _NudgeBoundText(() => DateText, value => DateText = value, restored);
        }

        private void _RevertTimeText(DateTimeSetterFilter filter)
        {
            var restored = filter.Options.Time.ToString(TimeFormatWithSeconds, CultureInfo.InvariantCulture);
            LoadWithoutApplying(() => TimeText = restored);
            _NudgeBoundText(() => TimeText, value => TimeText = value, restored);
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

        private static TimestampFieldChoice _ChoiceFor(TimestampField field)
        {
            foreach (var choice in s_TimestampFields)
            {
                if (choice.Field == field)
                {
                    return choice;
                }
            }

            return s_TimestampFields[1];
        }
    }
}
