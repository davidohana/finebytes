using Mfr.Models.RenameList;
using Mfr.Models.RenameList.Fields.Office;

namespace Mfr.Filters.Formatting.Tokens.Office
{
    /// <summary>
    /// Shared implementation for no-arg <c>office-*</c> formatter tokens.
    /// </summary>
    internal abstract class OfficeDocumentTokenBase(IReadOnlyList<string> names, OfficeDocumentField propertyField)
        : IFormatToken,
            IRenameListMappedFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names => names;

        /// <inheritdoc />
        public bool TryGetFixedField(out string groupId, out string propertyKey)
        {
            groupId = OfficeRenameListFields.Group;
            propertyKey = OfficePropertyRenameListField.CatalogPropertyKey(propertyField);
            return true;
        }

        /// <inheritdoc />
        public Formatter Compile(string tokenArgs)
        {
            FormatOptionsParsing.RequireNoArgument(tokenArgs, FormatOptionsParsing.TokenDisplayName(this));

            return item =>
            {
                item.EnsureOfficeLoaded();
                return OfficeDocumentInfoFormatting.Format(
                    item.Original.Office,
                    propertyField,
                    PropertyDisplayContext.Token
                );
            };
        }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Title", "Document\\Office", "Office document title from PackageProperties", "office-title")]
    internal sealed class OfficeTitleToken : OfficeDocumentTokenBase
    {
        /// <summary>Registers <c>&lt;office-title&gt;</c>.</summary>
        public OfficeTitleToken()
            : base(["office-title"], OfficeDocumentField.Title) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Author", "Document\\Office", OfficeRenameListFieldTips.Author, "office-author")]
    internal sealed class OfficeAuthorToken : OfficeDocumentTokenBase
    {
        /// <summary>Registers <c>&lt;office-author&gt;</c>.</summary>
        public OfficeAuthorToken()
            : base(["office-author"], OfficeDocumentField.Author) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Subject", "Document\\Office", "Office document subject from PackageProperties", "office-subject")]
    internal sealed class OfficeSubjectToken : OfficeDocumentTokenBase
    {
        /// <summary>Registers <c>&lt;office-subject&gt;</c>.</summary>
        public OfficeSubjectToken()
            : base(["office-subject"], OfficeDocumentField.Subject) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo(
        "Keywords",
        "Document\\Office",
        "Office document keywords from PackageProperties",
        "office-keywords"
    )]
    internal sealed class OfficeKeywordsToken : OfficeDocumentTokenBase
    {
        /// <summary>Registers <c>&lt;office-keywords&gt;</c>.</summary>
        public OfficeKeywordsToken()
            : base(["office-keywords"], OfficeDocumentField.Keywords) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Category", "Document\\Office", OfficeRenameListFieldTips.Category, "office-category")]
    internal sealed class OfficeCategoryToken : OfficeDocumentTokenBase
    {
        /// <summary>Registers <c>&lt;office-category&gt;</c>.</summary>
        public OfficeCategoryToken()
            : base(["office-category"], OfficeDocumentField.Category) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo(
        "Description",
        "Document\\Office",
        "Office document description from PackageProperties",
        "office-description"
    )]
    internal sealed class OfficeDescriptionToken : OfficeDocumentTokenBase
    {
        /// <summary>Registers <c>&lt;office-description&gt;</c>.</summary>
        public OfficeDescriptionToken()
            : base(["office-description"], OfficeDocumentField.Description) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo(
        "Last Modified By",
        "Document\\Office",
        OfficeRenameListFieldTips.LastModifiedBy,
        "office-last-modified-by"
    )]
    internal sealed class OfficeLastModifiedByToken : OfficeDocumentTokenBase
    {
        /// <summary>Registers <c>&lt;office-last-modified-by&gt;</c>.</summary>
        public OfficeLastModifiedByToken()
            : base(["office-last-modified-by"], OfficeDocumentField.LastModifiedBy) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo(
        "Created",
        "Document\\Office",
        "Office PackageProperties creation date (general format)",
        "office-created"
    )]
    internal sealed class OfficeCreatedToken : OfficeDocumentTokenBase
    {
        /// <summary>Registers <c>&lt;office-created&gt;</c>.</summary>
        public OfficeCreatedToken()
            : base(["office-created"], OfficeDocumentField.Created) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo(
        "Modified",
        "Document\\Office",
        "Office PackageProperties modification date (general format)",
        "office-modified"
    )]
    internal sealed class OfficeModifiedToken : OfficeDocumentTokenBase
    {
        /// <summary>Registers <c>&lt;office-modified&gt;</c>.</summary>
        public OfficeModifiedToken()
            : base(["office-modified"], OfficeDocumentField.Modified) { }
    }
}
