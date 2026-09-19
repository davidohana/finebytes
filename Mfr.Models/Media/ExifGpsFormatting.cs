using System.Globalization;

namespace Mfr.Models.Media
{
    /// <summary>
    /// Formats typed GPS coordinates for formatter tokens and Rename List columns.
    /// </summary>
    public static class ExifGpsFormatting
    {
        /// <summary>
        /// Formats a GPS latitude or longitude as an invariant decimal string.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Uses up to six decimal places and trims trailing zeros (<c>0.######</c>).
        /// Missing values expand empty.
        /// </para>
        /// </remarks>
        /// <param name="coordinate">Decimal degrees, or <see langword="null"/> when absent.</param>
        /// <returns>Formatted text, or empty when <paramref name="coordinate"/> is null.</returns>
        public static string FormatCoordinate(double? coordinate)
        {
            if (coordinate is not { } value)
            {
                return string.Empty;
            }

            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }
    }
}
