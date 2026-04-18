using System.Diagnostics;
using System.Text.Json.Serialization;
using Mfr.Models;

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
        Years
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
        [property: JsonPropertyName("unit")] TimeShiftUnit Unit);

    /// <summary>
    /// Shifts creation, last write, or last access time by an amount in the chosen unit.
    /// </summary>
    /// <param name="Options">Timestamp field, amount, and unit.</param>
    public sealed record TimeShifterFilter(
        TimeShifterOptions Options) : BaseFilter
    {
        /// <inheritdoc />
        public override string Type => "TimeShifter";

        /// <inheritdoc />
        protected internal override void ApplyCore(RenameItem item)
        {
            var preview = item.Preview;
            switch (Options.TimestampField)
            {
                case TimestampField.Creation:
                    preview.CreationTime = _Shift(preview.CreationTime, Options.Amount, Options.Unit);
                    break;
                case TimestampField.LastWrite:
                    preview.LastWriteTime = _Shift(preview.LastWriteTime, Options.Amount, Options.Unit);
                    break;
                case TimestampField.LastAccess:
                    preview.LastAccessTime = _Shift(preview.LastAccessTime, Options.Amount, Options.Unit);
                    break;
                default:
                    throw new UnreachableException();
            }
        }

        private static DateTime _Shift(DateTime current, int amount, TimeShiftUnit unit)
        {
            return unit switch
            {
                TimeShiftUnit.Seconds => current.AddSeconds(amount),
                TimeShiftUnit.Minutes => current.AddMinutes(amount),
                TimeShiftUnit.Hours => current.AddHours(amount),
                TimeShiftUnit.Days => current.AddDays(amount),
                TimeShiftUnit.Months => current.AddMonths(amount),
                TimeShiftUnit.Years => current.AddYears(amount),
                _ => throw new UnreachableException()
            };
        }
    }
}
