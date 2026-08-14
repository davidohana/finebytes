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
    internal sealed class ImageWidthToken : ImagePropertyTokenBase
    {
        /// <summary>Registers <c>&lt;image-width&gt;</c>.</summary>
        public ImageWidthToken()
            : base(["image-width"], ImagePropertyField.Width)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class ImageHeightToken : ImagePropertyTokenBase
    {
        /// <summary>Registers <c>&lt;image-height&gt;</c>.</summary>
        public ImageHeightToken()
            : base(["image-height"], ImagePropertyField.Height)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class ImageBitDepthToken : ImagePropertyTokenBase
    {
        /// <summary>Registers <c>&lt;image-bit-depth&gt;</c>.</summary>
        public ImageBitDepthToken()
            : base(["image-bit-depth"], ImagePropertyField.BitDepth)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class ImageFormatToken : ImagePropertyTokenBase
    {
        /// <summary>Registers <c>&lt;image-format&gt;</c>.</summary>
        public ImageFormatToken()
            : base(["image-format"], ImagePropertyField.Format)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class ImageHorzResToken : ImagePropertyTokenBase
    {
        /// <summary>Registers <c>&lt;image-horz-res&gt;</c>.</summary>
        public ImageHorzResToken()
            : base(["image-horz-res"], ImagePropertyField.HorizontalResolutionDpi)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class ImageVertResToken : ImagePropertyTokenBase
    {
        /// <summary>Registers <c>&lt;image-vert-res&gt;</c>.</summary>
        public ImageVertResToken()
            : base(["image-vert-res"], ImagePropertyField.VerticalResolutionDpi)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class ImageFrameCountToken : ImagePropertyTokenBase
    {
        /// <summary>Registers <c>&lt;image-frame-count&gt;</c>.</summary>
        public ImageFrameCountToken()
            : base(["image-frame-count"], ImagePropertyField.FrameCount)
        {
        }
    }
}
