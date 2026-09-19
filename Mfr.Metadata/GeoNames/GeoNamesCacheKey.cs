using System.Globalization;

namespace Mfr.Metadata.GeoNames
{
    /// <summary>
    /// Builds L2/L3 cache keys for GeoNames <c>findNearby</c> results.
    /// </summary>
    public static class GeoNamesCacheKey
    {
        /// <summary>
        /// Builds a cache key from rounded coordinates and the effective username.
        /// </summary>
        /// <param name="latitude">GPS latitude in decimal degrees.</param>
        /// <param name="longitude">GPS longitude in decimal degrees.</param>
        /// <param name="effectiveUsername">Resolved GeoNames username (override or bundled).</param>
        /// <returns>Stable invariant key string.</returns>
        public static string Create(double latitude, double longitude, string effectiveUsername)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(effectiveUsername);

            var lat = Math.Round(latitude, 4, MidpointRounding.AwayFromZero)
                .ToString("0.0000", CultureInfo.InvariantCulture);
            var lon = Math.Round(longitude, 4, MidpointRounding.AwayFromZero)
                .ToString("0.0000", CultureInfo.InvariantCulture);
            return string.Concat(lat, "|", lon, "|", effectiveUsername.Trim());
        }
    }
}
