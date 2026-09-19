namespace Mfr.Filters
{
    /// <summary>
    /// Shared classification of exceptions that Rename List soft-stores vs preview PreviewError.
    /// </summary>
    internal static class RenameItemMetadataLoadFailures
    {
        /// <summary>
        /// Returns whether <paramref name="ex"/> is a metadata read failure safe to soft-store on a row.
        /// </summary>
        /// <param name="ex">Exception from an Ensure* or reader call.</param>
        /// <returns><see langword="true"/> when the grid path should soft-store and tokens should PreviewError.</returns>
        internal static bool IsReadFailure(Exception ex)
        {
            if (
                ex
                is InvalidOperationException
                    or IOException
                    or InvalidDataException
                    or ArgumentException
                    or UnauthorizedAccessException
            )
            {
                return true;
            }

            // TagLib / MetadataExtractor / PdfPig / VersOne.Epub / OpenXml without a Filters package reference.
            // Walk bases so VersOne concretes (EpubPackageException, …) match EpubReaderException.
            for (var type = ex.GetType(); type is not null && type != typeof(object); type = type.BaseType)
            {
                if (
                    type.Name
                    is "UnsupportedFormatException"
                        or "CorruptFileException"
                        or "ImageProcessingException"
                        or "PdfDocumentFormatException"
                        or "PdfDocumentEncryptedException"
                        or "EpubReaderException"
                        or "OpenXmlPackageException"
                        or "FileFormatException"
                )
                {
                    return true;
                }
            }

            return false;
        }
    }
}
