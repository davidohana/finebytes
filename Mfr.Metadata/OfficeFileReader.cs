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
        /// <exception cref="FileFormatException">The path is not a valid OPC/ZIP package.</exception>
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
                Title = properties.Title.NormalizeMetadataText(),
                Author = properties.Creator.NormalizeMetadataText(),
                Subject = properties.Subject.NormalizeMetadataText(),
                Keywords = properties.Keywords.NormalizeMetadataText(),
                Category = properties.Category.NormalizeMetadataText(),
                Description = properties.Description.NormalizeMetadataText(),
                LastModifiedBy = properties.LastModifiedBy.NormalizeMetadataText(),
                Created = _MapDate(properties.Created),
                Modified = _MapDate(properties.Modified),
            };
        }

        /// <summary>
        /// Maps package <see cref="DateTime"/> to UTC <see cref="DateTimeOffset"/> (PDF-stable token times).
        /// </summary>
        /// <remarks>
        /// <para>
        /// OpenXml often surfaces Zulu core props as <see cref="DateTimeKind.Local"/>. Unspecified kind is treated as
        /// UTC offset 0; Local/Utc keep the same instant and normalize to offset 0.
        /// </para>
        /// </remarks>
        private static DateTimeOffset? _MapDate(DateTime? value)
        {
            if (value is not { } dt)
            {
                return null;
            }

            if (dt.Kind == DateTimeKind.Unspecified)
            {
                return new DateTimeOffset(dt, TimeSpan.Zero);
            }

            return new DateTimeOffset(dt).ToUniversalTime();
        }
    }
}
