using System.Globalization;
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
        private const string TimeFormat = "HH':'mm':'ss";

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
                TimeText = DateTime.Now.ToString(TimeFormat, CultureInfo.InvariantCulture);
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
                TimeText = filter.Options.Time.ToString(TimeFormat, CultureInfo.InvariantCulture);
            });
        }

        private void _ApplyOptions()
        {
            if (IsLoading || SelectedTimestampField is null || Step.Filter is not DateTimeSetterFilter filter)
            {
                return;
            }

            if (
                !DateOnly.TryParseExact(
                    (DateText ?? string.Empty).Trim(),
                    DateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var date
                )
            )
            {
                return;
            }

            if (
                !TimeOnly.TryParseExact(
                    (TimeText ?? string.Empty).Trim(),
                    TimeFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var time
                )
            )
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
