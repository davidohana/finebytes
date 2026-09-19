using Mfr.Utils;
using UglyToad.PdfPig;

namespace Mfr.Metadata
{
    /// <summary>
    /// Opens a PDF once with PdfPig and maps Info fields plus page count.
    /// </summary>
    public static class PdfFileReader
    {
        /// <summary>
        /// Reads PDF document Info and page count from an existing regular file.
        /// </summary>
        /// <param name="absolutePath">Fully qualified filesystem path to an existing file.</param>
        /// <returns>A detached <see cref="PdfDocumentInfo"/> snapshot.</returns>
        /// <exception cref="ArgumentException"><paramref name="absolutePath"/> is empty, relative, missing, or a directory.</exception>
        /// <exception cref="UglyToad.PdfPig.Core.PdfDocumentFormatException">The file is not a readable PDF.</exception>
        public static PdfDocumentInfo Read(string absolutePath)
        {
            absolutePath.RequireExistingRegularFile();

            using var document = PdfDocument.Open(absolutePath);
            return _MapFrom(document);
        }

        private static PdfDocumentInfo _MapFrom(PdfDocument document)
        {
            var information = document.Information;
            return new PdfDocumentInfo
            {
                Title = information.Title.NormalizeMetadataText(),
                Author = information.Author.NormalizeMetadataText(),
                Subject = information.Subject.NormalizeMetadataText(),
                Keywords = information.Keywords.NormalizeMetadataText(),
                Creator = information.Creator.NormalizeMetadataText(),
                Producer = information.Producer.NormalizeMetadataText(),
                Created = information.GetCreatedDateTimeOffset(),
                Modified = information.GetModifiedDateTimeOffset(),
                PageCount = document.NumberOfPages,
            };
        }
    }
}
