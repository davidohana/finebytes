namespace Mfr.Models.Media
{
    /// <summary>
    /// Read-only MetadataExtractor raster snapshot for image formatter tokens.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Populated lazily from disk; never written back. Integer and DPI fields use <c>0</c> for absent.
    /// Format names are MetadataExtractor short type names (<c>JPEG</c>, <c>PNG</c>, <c>GIF</c>,
    /// <c>TIFF</c>, <c>BMP</c>, <c>ICO</c>, <c>WebP</c>).
    /// </para>
    /// </remarks>
    public sealed record ImageProperties
    {
        /// <summary>
        /// Gets the detected raster format short name, or <see langword="null"/> when unknown.
        /// </summary>
        public string? Format { get; init; }

        /// <summary>
        /// Gets image width in pixels; <c>0</c> when absent.
        /// </summary>
        public int Width { get; init; }

        /// <summary>
        /// Gets image height in pixels; <c>0</c> when absent.
        /// </summary>
        public int Height { get; init; }

        /// <summary>
        /// Gets total bits per pixel; <c>0</c> when absent.
        /// </summary>
        public int BitDepth { get; init; }

        /// <summary>
        /// Gets horizontal resolution in dots per inch; <c>0</c> when absent or unspecified.
        /// </summary>
        public double HorizontalResolutionDpi { get; init; }

        /// <summary>
        /// Gets vertical resolution in dots per inch; <c>0</c> when absent or unspecified.
        /// </summary>
        public double VerticalResolutionDpi { get; init; }

        /// <summary>
        /// Gets frame count; <c>0</c> when unknown. Stills with known dimensions are <c>1</c>.
        /// </summary>
        public int FrameCount { get; init; }
    }
}
