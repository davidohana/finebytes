using System.Diagnostics;
using System.Text.Json.Serialization;
using Mfr.Models;

namespace Mfr.Filters.Attributes
{
    /// <summary>
    /// Which timestamp to set and the calendar date to apply; the time-of-day on the preview timestamp is preserved.
    /// </summary>
    /// <param name="TimestampField">Which filesystem timestamp to set.</param>
    /// <param name="Date">Calendar date to set.</param>
    public sealed record DateSetterOptions(
        [property: JsonPropertyName("timestampField")] TimestampField TimestampField,
        [property: JsonPropertyName("date")] DateOnly Date);

    /// <summary>
    /// Sets the calendar date for creation, last write, or last access time. Does not change the time-of-day part.
    /// </summary>
    /// <param name="Options">Timestamp field and date value.</param>
    public sealed record DateSetterFilter(
        DateSetterOptions Options) : BaseFilter
    {
        /// <inheritdoc />
        public override string Type => "DateSetter";

        /// <inheritdoc />
        protected internal override void ApplyCore(RenameItem item)
        {
            var preview = item.Preview;
            switch (Options.TimestampField)
            {
                case TimestampField.Creation:
                    preview.CreationTime = _SetDatePreserveTime(preview.CreationTime, Options.Date);
                    break;
                case TimestampField.LastWrite:
                    preview.LastWriteTime = _SetDatePreserveTime(preview.LastWriteTime, Options.Date);
                    break;
                case TimestampField.LastAccess:
                    preview.LastAccessTime = _SetDatePreserveTime(preview.LastAccessTime, Options.Date);
                    break;
                default:
                    throw new UnreachableException();
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
