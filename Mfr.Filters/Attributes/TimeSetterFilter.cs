using System.Text.Json.Serialization;
using Mfr.Models;

namespace Mfr.Filters.Attributes
{
    /// <summary>
    /// Time-of-day to apply; the calendar date on the preview timestamp is preserved.
    /// </summary>
    /// <param name="Time">Local time to set.</param>
    public sealed record TimeSetterOptions(
        [property: JsonPropertyName("time")] TimeOnly Time);

    /// <summary>
    /// Sets the time-of-day for creation, last write, or last access time. Does not change the calendar date part.
    /// </summary>
    /// <param name="Target">One of <see cref="CreationDateTarget"/>, <see cref="LastWriteDateTarget"/>, <see cref="LastAccessDateTarget"/>.</param>
    /// <param name="Options">Time value for the chosen timestamp field.</param>
    public sealed record TimeSetterFilter(
        FilterTarget Target,
        TimeSetterOptions Options) : BaseFilter(Target)
    {
        /// <inheritdoc />
        public override string Type => "TimeSetter";

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
                    preview.CreationTime = _SetTimePreserveDate(preview.CreationTime, Options.Time);
                    break;
                case LastWriteDateTarget:
                    preview.LastWriteTime = _SetTimePreserveDate(preview.LastWriteTime, Options.Time);
                    break;
                case LastAccessDateTarget:
                    preview.LastAccessTime = _SetTimePreserveDate(preview.LastAccessTime, Options.Time);
                    break;
                default:
                    throw new InvalidOperationException(
                        "TimeSetter requires target family CreationDate, LastWriteDate, or LastAccessDate.");
            }
        }

        private static DateTime _SetTimePreserveDate(DateTime current, TimeOnly time)
        {
            return current.Date.Add(time.ToTimeSpan());
        }
    }
}
