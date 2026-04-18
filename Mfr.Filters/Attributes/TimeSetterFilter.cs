using System.Diagnostics;
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
    /// <param name="Timestamp">Which timestamp field to set.</param>
    /// <param name="Options">Time value for the chosen timestamp field.</param>
    public sealed record TimeSetterFilter(
        [property: JsonPropertyName("timestamp")] TimestampField Timestamp,
        TimeSetterOptions Options) : BaseFilter
    {
        /// <inheritdoc />
        public override string Type => "TimeSetter";

        /// <inheritdoc />
        protected internal override void ApplyCore(RenameItem item)
        {
            var preview = item.Preview;
            switch (Timestamp)
            {
                case TimestampField.Creation:
                    preview.CreationTime = _SetTimePreserveDate(preview.CreationTime, Options.Time);
                    break;
                case TimestampField.LastWrite:
                    preview.LastWriteTime = _SetTimePreserveDate(preview.LastWriteTime, Options.Time);
                    break;
                case TimestampField.LastAccess:
                    preview.LastAccessTime = _SetTimePreserveDate(preview.LastAccessTime, Options.Time);
                    break;
                default:
                    throw new UnreachableException();
            }
        }

        private static DateTime _SetTimePreserveDate(DateTime current, TimeOnly time)
        {
            return current.Date.Add(time.ToTimeSpan());
        }
    }
}
