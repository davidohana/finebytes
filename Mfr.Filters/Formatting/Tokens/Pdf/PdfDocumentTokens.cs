using Mfr.Models.RenameList;
using Mfr.Models.RenameList.Fields.Pdf;

namespace Mfr.Filters.Formatting.Tokens.Pdf
{
    /// <summary>
    /// Shared implementation for no-arg <c>pdf-*</c> formatter tokens.
    /// </summary>
    internal abstract class PdfDocumentTokenBase(IReadOnlyList<string> names, PdfDocumentField propertyField)
        : IFormatToken,
            IRenameListMappedFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names => names;

        /// <inheritdoc />
        public bool TryGetFixedField(out string groupId, out string propertyKey)
        {
            groupId = PdfRenameListFields.Group;
            propertyKey = PdfPropertyRenameListField.CatalogPropertyKey(propertyField);
            return true;
        }

        /// <inheritdoc />
        public Formatter Compile(string tokenArgs)
        {
            FormatOptionsParsing.RequireNoArgument(tokenArgs, FormatOptionsParsing.TokenDisplayName(this));

            return item =>
            {
                item.EnsurePdfLoaded();
                return PdfDocumentInfoFormatting.Format(item.Original.Pdf, propertyField, PropertyDisplayContext.Token);
            };
        }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Title", "Document\\Pdf", "PDF document title from Info", "pdf-title")]
    internal sealed class PdfTitleToken : PdfDocumentTokenBase
    {
        /// <summary>Registers <c>&lt;pdf-title&gt;</c>.</summary>
        public PdfTitleToken()
            : base(["pdf-title"], PdfDocumentField.Title) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Author", "Document\\Pdf", "PDF document author from Info", "pdf-author")]
    internal sealed class PdfAuthorToken : PdfDocumentTokenBase
    {
        /// <summary>Registers <c>&lt;pdf-author&gt;</c>.</summary>
        public PdfAuthorToken()
            : base(["pdf-author"], PdfDocumentField.Author) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Subject", "Document\\Pdf", "PDF document subject from Info", "pdf-subject")]
    internal sealed class PdfSubjectToken : PdfDocumentTokenBase
    {
        /// <summary>Registers <c>&lt;pdf-subject&gt;</c>.</summary>
        public PdfSubjectToken()
            : base(["pdf-subject"], PdfDocumentField.Subject) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Keywords", "Document\\Pdf", "PDF document keywords from Info", "pdf-keywords")]
    internal sealed class PdfKeywordsToken : PdfDocumentTokenBase
    {
        /// <summary>Registers <c>&lt;pdf-keywords&gt;</c>.</summary>
        public PdfKeywordsToken()
            : base(["pdf-keywords"], PdfDocumentField.Keywords) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo(
        "Creator",
        "Document\\Pdf",
        "Application that created the original document before PDF conversion",
        "pdf-creator"
    )]
    internal sealed class PdfCreatorToken : PdfDocumentTokenBase
    {
        /// <summary>Registers <c>&lt;pdf-creator&gt;</c>.</summary>
        public PdfCreatorToken()
            : base(["pdf-creator"], PdfDocumentField.Creator) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Producer", "Document\\Pdf", "Application that produced this PDF file", "pdf-producer")]
    internal sealed class PdfProducerToken : PdfDocumentTokenBase
    {
        /// <summary>Registers <c>&lt;pdf-producer&gt;</c>.</summary>
        public PdfProducerToken()
            : base(["pdf-producer"], PdfDocumentField.Producer) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Created", "Document\\Pdf", "PDF Info creation date (general format)", "pdf-created")]
    internal sealed class PdfCreatedToken : PdfDocumentTokenBase
    {
        /// <summary>Registers <c>&lt;pdf-created&gt;</c>.</summary>
        public PdfCreatedToken()
            : base(["pdf-created"], PdfDocumentField.Created) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Modified", "Document\\Pdf", "PDF Info modification date (general format)", "pdf-modified")]
    internal sealed class PdfModifiedToken : PdfDocumentTokenBase
    {
        /// <summary>Registers <c>&lt;pdf-modified&gt;</c>.</summary>
        public PdfModifiedToken()
            : base(["pdf-modified"], PdfDocumentField.Modified) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Page Count", "Document\\Pdf", "Number of pages in the PDF", "pdf-page-count")]
    internal sealed class PdfPageCountToken : PdfDocumentTokenBase
    {
        /// <summary>Registers <c>&lt;pdf-page-count&gt;</c>.</summary>
        public PdfPageCountToken()
            : base(["pdf-page-count"], PdfDocumentField.PageCount) { }
    }
}
