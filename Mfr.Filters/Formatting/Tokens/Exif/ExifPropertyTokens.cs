namespace Mfr.Filters.Formatting.Tokens.Exif
{
    /// <summary>
    /// Shared implementation for no-arg <c>exif-*</c> formatter tokens.
    /// </summary>
    internal abstract class ExifPropertyTokenBase(IReadOnlyList<string> names, ExifPropertyField field) : IFormatToken
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
                return ExifDataFormatting.Format(item.Original.Exif, field);
            };
        }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Make", "Image\\EXIF", "Camera manufacturer from EXIF", "exif-make")]
    internal sealed class ExifMakeToken : ExifPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;exif-make&gt;</c>.</summary>
        public ExifMakeToken()
            : base(["exif-make"], ExifPropertyField.Make) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Model", "Image\\EXIF", "Camera model from EXIF", "exif-model")]
    internal sealed class ExifModelToken : ExifPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;exif-model&gt;</c>.</summary>
        public ExifModelToken()
            : base(["exif-model"], ExifPropertyField.Model) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Exposure", "Image\\EXIF", "Exposure time description from EXIF", "exif-exposure")]
    internal sealed class ExifExposureToken : ExifPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;exif-exposure&gt;</c>.</summary>
        public ExifExposureToken()
            : base(["exif-exposure"], ExifPropertyField.Exposure) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("F-Number", "Image\\EXIF", "F-number description from EXIF", "exif-fnumber")]
    internal sealed class ExifFNumberToken : ExifPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;exif-fnumber&gt;</c>.</summary>
        public ExifFNumberToken()
            : base(["exif-fnumber"], ExifPropertyField.FNumber) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("ISO", "Image\\EXIF", "ISO speed description from EXIF", "exif-iso")]
    internal sealed class ExifIsoToken : ExifPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;exif-iso&gt;</c>.</summary>
        public ExifIsoToken()
            : base(["exif-iso"], ExifPropertyField.Iso) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Focal Length", "Image\\EXIF", "Focal length description from EXIF", "exif-focal")]
    internal sealed class ExifFocalToken : ExifPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;exif-focal&gt;</c>.</summary>
        public ExifFocalToken()
            : base(["exif-focal"], ExifPropertyField.FocalLength) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Focal Length 35mm", "Image\\EXIF", "35mm-equivalent focal length from EXIF", "exif-focal-35")]
    internal sealed class ExifFocal35Token : ExifPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;exif-focal-35&gt;</c>.</summary>
        public ExifFocal35Token()
            : base(["exif-focal-35"], ExifPropertyField.FocalLength35mm) { }
    }
}
