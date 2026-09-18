namespace Mfr.Models.Media
{
    /// <summary>
    /// Read-only EPUB Dublin Core Info snapshot for formatter tokens.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Populated lazily from disk via VersOne.Epub <c>OpenBook</c>; never written back. Missing text fields are
    /// <see langword="null"/>. Multi-value DC lists use the first non-blank entry only. <see cref="Date"/> stays a
    /// literal string (year-only / incomplete dates are common). <see cref="Identifier"/> prefers the package
    /// unique-identifier match when present.
    /// </para>
    /// </remarks>
    public sealed record EpubDocumentInfo
    {
        /// <summary>
        /// Gets the document title (dc:title), or <see langword="null"/> when absent.
        /// </summary>
        public string? Title { get; init; }

        /// <summary>
        /// Gets the Dublin Core creator / author (dc:creator), or <see langword="null"/> when absent.
        /// </summary>
        public string? Creator { get; init; }

        /// <summary>
        /// Gets the publisher (dc:publisher), or <see langword="null"/> when absent.
        /// </summary>
        public string? Publisher { get; init; }

        /// <summary>
        /// Gets the language (dc:language), or <see langword="null"/> when absent.
        /// </summary>
        public string? Language { get; init; }

        /// <summary>
        /// Gets the literal publication date string (dc:date), or <see langword="null"/> when absent.
        /// </summary>
        public string? Date { get; init; }

        /// <summary>
        /// Gets the preferred identifier (unique-id match, else first dc:identifier), or <see langword="null"/> when absent.
        /// </summary>
        public string? Identifier { get; init; }

        /// <summary>
        /// Gets the subject (dc:subject), or <see langword="null"/> when absent.
        /// </summary>
        public string? Subject { get; init; }

        /// <summary>
        /// Gets the description (dc:description), or <see langword="null"/> when absent.
        /// </summary>
        public string? Description { get; init; }
    }
}
