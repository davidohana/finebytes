namespace Mfr.Filters.Formatting.Tokens.Image
{
    /// <summary>
    /// Shared implementation for no-arg <c>image-*</c> formatter tokens.
    /// </summary>
    internal abstract class ImagePropertyTokenBase(IReadOnlyList<string> names, ImagePropertyField field) : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names => names;

        /// <inheritdoc />
        public Formatter Compile(string tokenArgs)
        {
            FormatOptionsParsing.RequireNoArgument(tokenArgs, FormatOptionsParsing.TokenDisplayName(this));

            return item =>
            {
                item.EnsureImagePropertiesLoaded();
                return ImagePropertiesFormatting.Format(item.Original.Image, field);
            };
        }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Width", "Image\\Properties", "Image width in pixels", "image-width")]
    internal sealed class ImageWidthToken : ImagePropertyTokenBase
    {
        /// <summary>Registers <c>&lt;image-width&gt;</c>.</summary>
        public ImageWidthToken()
            : base(["image-width"], ImagePropertyField.Width) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Height", "Image\\Properties", "Image height in pixels", "image-height")]
    internal sealed class ImageHeightToken : ImagePropertyTokenBase
    {
        /// <summary>Registers <c>&lt;image-height&gt;</c>.</summary>
        public ImageHeightToken()
            : base(["image-height"], ImagePropertyField.Height) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Bit Depth", "Image\\Properties", "Image bit depth", "image-bit-depth")]
    internal sealed class ImageBitDepthToken : ImagePropertyTokenBase
    {
        /// <summary>Registers <c>&lt;image-bit-depth&gt;</c>.</summary>
        public ImageBitDepthToken()
            : base(["image-bit-depth"], ImagePropertyField.BitDepth) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Format", "Image\\Properties", "Image file format (i.e. JPEG)", "image-format")]
    internal sealed class ImageFormatToken : ImagePropertyTokenBase
    {
        /// <summary>Registers <c>&lt;image-format&gt;</c>.</summary>
        public ImageFormatToken()
            : base(["image-format"], ImagePropertyField.Format) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo(
        "Horizontal Resolution",
        "Image\\Properties",
        "Image horizontal resolution (DPI)",
        "image-horz-res"
    )]
    internal sealed class ImageHorzResToken : ImagePropertyTokenBase
    {
        /// <summary>Registers <c>&lt;image-horz-res&gt;</c>.</summary>
        public ImageHorzResToken()
            : base(["image-horz-res"], ImagePropertyField.HorizontalResolutionDpi) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Vertical Resolution", "Image\\Properties", "Image vertical resolution (DPI)", "image-vert-res")]
    internal sealed class ImageVertResToken : ImagePropertyTokenBase
    {
        /// <summary>Registers <c>&lt;image-vert-res&gt;</c>.</summary>
        public ImageVertResToken()
            : base(["image-vert-res"], ImagePropertyField.VerticalResolutionDpi) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Frames Count", "Image\\Properties", "Number of frames in the image", "image-frame-count")]
    internal sealed class ImageFrameCountToken : ImagePropertyTokenBase
    {
        /// <summary>Registers <c>&lt;image-frame-count&gt;</c>.</summary>
        public ImageFrameCountToken()
            : base(["image-frame-count"], ImagePropertyField.FrameCount) { }
    }
}
