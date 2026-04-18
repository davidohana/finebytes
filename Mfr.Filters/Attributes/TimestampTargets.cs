using Mfr.Models;

namespace Mfr.Filters.Attributes
{
    internal static class TimestampTargets
    {
        internal static void Require(FilterTarget target)
        {
            if (target is not (CreationDateTarget or LastWriteDateTarget or LastAccessDateTarget))
            {
                throw new InvalidOperationException(
                    "Date and time setter filters require target family CreationDate, LastWriteDate, or LastAccessDate.");
            }
        }
    }
}
