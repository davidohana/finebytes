using System.Globalization;

namespace Mfr.Filters.Formatting
{
    /// <summary>
    /// Shared token-value formatting for numeric, duration, and boolean properties.
    /// <para>
    /// Used by media, image, and MPEG formatters. Zero means a numeric or duration property is absent.
    /// Boolean flags always emit <c>Yes</c> or <c>No</c>.
    /// </para>
    /// </summary>
    internal static class PropertyValueFormatting
    {
        /// <summary>
        /// Formats a positive integer as invariant digits.
        /// </summary>
        /// <param name="value">Property value; <c>0</c> means absent.</param>
        /// <returns>Invariant digits, or empty when <paramref name="value"/> is <c>0</c>.</returns>
        internal static string PositiveInt(int value)
        {
            if (value == 0)
                return string.Empty;

            return value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Formats a duration as <c>h:mm:ss</c> with total hours unpadded.
        /// </summary>
        /// <param name="duration">Property value; <see cref="TimeSpan.Zero"/> means absent.</param>
        /// <returns>Hours, minutes, and seconds, or empty when <paramref name="duration"/> is zero.</returns>
        internal static string Duration(TimeSpan duration)
        {
            if (duration == TimeSpan.Zero)
                return string.Empty;

            var totalHours = (int)duration.TotalHours;
            return string.Create(
                CultureInfo.InvariantCulture,
                $"{totalHours}:{duration.Minutes:D2}:{duration.Seconds:D2}"
            );
        }

        /// <summary>
        /// Formats a duration as whole seconds.
        /// </summary>
        /// <param name="duration">Property value; <see cref="TimeSpan.Zero"/> means absent.</param>
        /// <returns>
        /// Floored total seconds as invariant digits, or empty when <paramref name="duration"/> is zero.
        /// </returns>
        internal static string DurationSec(TimeSpan duration)
        {
            if (duration == TimeSpan.Zero)
                return string.Empty;

            var seconds = (long)Math.Floor(duration.TotalSeconds);
            return seconds.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Formats a boolean flag as <c>Yes</c> or <c>No</c>.
        /// </summary>
        /// <param name="value">Flag value; these properties are never treated as absent.</param>
        /// <returns>
        /// <c>Yes</c> when <paramref name="value"/> is <see langword="true"/>; otherwise <c>No</c>.
        /// </returns>
        internal static string YesNo(bool value)
        {
            return value ? "Yes" : "No";
        }
    }
}
