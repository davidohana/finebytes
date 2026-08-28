using Mfr.Models.Media;
using Mfr.Models.Rename;

namespace Mfr.Models.RenameList.Fields.Image
{
    /// <summary>
    /// Shared base for MFR7 Image Rename List fields.
    /// </summary>
    /// <param name="propertyKey">Property key within the Image group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    public abstract class ImageRenameListField(string propertyKey, string displayName, int? defaultWidth = 40)
        : RenameListField(propertyKey, displayName, defaultWidth, isSortable: false, supportsPreview: false)
    {
        /// <summary>
        /// MFR7 Image property group id.
        /// </summary>
        public const string Group = "Image";

        /// <summary>
        /// User-visible group label in the field shuttle dropdown.
        /// </summary>
        public const string GroupLabel = "Image";

        /// <inheritdoc />
        public sealed override string GroupId => Group;

        /// <inheritdoc />
        public sealed override string GroupDisplayName => GroupLabel;

        /// <inheritdoc />
        public sealed override RenameListFieldMetadataLoad MetadataLoad =>
            RenameListFieldMetadataLoad.ImageProperties;
    }

    /// <summary>
    /// One read-only image property column backed by <see cref="ImageProperties"/>.
    /// </summary>
    /// <param name="propertyKey">Property key within the Image group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="field">Image property to format.</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    public sealed class ImagePropertyRenameListField(
        string propertyKey,
        string displayName,
        ImageRenameListProperty field,
        int? defaultWidth = 40
    ) : ImageRenameListField(propertyKey, displayName, defaultWidth)
    {
        /// <summary>
        /// Gets the image property addressed by this column.
        /// </summary>
        public ImageRenameListProperty Field { get; } = field;

        /// <inheritdoc />
        public override string Resolve(FileMeta meta)
        {
            return ImageRenameListFieldDisplay.Format(meta.Image, Field);
        }
    }

    /// <summary>
    /// Image properties exposed as Rename List columns.
    /// </summary>
    public enum ImageRenameListProperty
    {
        /// <summary>Raster format short name.</summary>
        Format,

        /// <summary>Width in pixels.</summary>
        Width,

        /// <summary>Height in pixels.</summary>
        Height,

        /// <summary>Total bits per pixel.</summary>
        BitDepth,

        /// <summary>Horizontal resolution in DPI.</summary>
        HorizontalResolutionDpi,

        /// <summary>Vertical resolution in DPI.</summary>
        VerticalResolutionDpi,

        /// <summary>Frame count.</summary>
        FrameCount,
    }

    /// <summary>
    /// Formats <see cref="ImageProperties"/> for Rename List image columns.
    /// </summary>
    internal static class ImageRenameListFieldDisplay
    {
        /// <summary>
        /// Formats one image property for grid display.
        /// </summary>
        /// <param name="image">Loaded snapshot, or <see langword="null"/> when unset.</param>
        /// <param name="field">Which property to format.</param>
        /// <returns>Formatted text, or empty when absent.</returns>
        internal static string Format(ImageProperties? image, ImageRenameListProperty field)
        {
            if (image is null)
            {
                return string.Empty;
            }

            return field switch
            {
                ImageRenameListProperty.Format => RenameListFieldDisplay.FormatOptionalText(image.Format),
                ImageRenameListProperty.Width => RenameListFieldDisplay.FormatPositiveInt(image.Width),
                ImageRenameListProperty.Height => RenameListFieldDisplay.FormatPositiveInt(image.Height),
                ImageRenameListProperty.BitDepth => RenameListFieldDisplay.FormatPositiveInt(image.BitDepth),
                ImageRenameListProperty.HorizontalResolutionDpi => RenameListFieldDisplay.FormatDpi(
                    image.HorizontalResolutionDpi
                ),
                ImageRenameListProperty.VerticalResolutionDpi => RenameListFieldDisplay.FormatDpi(
                    image.VerticalResolutionDpi
                ),
                ImageRenameListProperty.FrameCount => RenameListFieldDisplay.FormatPositiveInt(image.FrameCount),
                _ => string.Empty,
            };
        }
    }
}
