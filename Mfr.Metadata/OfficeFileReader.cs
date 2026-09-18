using DocumentFormat.OpenXml.Packaging;
using Mfr.Utils;

namespace Mfr.Metadata
{
    /// <summary>
    /// Opens a DOCX once with OpenXml and maps PackageProperties.
    /// </summary>
    public static class OfficeFileReader
    {
        /// <summary>
        /// Reads Office document Info from an existing regular <c>.docx</c> file.
        /// </summary>
        /// <param name="absolutePath">Fully qualified filesystem path to an existing file.</param>
        /// <returns>A detached <see cref="OfficeDocumentInfo"/> snapshot.</returns>
        /// <exception cref="ArgumentException"><paramref name="absolutePath"/> is empty, relative, missing, or a directory.</exception>
        /// <exception cref="InvalidDataException">The path is not a <c>.docx</c> file.</exception>
        /// <exception cref="OpenXmlPackageException">The file is not a readable WordprocessingDocument package.</exception>
        public static OfficeDocumentInfo Read(string absolutePath)
        {
            absolutePath.RequireExistingRegularFile();

            if (!absolutePath.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Not a DOCX Office Open XML document.");
            }

            using var document = WordprocessingDocument.Open(absolutePath, isEditable: false);
#pragma warning disable OOXML0001 // PackageProperties is IPackageProperties (experimental); no stable non-experimental PackageProperties API on OpenXml 3.x.
            var properties = document.PackageProperties;
#pragma warning restore OOXML0001
            return new OfficeDocumentInfo
            {
                Title = _NormalizeText(properties.Title),
                Author = _NormalizeText(properties.Creator),
                Subject = _NormalizeText(properties.Subject),
                Keywords = _NormalizeText(properties.Keywords),
                Category = _NormalizeText(properties.Category),
                Description = _NormalizeText(properties.Description),
                LastModifiedBy = _NormalizeText(properties.LastModifiedBy),
                Created = _MapDate(properties.Created),
                Modified = _MapDate(properties.Modified),
            };
        }

        /// <summary>
        /// Treats blank as absent and collapses newlines to spaces (same policy as PDF/EPUB Info text).
        /// </summary>
        private static string? _NormalizeText(string? value)
        {
            if (value.IsBlank())
            {
                return null;
            }

            return value.Replace('\n', ' ').Replace('\r', ' ').Trim();
        }

        /// <summary>
        /// Maps package <see cref="DateTime"/> to <see cref="DateTimeOffset"/>; unspecified kind uses UTC offset 0.
        /// </summary>
        private static DateTimeOffset? _MapDate(DateTime? value)
        {
            if (value is not { } dt)
            {
                return null;
            }

            if (dt.Kind == DateTimeKind.Utc)
            {
                return new DateTimeOffset(dt);
            }

            if (dt.Kind == DateTimeKind.Local)
            {
                return new DateTimeOffset(dt);
            }

            return new DateTimeOffset(dt, TimeSpan.Zero);
        }
    }
}
