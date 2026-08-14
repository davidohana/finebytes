using System.Globalization;
using Mfr.Utils;

namespace Mfr.Filters.Formatting.Tokens.Image
{
    /// <summary>
    /// Formats <see cref="ImageProperties"/> fields for formatter tokens.
    /// </summary>
    internal static class ImagePropertiesFormatting
    {
        /// <summary>
        /// Formats an image property field for token expansion.
        /// </summary>
        /// <param name="image">Loaded snapshot, or <see langword="null"/> when unset.</param>
        /// <param name="field">Which field to format.</param>
        /// <returns>Formatted text, or empty when absent.</returns>
        public static string Format(ImageProperties? image, ImagePropertyField field)
        {
            if (image is null)
                return string.Empty;

            return field switch
            {
                ImagePropertyField.Format => _FormatName(image.Format),
                ImagePropertyField.Width => PropertyValueFormatting.PositiveInt(image.Width),
                ImagePropertyField.Height => PropertyValueFormatting.PositiveInt(image.Height),
                ImagePropertyField.BitDepth => PropertyValueFormatting.PositiveInt(image.BitDepth),
                ImagePropertyField.HorizontalResolutionDpi => _FormatDpi(image.HorizontalResolutionDpi),
                ImagePropertyField.VerticalResolutionDpi => _FormatDpi(image.VerticalResolutionDpi),
                ImagePropertyField.FrameCount => PropertyValueFormatting.PositiveInt(image.FrameCount),
                _ => string.Empty,
            };
        }

        private static string _FormatName(string? format)
        {
            return format.IsBlank() ? string.Empty : format;
        }

        private static string _FormatDpi(double value)
        {
            if (value <= 0)
                return string.Empty;

            var rounded = Math.Round(value);
            if (Math.Abs(value - rounded) < 1e-9)
                return rounded.ToString(CultureInfo.InvariantCulture);

            return value.ToString("G", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Fields exposed by image-* formatter tokens.
    /// </summary>
    internal enum ImagePropertyField
    {
        Format,
        Width,
        Height,
        BitDepth,
        HorizontalResolutionDpi,
        VerticalResolutionDpi,
        FrameCount,
    }
}
