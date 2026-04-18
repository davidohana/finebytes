using System.Diagnostics;
using System.Text.Json.Serialization;
using Mfr.Models;

namespace Mfr.Filters.Attributes
{
    /// <summary>
    /// Which timestamp to set and the time-of-day to apply; the calendar date on the preview timestamp is preserved.
    /// </summary>
    /// <param name="TimestampField">Which filesystem timestamp to set.</param>
    /// <param name="Time">Local time to set.</param>
    public sealed record TimeSetterOptions(
        [property: JsonPropertyName("timestampField")] TimestampField TimestampField,
        [property: JsonPropertyName("time")] TimeOnly Time);

    /// <summary>
    /// Sets the time-of-day for creation, last write, or last access time. Does not change the calendar date part.
    /// </summary>
    /// <param name="Options">Timestamp field and time value.</param>
    public sealed record TimeSetterFilter(
        TimeSetterOptions Options) : BaseFilter
    {
        /// <inheritdoc />
        public override string Type => "TimeSetter";

        /// <inheritdoc />
        protected internal override void ApplyCore(RenameItem item)
        {
            var preview = item.Preview;
            switch (Options.TimestampField)
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
