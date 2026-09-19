using System.Diagnostics;
using Mfr.Models.Media;
using Mfr.Models.Rename;

namespace Mfr.Models.RenameList.Fields.Jpeg
{
    /// <summary>
    /// Shared base for MFR7 Jpeg Tag (EXIF read-only) Rename List fields.
    /// </summary>
    /// <param name="propertyKey">Property key within the Jpeg group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    /// <param name="tip">Optional tooltip clarifying the field.</param>
    internal abstract class JpegRenameListField(
        string propertyKey,
        string displayName,
        int? defaultWidth = 80,
        string? tip = null
    )
        : OriginalOnlyRenameListField(
            JpegRenameListFields.Group,
            JpegRenameListFields.GroupLabel,
            propertyKey,
            displayName,
            defaultWidth,
            RenameListMetadataRequirement.ImageProperties,
            tip
        );

    /// <summary>
    /// One read-only EXIF column backed by <see cref="ExifData"/>.
    /// </summary>
    /// <param name="propertyKey">Property key within the Jpeg group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="field">EXIF property to format.</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    /// <param name="tip">Optional tooltip clarifying the field.</param>
    internal sealed class JpegExifRenameListField(
        string propertyKey,
        string displayName,
        ExifPropertyField field,
        int? defaultWidth = 80,
        string? tip = null
    ) : JpegRenameListField(propertyKey, displayName, defaultWidth, tip)
    {
        /// <summary>
        /// Gets the EXIF property addressed by this column.
        /// </summary>
        public ExifPropertyField Field { get; } = field;

        /// <summary>
        /// Maps an <see cref="ExifPropertyField"/> to its Rename List catalog property key.
        /// </summary>
        /// <param name="field">EXIF property field.</param>
        /// <returns>Catalog key under <see cref="JpegRenameListFields.Key"/>.</returns>
        internal static string CatalogPropertyKey(ExifPropertyField field)
        {
            return field switch
            {
                ExifPropertyField.Title => JpegRenameListFields.Key.Title,
                ExifPropertyField.Subject => JpegRenameListFields.Key.Subject,
                ExifPropertyField.Author => JpegRenameListFields.Key.Author,
                ExifPropertyField.Keywords => JpegRenameListFields.Key.Keywords,
                ExifPropertyField.Comments => JpegRenameListFields.Key.Comments,
                ExifPropertyField.DateTaken => JpegRenameListFields.Key.DateTaken,
                ExifPropertyField.Make => JpegRenameListFields.Key.Make,
                ExifPropertyField.Model => JpegRenameListFields.Key.Model,
                ExifPropertyField.Description => JpegRenameListFields.Key.Description,
                ExifPropertyField.Artist => JpegRenameListFields.Key.Artist,
                ExifPropertyField.ImageNumber => JpegRenameListFields.Key.ImageNumber,
                ExifPropertyField.UserComment => JpegRenameListFields.Key.UserComment,
                ExifPropertyField.Exposure => JpegRenameListFields.Key.Exposure,
                ExifPropertyField.FNumber => JpegRenameListFields.Key.FNumber,
                ExifPropertyField.Iso => JpegRenameListFields.Key.Iso,
                ExifPropertyField.FocalLength => JpegRenameListFields.Key.FocalLength,
                ExifPropertyField.FocalLength35mm => JpegRenameListFields.Key.FocalLength35mm,
                ExifPropertyField.GpsLatitude => JpegRenameListFields.Key.Latitude,
                ExifPropertyField.GpsLongitude => JpegRenameListFields.Key.Longitude,
                _ => throw new UnreachableException(),
            };
        }

        /// <inheritdoc />
        public override string Resolve(FileMeta meta)
        {
            return ExifDataFormatting.Format(meta.Exif, Field);
        }

        /// <inheritdoc />
        public override int CompareForSort(FileMeta left, FileMeta right)
        {
            if (Field == ExifPropertyField.DateTaken)
            {
                var leftDate = left.Exif?.DateTaken ?? default;
                var rightDate = right.Exif?.DateTaken ?? default;
                return RenameListFieldSortCompare.DateTime(leftDate, rightDate);
            }

            if (Field == ExifPropertyField.GpsLatitude)
            {
                return RenameListFieldSortCompare.Double(
                    left.Exif?.GpsLatitude ?? default,
                    right.Exif?.GpsLatitude ?? default
                );
            }

            if (Field == ExifPropertyField.GpsLongitude)
            {
                return RenameListFieldSortCompare.Double(
                    left.Exif?.GpsLongitude ?? default,
                    right.Exif?.GpsLongitude ?? default
                );
            }

            if (Field == ExifPropertyField.ImageNumber)
            {
                return RenameListFieldSortCompare.ParsedInt64(Resolve(left), Resolve(right));
            }

            return base.CompareForSort(left, right);
        }
    }

    /// <summary>
    /// One read-only Nearby GeoNames column under the Jpeg Tag group.
    /// </summary>
    /// <param name="propertyKey">Property key within the Jpeg group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="field">GeoNames property to format.</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    /// <param name="tip">Optional tooltip clarifying the field.</param>
    internal sealed class JpegNearbyRenameListField(
        string propertyKey,
        string displayName,
        GeoNamesField field,
        int? defaultWidth = 120,
        string? tip = null
    )
        : OriginalOnlyRenameListField(
            JpegRenameListFields.Group,
            JpegRenameListFields.GroupLabel,
            propertyKey,
            displayName,
            defaultWidth,
            RenameListMetadataRequirement.GeoNames,
            tip
        )
    {
        /// <summary>
        /// Gets the GeoNames property addressed by this column.
        /// </summary>
        public GeoNamesField Field { get; } = field;

        /// <summary>
        /// Maps a <see cref="GeoNamesField"/> to its Rename List catalog property key.
        /// </summary>
        /// <param name="field">GeoNames property field.</param>
        /// <returns>Catalog key under <see cref="JpegRenameListFields.Key"/>.</returns>
        internal static string CatalogPropertyKey(GeoNamesField field)
        {
            return field switch
            {
                GeoNamesField.Place => JpegRenameListFields.Key.NearbyPlace,
                GeoNamesField.Region => JpegRenameListFields.Key.NearbyRegion,
                GeoNamesField.Country => JpegRenameListFields.Key.NearbyCountry,
                _ => throw new UnreachableException(),
            };
        }

        /// <inheritdoc />
        public override string Resolve(FileMeta meta)
        {
            return GeoNamesFormatting.Format(meta.GeoNames, Field);
        }
    }
}
