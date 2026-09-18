namespace Mfr.Models.Media
{
    /// <summary>
    /// Read-only PDF Info + page-count snapshot for formatter tokens.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Populated lazily from disk via PdfPig; never written back. Missing text fields are
    /// <see langword="null"/>. <see cref="Created"/> / <see cref="Modified"/> are
    /// <see langword="null"/> when absent or unparseable. <see cref="PageCount"/> is <c>0</c> when
    /// unknown (successful opens normally report a positive count).
    /// </para>
    /// </remarks>
    public sealed record PdfDocumentInfo
    {
        /// <summary>
        /// Gets the document title (Info Title), or <see langword="null"/> when absent.
        /// </summary>
        public string? Title { get; init; }

        /// <summary>
        /// Gets the document author (Info Author), or <see langword="null"/> when absent.
        /// </summary>
        public string? Author { get; init; }

        /// <summary>
        /// Gets the document subject (Info Subject), or <see langword="null"/> when absent.
        /// </summary>
        public string? Subject { get; init; }

        /// <summary>
        /// Gets the document keywords (Info Keywords), or <see langword="null"/> when absent.
        /// </summary>
        public string? Keywords { get; init; }

        /// <summary>
        /// Gets the creating application (Info Creator), or <see langword="null"/> when absent.
        /// </summary>
        public string? Creator { get; init; }

        /// <summary>
        /// Gets the PDF producer (Info Producer), or <see langword="null"/> when absent.
        /// </summary>
        public string? Producer { get; init; }

        /// <summary>
        /// Gets the parsed creation date (Info CreationDate), or <see langword="null"/> when absent or unparseable.
        /// </summary>
        public DateTimeOffset? Created { get; init; }

        /// <summary>
        /// Gets the parsed modification date (Info ModDate), or <see langword="null"/> when absent or unparseable.
        /// </summary>
        public DateTimeOffset? Modified { get; init; }

        /// <summary>
        /// Gets the page count; <c>0</c> when unknown.
        /// </summary>
        public int PageCount { get; init; }
    }
}
