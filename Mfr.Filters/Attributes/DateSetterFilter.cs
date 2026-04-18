using System.Text.Json.Serialization;
using Mfr.Models;

namespace Mfr.Filters.Attributes
{
    /// <summary>
    /// Date portion to apply; the time-of-day on the preview timestamp is preserved.
    /// </summary>
    /// <param name="Date">Calendar date to set.</param>
    public sealed record DateSetterOptions(
        [property: JsonPropertyName("date")] DateOnly Date);

    /// <summary>
    /// Sets the calendar date for creation, last write, or last access time. Does not change the time-of-day part.
    /// </summary>
    /// <param name="Target">One of <see cref="CreationDateTarget"/>, <see cref="LastWriteDateTarget"/>, <see cref="LastAccessDateTarget"/>.</param>
    /// <param name="Options">Date value for the chosen timestamp field.</param>
    public sealed record DateSetterFilter(
        FilterTarget Target,
        DateSetterOptions Options) : BaseFilter(Target)
    {
        /// <inheritdoc />
        public override string Type => "DateSetter";

        /// <inheritdoc />
        protected override void _Setup()
        {
            TimestampTargets.Require(Target);
        }

        /// <inheritdoc />
        protected internal override void ApplyCore(RenameItem item)
        {
            var preview = item.Preview;
            switch (Target)
            {
                case CreationDateTarget:
                    preview.CreationTime = _SetDatePreserveTime(preview.CreationTime, Options.Date);
                    break;
                case LastWriteDateTarget:
                    preview.LastWriteTime = _SetDatePreserveTime(preview.LastWriteTime, Options.Date);
                    break;
                case LastAccessDateTarget:
                    preview.LastAccessTime = _SetDatePreserveTime(preview.LastAccessTime, Options.Date);
                    break;
                default:
                    throw new InvalidOperationException(
                        "DateSetter requires target family CreationDate, LastWriteDate, or LastAccessDate.");
            }
        }

        private static DateTime _SetDatePreserveTime(DateTime current, DateOnly date)
        {
            return new(
                date.Year,
                date.Month,
                date.Day,
                current.Hour,
                current.Minute,
                current.Second,
                current.Millisecond,
                current.Kind);
        }
    }
}
