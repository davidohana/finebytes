using Mfr.Metadata.GeoNames;
using Mfr.Models.RenameList;
using Mfr.Utils;

namespace Mfr.Filters
{
    /// <summary>
    /// Lazily loads GeoNames nearby place data onto rename rows for formatter tokens.
    /// </summary>
    internal static class RenameItemGeoNamesExtensions
    {
        /// <summary>
        /// Ensures <see cref="RenameItem.Original"/> carries GeoNames nearby data for the row GPS.
        /// </summary>
        /// <param name="item">Rename row to load.</param>
        /// <exception cref="InvalidOperationException">Directory row, image/EXIF failure, or GeoNames failure.</exception>
        /// <exception cref="GeoNamesRateLimitException">GeoNames quota/rate limit (or tripped breaker).</exception>
        internal static void EnsureGeoNamesLoaded(this RenameItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (item.WasMetadataLoadAttempted(RenameListMetadataRequirement.GeoNames))
            {
                return;
            }

            item.MarkMetadataLoadAttempted(RenameListMetadataRequirement.GeoNames);

            if (item.Original.Attributes.IsDirectory())
            {
                throw new InvalidOperationException("Cannot read GeoNames data for a directory.");
            }

            item.EnsureImagePropertiesLoaded();

            var latitude = item.Original.Exif?.GpsLatitude;
            var longitude = item.Original.Exif?.GpsLongitude;
            if (latitude is null || longitude is null)
            {
                item.SetGeoNamesInfo(new GeoNamesInfo());
                return;
            }

            item.SetGeoNamesInfo(GeoNamesClient.Shared.FindNearby(latitude.Value, longitude.Value));
        }
    }
}
