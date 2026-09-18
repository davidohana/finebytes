using System.Diagnostics;
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
    internal abstract class ImageRenameListField(string propertyKey, string displayName, int? defaultWidth = 40)
        : OriginalOnlyRenameListField(
            ImageRenameListFields.Group,
            ImageRenameListFields.GroupLabel,
            propertyKey,
            displayName,
            defaultWidth,
            RenameListMetadataRequirement.ImageProperties
        );

    /// <summary>
    /// One read-only image property column backed by <see cref="ImageProperties"/>.
    /// </summary>
    /// <param name="propertyKey">Property key within the Image group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="field">Image property to format.</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    internal sealed class ImagePropertyRenameListField(
        string propertyKey,
        string displayName,
        ImagePropertyField field,
        int? defaultWidth = 40
    ) : ImageRenameListField(propertyKey, displayName, defaultWidth)
    {
        /// <summary>
        /// Gets the image property addressed by this column.
        /// </summary>
        public ImagePropertyField Field { get; } = field;

        /// <summary>
        /// Maps an <see cref="ImagePropertyField"/> to its Rename List catalog property key.
        /// </summary>
        /// <param name="field">Image property field.</param>
        /// <returns>Catalog key under <see cref="ImageRenameListFields.Key"/>.</returns>
        internal static string CatalogPropertyKey(ImagePropertyField field)
        {
            return field switch
            {
                ImagePropertyField.Format => ImageRenameListFields.Key.Format,
                ImagePropertyField.Width => ImageRenameListFields.Key.Width,
                ImagePropertyField.Height => ImageRenameListFields.Key.Height,
                ImagePropertyField.BitDepth => ImageRenameListFields.Key.BitDepth,
                ImagePropertyField.HorizontalResolutionDpi => ImageRenameListFields.Key.HorzRes,
                ImagePropertyField.VerticalResolutionDpi => ImageRenameListFields.Key.VertRes,
                ImagePropertyField.FrameCount => ImageRenameListFields.Key.Frames,
                _ => throw new UnreachableException(),
            };
        }

        /// <inheritdoc />
        public override string Resolve(FileMeta meta)
        {
            return ImagePropertiesFormatting.Format(meta.Image, Field, PropertyDisplayContext.Grid);
        }

        /// <inheritdoc />
        public override int CompareForSort(FileMeta left, FileMeta right)
        {
            var leftImage = left.Image;
            var rightImage = right.Image;
            return Field switch
            {
                ImagePropertyField.Width => RenameListFieldSortCompare.Int32(
                    leftImage?.Width ?? 0,
                    rightImage?.Width ?? 0
                ),
                ImagePropertyField.Height => RenameListFieldSortCompare.Int32(
                    leftImage?.Height ?? 0,
                    rightImage?.Height ?? 0
                ),
                ImagePropertyField.BitDepth => RenameListFieldSortCompare.Int32(
                    leftImage?.BitDepth ?? 0,
                    rightImage?.BitDepth ?? 0
                ),
                ImagePropertyField.HorizontalResolutionDpi => RenameListFieldSortCompare.Double(
                    leftImage?.HorizontalResolutionDpi ?? 0,
                    rightImage?.HorizontalResolutionDpi ?? 0
                ),
                ImagePropertyField.VerticalResolutionDpi => RenameListFieldSortCompare.Double(
                    leftImage?.VerticalResolutionDpi ?? 0,
                    rightImage?.VerticalResolutionDpi ?? 0
                ),
                ImagePropertyField.FrameCount => RenameListFieldSortCompare.Int32(
                    leftImage?.FrameCount ?? 0,
                    rightImage?.FrameCount ?? 0
                ),
                ImagePropertyField.Format => base.CompareForSort(left, right),
                _ => throw new UnreachableException(),
            };
        }
    }
}
