using System.Globalization;
using Mfr.Models.Media;
using Mfr.Utils;

namespace Mfr.Models.RenameList
{
    /// <summary>
    /// Parses Rename List display / log-friendly strings for Extended date WriteTargets.
    /// </summary>
    internal static class RenameListFieldParse
    {
        /// <summary>
        /// Parses a filesystem timestamp from grid display or rename-log round-trip form.
        /// </summary>
        /// <param name="value">
        /// Culture <c>G</c> display text or invariant round-trip <c>O</c>. Blank is rejected — filesystem
        /// timestamps cannot be cleared via Manual Override.
        /// </param>
        /// <returns>Parsed timestamp inside <see cref="FileTimestampDateLimits"/>.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="value"/> is blank, not a recognized date/time, or outside
        /// <see cref="FileTimestampDateLimits.Min"/>..<see cref="FileTimestampDateLimits.Max"/>.
        /// </exception>
        internal static DateTime ParseFileDate(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (value.IsBlank())
            {
                throw new ArgumentException("Date/time value cannot be empty or whitespace.", nameof(value));
            }

            var trimmed = value.Trim();
            if (!_TryParseFileDateText(trimmed, out var parsed))
            {
                throw new ArgumentException($"Unrecognized date/time value '{value}'.", nameof(value));
            }

            if (!FileTimestampDateLimits.IsInRange(DateOnly.FromDateTime(parsed)))
            {
                throw new ArgumentException(
                    $"Date/time '{value}' is outside {FileTimestampDateLimits.Min:yyyy-MM-dd}..{FileTimestampDateLimits.Max:yyyy-MM-dd}.",
                    nameof(value)
                );
            }

            return parsed;
        }

        /// <summary>
        /// Tries culture display, round-trip <c>O</c>, then invariant parse for a trimmed timestamp string.
        /// </summary>
        private static bool _TryParseFileDateText(string trimmed, out DateTime parsed)
        {
            if (
                DateTime.TryParseExact(
                    trimmed,
                    "O",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out parsed
                )
            )
            {
                return true;
            }

            if (DateTime.TryParse(trimmed, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsed))
            {
                return true;
            }

            return DateTime.TryParse(
                trimmed,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind,
                out parsed
            );
        }
    }
}
