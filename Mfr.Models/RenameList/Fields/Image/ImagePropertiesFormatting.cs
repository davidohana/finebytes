using System.Diagnostics;
using Mfr.Models.Media;

namespace Mfr.Models.RenameList.Fields.Image
{
    /// <summary>
    /// Formats <see cref="ImageProperties"/> for formatter tokens and Rename List columns.
    /// </summary>
    internal static class ImagePropertiesFormatting
    {
        /// <summary>
        /// Formats one image property for display.
        /// </summary>
        /// <param name="image">Loaded snapshot, or <see langword="null"/> when unset.</param>
        /// <param name="field">Which property to format.</param>
        /// <param name="context">
        /// Display surface. Image arms are currently identical for Token and Grid; the
        /// parameter is required so Image and PDF share one formatter signature.
        /// </param>
        /// <returns>Formatted text, or empty when absent.</returns>
        internal static string Format(ImageProperties? image, ImagePropertyField field, PropertyDisplayContext context)
        {
            // No Token vs Grid fork for Image today; keep the parameter for signature parity with PDF.
            _ = context;

            if (image is null)
            {
                return string.Empty;
            }

            return field switch
            {
                ImagePropertyField.Format => RenameListFieldDisplay.FormatOptionalText(image.Format),
                ImagePropertyField.Width => RenameListFieldDisplay.FormatPositiveInt(image.Width),
                ImagePropertyField.Height => RenameListFieldDisplay.FormatPositiveInt(image.Height),
                ImagePropertyField.BitDepth => RenameListFieldDisplay.FormatPositiveInt(image.BitDepth),
                ImagePropertyField.HorizontalResolutionDpi => RenameListFieldDisplay.FormatDpi(
                    image.HorizontalResolutionDpi
                ),
                ImagePropertyField.VerticalResolutionDpi => RenameListFieldDisplay.FormatDpi(
                    image.VerticalResolutionDpi
                ),
                ImagePropertyField.FrameCount => RenameListFieldDisplay.FormatPositiveInt(image.FrameCount),
                _ => throw new UnreachableException(),
            };
        }
    }
}
