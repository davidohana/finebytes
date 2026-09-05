using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Attributes;
using Mfr.Models.Filters;
using Mfr.Models.Media;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Attributes
{
    /// <summary>
    /// Shared Filter Configuration editor for <see cref="DateSetterFilter"/> and <see cref="TimeSetterFilter"/>.
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
            (IsDateMode, ValuePhrase) = _ResolveMode(step.Filter);
            _selectedTimestampField = s_TimestampFields[1];
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets the timestamp-field combo choices (MFR7 labels).
        /// </summary>
        public IReadOnlyList<TimestampFieldChoice> TimestampFields => s_TimestampFields;

        /// <summary>
        /// Gets whether this editor is editing a date (vs time-of-day).
        /// </summary>
        public bool IsDateMode { get; }

        /// <summary>
        /// Gets whether this editor is editing a time-of-day (vs date).
        /// </summary>
        public bool IsTimeMode => !IsDateMode;

        /// <summary>
        /// Gets the trailing label after the field combo (<c>date to:</c> / <c>time to:</c>).
        /// </summary>
        public string ValuePhrase { get; }

        /// <summary>
        /// Gets or sets which filesystem timestamp to set.
        /// </summary>
        [ObservableProperty]
        private TimestampFieldChoice _selectedTimestampField;

        /// <summary>
        /// Gets or sets the calendar date text (<c>yyyy-MM-dd</c>, date mode only).
        /// </summary>
        [ObservableProperty]
        private string _dateText = string.Empty;

        /// <summary>
        /// Gets or sets the time-of-day text (<c>HH:mm:ss</c>, time mode only).
        /// </summary>
        [ObservableProperty]
        private string _timeText = string.Empty;

        partial void OnSelectedTimestampFieldChanged(TimestampFieldChoice value) => _ApplyOptions();

        partial void OnDateTextChanged(string value) => _ApplyOptions();

        partial void OnTimeTextChanged(string value) => _ApplyOptions();

        /// <summary>
        /// Sets the value control to today (date) or now (time), matching MFR7 Current.
        /// </summary>
        [RelayCommand]
        public void SetCurrent()
        {
            if (IsDateMode)
            {
                DateText = DateTime.Today.ToString(DateFormat, CultureInfo.InvariantCulture);
                return;
            }

            TimeText = DateTime.Now.ToString(TimeFormat, CultureInfo.InvariantCulture);
        }

        private void _SyncFromFilter()
        {
            LoadWithoutApplying(() =>
            {
                if (Step.Filter is DateSetterFilter dateSetter)
                {
                    SelectedTimestampField = _ChoiceFor(dateSetter.Options.TimestampField);
                    DateText = dateSetter.Options.Date.ToString(DateFormat, CultureInfo.InvariantCulture);
                    return;
                }

                if (Step.Filter is TimeSetterFilter timeSetter)
                {
                    SelectedTimestampField = _ChoiceFor(timeSetter.Options.TimestampField);
                    TimeText = timeSetter.Options.Time.ToString(TimeFormat, CultureInfo.InvariantCulture);
                }
            });
        }

        private void _ApplyOptions()
        {
            if (IsLoading || SelectedTimestampField is null)
            {
                return;
            }

            if (Step.Filter is DateSetterFilter dateSetter)
            {
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

                var options = new DateSetterOptions(
                    TimestampField: SelectedTimestampField.Field,
                    Date: date
                );
                ApplyIfChanged(dateSetter, dateSetter with { Options = options });
                return;
            }

            if (Step.Filter is TimeSetterFilter timeSetter)
            {
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

                var options = new TimeSetterOptions(
                    TimestampField: SelectedTimestampField.Field,
                    Time: time
                );
                ApplyIfChanged(timeSetter, timeSetter with { Options = options });
            }
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

        private static (bool IsDateMode, string ValuePhrase) _ResolveMode(BaseFilter filter)
        {
            return filter switch
            {
                DateSetterFilter => (true, "date to:"),
                TimeSetterFilter => (false, "time to:"),
                _ => throw new InvalidOperationException(
                    $"Date/time setter editor does not support {filter.GetType().Name}."
                ),
            };
        }
    }
}
