using System.Text.Json.Serialization;

namespace Mfr.Filters.Attributes
{
    /// <summary>
    /// Which timestamp to set and optional calendar date and/or time-of-day values.
    /// </summary>
    /// <param name="TimestampField">Which filesystem timestamp to set.</param>
    /// <param name="SetDate">When true, replace the calendar date; otherwise leave it unchanged.</param>
    /// <param name="Date">Calendar date to set when <paramref name="SetDate"/> is true.</param>
    /// <param name="SetTime">When true, replace the time-of-day; otherwise leave it unchanged.</param>
    /// <param name="Time">Local time to set when <paramref name="SetTime"/> is true.</param>
    public sealed record DateTimeSetterOptions(
        [property: JsonPropertyName("timestampField")] TimestampField TimestampField,
        [property: JsonPropertyName("setDate")] bool SetDate,
        [property: JsonPropertyName("date")] DateOnly Date,
        [property: JsonPropertyName("setTime")] bool SetTime,
        [property: JsonPropertyName("time")] TimeOnly Time
    );

    /// <summary>
    /// Sets the calendar date and/or time-of-day for creation, last write, or last access time.
    /// </summary>
    /// <param name="Options">Timestamp field and optional date/time values.</param>
    /// <remarks>
    /// <para>
    /// Calendar dates must stay in <see cref="FileTimestampDateLimits.Min"/>..<see cref="FileTimestampDateLimits.Max"/>.
    /// Out-of-range dates are ignored at apply time so preview/commit cannot request illegal timestamps.
    /// </para>
    /// </remarks>
    [FilterPalette(FilterGroup.Attributes, "Date/Time Setter")]
    public sealed record DateTimeSetterFilter(DateTimeSetterOptions Options) : BaseFilter
    {
        /// <summary>
        /// Creates a filter with defaults (last write; date and time both on, today/now).
        /// </summary>
        public DateTimeSetterFilter()
            : this(
                new DateTimeSetterOptions(
                    TimestampField: TimestampField.LastWrite,
                    SetDate: true,
                    Date: DateOnly.FromDateTime(DateTime.Today),
                    SetTime: true,
                    Time: TimeOnly.FromDateTime(DateTime.Now)
                )
            ) { }

        /// <inheritdoc />
        public override string Type => "DateTimeSetter";

        /// <inheritdoc />
        protected internal override void ApplyCore(RenameItem item)
        {
            if (!Options.SetDate && !Options.SetTime)
            {
                return;
            }

            if (Options.SetDate && !FileTimestampDateLimits.IsInRange(Options.Date))
            {
                return;
            }

            TimestampFields.Update(item.Preview, Options.TimestampField, _Apply);
        }

        /// <summary>
        /// Applies optional date and/or time replacements while preserving <see cref="DateTime.Kind"/>.
        /// </summary>
        private DateTime _Apply(DateTime current)
        {
            var result = current;
            if (Options.SetDate)
            {
                result = Options.Date.ToDateTime(TimeOnly.FromDateTime(result), result.Kind);
            }

            if (Options.SetTime)
            {
                result = DateOnly.FromDateTime(result).ToDateTime(Options.Time, result.Kind);
            }

            return result;
        }
    }
}
