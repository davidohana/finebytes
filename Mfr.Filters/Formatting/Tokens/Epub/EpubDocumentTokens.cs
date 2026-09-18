using Mfr.Models.RenameList;
using Mfr.Models.RenameList.Fields.Epub;

namespace Mfr.Filters.Formatting.Tokens.Epub
{
    /// <summary>
    /// Shared implementation for no-arg <c>epub-*</c> formatter tokens.
    /// </summary>
    internal abstract class EpubDocumentTokenBase(IReadOnlyList<string> names, EpubDocumentField propertyField)
        : IFormatToken,
            IRenameListMappedFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names => names;

        /// <inheritdoc />
        public bool TryGetFixedField(out string groupId, out string propertyKey)
        {
            groupId = EpubRenameListFields.Group;
            propertyKey = EpubPropertyRenameListField.CatalogPropertyKey(propertyField);
            return true;
        }

        /// <inheritdoc />
        public Formatter Compile(string tokenArgs)
        {
            FormatOptionsParsing.RequireNoArgument(tokenArgs, FormatOptionsParsing.TokenDisplayName(this));

            return item =>
            {
                item.EnsureEpubLoaded();
                return EpubDocumentInfoFormatting.Format(
                    item.Original.Epub,
                    propertyField,
                    PropertyDisplayContext.Token
                );
            };
        }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Title", "Document\\Epub", "EPUB document title from Dublin Core", "epub-title")]
    internal sealed class EpubTitleToken : EpubDocumentTokenBase
    {
        /// <summary>Registers <c>&lt;epub-title&gt;</c>.</summary>
        public EpubTitleToken()
            : base(["epub-title"], EpubDocumentField.Title) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Creator", "Document\\Epub", EpubRenameListFieldTips.Creator, "epub-creator")]
    internal sealed class EpubCreatorToken : EpubDocumentTokenBase
    {
        /// <summary>Registers <c>&lt;epub-creator&gt;</c>.</summary>
        public EpubCreatorToken()
            : base(["epub-creator"], EpubDocumentField.Creator) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Publisher", "Document\\Epub", "EPUB document publisher from Dublin Core", "epub-publisher")]
    internal sealed class EpubPublisherToken : EpubDocumentTokenBase
    {
        /// <summary>Registers <c>&lt;epub-publisher&gt;</c>.</summary>
        public EpubPublisherToken()
            : base(["epub-publisher"], EpubDocumentField.Publisher) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Language", "Document\\Epub", "EPUB document language from Dublin Core", "epub-language")]
    internal sealed class EpubLanguageToken : EpubDocumentTokenBase
    {
        /// <summary>Registers <c>&lt;epub-language&gt;</c>.</summary>
        public EpubLanguageToken()
            : base(["epub-language"], EpubDocumentField.Language) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Date", "Document\\Epub", "EPUB publication date from Dublin Core (literal string)", "epub-date")]
    internal sealed class EpubDateToken : EpubDocumentTokenBase
    {
        /// <summary>Registers <c>&lt;epub-date&gt;</c>.</summary>
        public EpubDateToken()
            : base(["epub-date"], EpubDocumentField.Date) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Identifier", "Document\\Epub", EpubRenameListFieldTips.Identifier, "epub-identifier")]
    internal sealed class EpubIdentifierToken : EpubDocumentTokenBase
    {
        /// <summary>Registers <c>&lt;epub-identifier&gt;</c>.</summary>
        public EpubIdentifierToken()
            : base(["epub-identifier"], EpubDocumentField.Identifier) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Subject", "Document\\Epub", "EPUB document subject from Dublin Core", "epub-subject")]
    internal sealed class EpubSubjectToken : EpubDocumentTokenBase
    {
        /// <summary>Registers <c>&lt;epub-subject&gt;</c>.</summary>
        public EpubSubjectToken()
            : base(["epub-subject"], EpubDocumentField.Subject) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Description", "Document\\Epub", EpubRenameListFieldTips.Description, "epub-description")]
    internal sealed class EpubDescriptionToken : EpubDocumentTokenBase
    {
        /// <summary>Registers <c>&lt;epub-description&gt;</c>.</summary>
        public EpubDescriptionToken()
            : base(["epub-description"], EpubDocumentField.Description) { }
    }
}
