using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Mfr.Filters.Attributes
{
    /// <summary>
    /// Unit for shifting a filesystem timestamp by an integer amount.
    /// </summary>
    public enum TimeShiftUnit
    {
        /// <summary>
        /// Seconds.
        /// </summary>
        [JsonStringEnumMemberName("seconds")]
        Seconds,

        /// <summary>
        /// Minutes.
        /// </summary>
        [JsonStringEnumMemberName("minutes")]
        Minutes,

        /// <summary>
        /// Hours.
        /// </summary>
        [JsonStringEnumMemberName("hours")]
        Hours,

        /// <summary>
        /// Days.
        /// </summary>
        [JsonStringEnumMemberName("days")]
        Days,

        /// <summary>
        /// Calendar months.
        /// </summary>
        [JsonStringEnumMemberName("months")]
        Months,

        /// <summary>
        /// Calendar years.
        /// </summary>
        [JsonStringEnumMemberName("years")]
        Years,
    }

    /// <summary>
    /// Which timestamp to shift and the signed amount and unit.
    /// </summary>
    /// <param name="TimestampField">Which filesystem timestamp to shift.</param>
    /// <param name="Amount">Positive shifts forward; negative shifts backward.</param>
    /// <param name="Unit">How to interpret <paramref name="Amount"/>.</param>
    public sealed record TimeShifterOptions(
        [property: JsonPropertyName("timestampField")] TimestampField TimestampField,
        [property: JsonPropertyName("amount")] int Amount,
        [property: JsonPropertyName("unit")] TimeShiftUnit Unit
    );

    /// <summary>
    /// Shifts creation, last write, or last access time by an amount in the chosen unit.
    /// </summary>
    /// <param name="Options">Timestamp field, amount, and unit.</param>
    /// <remarks>
    /// <para>
    /// Shifted calendar dates are clamped to <see cref="FileTimestampDateLimits.Min"/>..<see cref="FileTimestampDateLimits.Max"/>.
    /// When <c>DateTime.Add*</c> throws (amount too large for the unit or result outside <see cref="DateTime"/>),
    /// the field is set to the nearer product-range endpoint with the current time-of-day preserved.
    /// </para>
    /// </remarks>
    [FilterPalette(FilterGroup.Attributes, "Time Shifter")]
    public sealed record TimeShifterFilter(TimeShifterOptions Options) : BaseFilter
    {
        /// <summary>
        /// Creates a filter with MFR7 add-to-list defaults (last write time, +1 day).
        /// </summary>
        public TimeShifterFilter()
            : this(
                new TimeShifterOptions(TimestampField: TimestampField.LastWrite, Amount: 1, Unit: TimeShiftUnit.Days)
            ) { }

        /// <inheritdoc />
        public override string Type => "TimeShifter";

        /// <inheritdoc />
        protected internal override void ApplyCore(RenameItem item)
        {
            TimestampFields.Update(
                item.Preview,
                Options.TimestampField,
                current => _Shift(current, Options.Amount, Options.Unit)
            );
        }

        /// <summary>
        /// Applies the unit shift, then clamps into the product file-timestamp date range.
        /// </summary>
        private static DateTime _Shift(DateTime current, int amount, TimeShiftUnit unit)
        {
            if (amount == 0)
            {
                return current;
            }

            DateTime shifted;
            try
            {
                shifted = unit switch
                {
                    TimeShiftUnit.Seconds => current.AddSeconds(amount),
                    TimeShiftUnit.Minutes => current.AddMinutes(amount),
                    TimeShiftUnit.Hours => current.AddHours(amount),
                    TimeShiftUnit.Days => current.AddDays(amount),
                    TimeShiftUnit.Months => current.AddMonths(amount),
                    TimeShiftUnit.Years => current.AddYears(amount),
                    _ => throw new UnreachableException(),
                };
            }
            catch (ArgumentOutOfRangeException)
            {
                return FileTimestampDateLimits.AtBound(
                    towardMax: amount > 0,
                    time: TimeOnly.FromDateTime(current),
                    kind: current.Kind
                );
            }

            return FileTimestampDateLimits.Clamp(shifted);
        }
    }
}
