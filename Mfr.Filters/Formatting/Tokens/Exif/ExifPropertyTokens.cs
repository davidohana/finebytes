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
    internal sealed class ExifMakeToken : ExifPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;exif-make&gt;</c>.</summary>
        public ExifMakeToken()
            : base(["exif-make"], ExifPropertyField.Make) { }
    }

    /// <inheritdoc />
    internal sealed class ExifModelToken : ExifPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;exif-model&gt;</c>.</summary>
        public ExifModelToken()
            : base(["exif-model"], ExifPropertyField.Model) { }
    }

    /// <inheritdoc />
    internal sealed class ExifExposureToken : ExifPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;exif-exposure&gt;</c>.</summary>
        public ExifExposureToken()
            : base(["exif-exposure"], ExifPropertyField.Exposure) { }
    }

    /// <inheritdoc />
    internal sealed class ExifFNumberToken : ExifPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;exif-fnumber&gt;</c>.</summary>
        public ExifFNumberToken()
            : base(["exif-fnumber"], ExifPropertyField.FNumber) { }
    }

    /// <inheritdoc />
    internal sealed class ExifIsoToken : ExifPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;exif-iso&gt;</c>.</summary>
        public ExifIsoToken()
            : base(["exif-iso"], ExifPropertyField.Iso) { }
    }

    /// <inheritdoc />
    internal sealed class ExifFocalToken : ExifPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;exif-focal&gt;</c>.</summary>
        public ExifFocalToken()
            : base(["exif-focal"], ExifPropertyField.FocalLength) { }
    }

    /// <inheritdoc />
    internal sealed class ExifFocal35Token : ExifPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;exif-focal-35&gt;</c>.</summary>
        public ExifFocal35Token()
            : base(["exif-focal-35"], ExifPropertyField.FocalLength35mm) { }
    }
}
