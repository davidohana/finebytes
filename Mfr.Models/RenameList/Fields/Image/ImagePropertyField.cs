namespace Mfr.Models.RenameList.Fields.Image
{
    /// <summary>
    /// Image properties shared by formatter tokens and Rename List columns.
    /// </summary>
    internal enum ImagePropertyField
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
}
