namespace Mfr.Models.Media
{
    /// <summary>
    /// Read-only Office PackageProperties snapshot for formatter tokens.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Populated lazily from disk via OpenXml <c>WordprocessingDocument</c> (DOCX first); never written back.
    /// Missing text fields are <see langword="null"/>. <see cref="Author"/> maps from OPC core <c>Creator</c>.
    /// <see cref="Created"/> / <see cref="Modified"/> are <see langword="null"/> when absent.
    /// </para>
    /// </remarks>
    public sealed record OfficeDocumentInfo
    {
        /// <summary>
        /// Gets the document title (PackageProperties Title), or <see langword="null"/> when absent.
        /// </summary>
        public string? Title { get; init; }

        /// <summary>
        /// Gets the document author (maps from PackageProperties Creator), or <see langword="null"/> when absent.
        /// </summary>
        public string? Author { get; init; }

        /// <summary>
        /// Gets the document subject (PackageProperties Subject), or <see langword="null"/> when absent.
        /// </summary>
        public string? Subject { get; init; }

        /// <summary>
        /// Gets the document keywords (PackageProperties Keywords), or <see langword="null"/> when absent.
        /// </summary>
        public string? Keywords { get; init; }

        /// <summary>
        /// Gets the document category (PackageProperties Category), or <see langword="null"/> when absent.
        /// </summary>
        public string? Category { get; init; }

        /// <summary>
        /// Gets the document description (PackageProperties Description), or <see langword="null"/> when absent.
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// Gets the last-modified-by name (PackageProperties LastModifiedBy), or <see langword="null"/> when absent.
        /// </summary>
        public string? LastModifiedBy { get; init; }

        /// <summary>
        /// Gets the package creation date (PackageProperties Created), or <see langword="null"/> when absent.
        /// </summary>
        public DateTimeOffset? Created { get; init; }

        /// <summary>
        /// Gets the package modification date (PackageProperties Modified), or <see langword="null"/> when absent.
        /// </summary>
        public DateTimeOffset? Modified { get; init; }
    }
}
