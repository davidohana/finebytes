using System.Runtime.ExceptionServices;
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
        /// <remarks>
        /// <para>
        /// Success always stores a non-null <see cref="FileMeta.GeoNames"/> snapshot (empty when no GPS).
        /// Failures are stored as soft load errors and rethrown so a later <c>geo-*</c> token still
        /// surfaces PreviewError instead of expanding empty (empty is a valid no-GPS success).
        /// </para>
        /// </remarks>
        internal static void EnsureGeoNamesLoaded(this RenameItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (item.WasMetadataLoadAttempted(RenameListMetadataRequirement.GeoNames))
            {
                if (item.GetMetadataLoadError(RenameListMetadataRequirement.GeoNames) is { } loadError)
                {
                    ExceptionDispatchInfo.Capture(loadError).Throw();
                }

                return;
            }

            item.MarkMetadataLoadAttempted(RenameListMetadataRequirement.GeoNames);

            try
            {
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
            catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException)
            {
                item.SetMetadataLoadError(RenameListMetadataRequirement.GeoNames, ex);
                throw;
            }
        }
    }
}
