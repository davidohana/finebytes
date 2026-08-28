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
    internal abstract class JpegRenameListField(string propertyKey, string displayName, int? defaultWidth = 80)
        : OriginalOnlyRenameListField(
            JpegRenameListFields.Group,
            JpegRenameListFields.GroupLabel,
            propertyKey,
            displayName,
            defaultWidth,
            RenameListFieldMetadataLoad.ImageProperties
        );

    /// <summary>
    /// One read-only EXIF column backed by <see cref="ExifData"/>.
    /// </summary>
    /// <param name="propertyKey">Property key within the Jpeg group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="field">EXIF property to format.</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    internal sealed class JpegExifRenameListField(
        string propertyKey,
        string displayName,
        JpegRenameListExifProperty field,
        int? defaultWidth = 80
    ) : JpegRenameListField(propertyKey, displayName, defaultWidth)
    {
        /// <summary>
        /// Gets the EXIF property addressed by this column.
        /// </summary>
        public JpegRenameListExifProperty Field { get; } = field;

        /// <inheritdoc />
        public override string Resolve(FileMeta meta)
        {
            return JpegRenameListFieldDisplay.Format(meta.Exif, Field);
        }
    }

    /// <summary>
    /// EXIF properties exposed as MFR7 Jpeg Tag Rename List columns.
    /// </summary>
    internal enum JpegRenameListExifProperty
    {
        /// <summary>Windows XP Title.</summary>
        Title,

        /// <summary>Windows XP Subject.</summary>
        Subject,

        /// <summary>Windows XP Author.</summary>
        Author,

        /// <summary>Windows XP Keywords.</summary>
        Keywords,

        /// <summary>Windows XP Comments.</summary>
        Comments,

        /// <summary>DateTimeOriginal.</summary>
        DateTaken,

        /// <summary>Camera make.</summary>
        Make,

        /// <summary>Camera model.</summary>
        Model,

        /// <summary>Image description.</summary>
        Description,

        /// <summary>IFD0 Artist.</summary>
        Artist,

        /// <summary>SubIFD image number (tag 37393).</summary>
        ImageNumber,

        /// <summary>SubIFD user comment.</summary>
        UserComment,

        /// <summary>Exposure time.</summary>
        Exposure,

        /// <summary>F-number.</summary>
        FNumber,

        /// <summary>ISO speed ratings.</summary>
        Iso,

        /// <summary>Focal length.</summary>
        FocalLength,

        /// <summary>Focal length in 35mm film.</summary>
        FocalLength35mm,
    }

    /// <summary>
    /// Formats <see cref="ExifData"/> for Rename List Jpeg Tag columns.
    /// </summary>
    internal static class JpegRenameListFieldDisplay
    {
        /// <summary>
        /// Formats one EXIF property for grid display.
        /// </summary>
        /// <param name="exif">Loaded snapshot, or <see langword="null"/> when unset.</param>
        /// <param name="field">Which property to format.</param>
        /// <returns>Formatted text, or empty when absent.</returns>
        internal static string Format(ExifData? exif, JpegRenameListExifProperty field)
        {
            if (exif is null)
            {
                return string.Empty;
            }

            return field switch
            {
                JpegRenameListExifProperty.Title => RenameListFieldDisplay.FormatOptionalText(exif.Title),
                JpegRenameListExifProperty.Subject => RenameListFieldDisplay.FormatOptionalText(exif.Subject),
                JpegRenameListExifProperty.Author => RenameListFieldDisplay.FormatOptionalText(exif.Author),
                JpegRenameListExifProperty.Keywords => RenameListFieldDisplay.FormatOptionalText(exif.Keywords),
                JpegRenameListExifProperty.Comments => RenameListFieldDisplay.FormatOptionalText(exif.Comments),
                JpegRenameListExifProperty.DateTaken => RenameListFieldDisplay.FormatExifDateTaken(exif),
                JpegRenameListExifProperty.Make => RenameListFieldDisplay.FormatOptionalText(exif.Make),
                JpegRenameListExifProperty.Model => RenameListFieldDisplay.FormatOptionalText(exif.Model),
                JpegRenameListExifProperty.Description => RenameListFieldDisplay.FormatOptionalText(exif.Description),
                JpegRenameListExifProperty.Artist => RenameListFieldDisplay.FormatOptionalText(exif.Artist),
                JpegRenameListExifProperty.ImageNumber => RenameListFieldDisplay.FormatExifTagId(
                    exif,
                    source: "ExifSub",
                    tagId: 37393
                ),
                JpegRenameListExifProperty.UserComment => RenameListFieldDisplay.FormatOptionalText(exif.UserComment),
                JpegRenameListExifProperty.Exposure => RenameListFieldDisplay.FormatOptionalText(exif.Exposure),
                JpegRenameListExifProperty.FNumber => RenameListFieldDisplay.FormatOptionalText(exif.FNumber),
                JpegRenameListExifProperty.Iso => RenameListFieldDisplay.FormatOptionalText(exif.Iso),
                JpegRenameListExifProperty.FocalLength => RenameListFieldDisplay.FormatOptionalText(exif.FocalLength),
                JpegRenameListExifProperty.FocalLength35mm => RenameListFieldDisplay.FormatOptionalText(
                    exif.FocalLength35mm
                ),
                _ => string.Empty,
            };
        }
    }
}
