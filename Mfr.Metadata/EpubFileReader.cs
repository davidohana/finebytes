using Mfr.Utils;
using VersOne.Epub;
using VersOne.Epub.Schema;

namespace Mfr.Metadata
{
    /// <summary>
    /// Opens an EPUB once with VersOne.Epub and maps Dublin Core package metadata.
    /// </summary>
    public static class EpubFileReader
    {
        /// <summary>
        /// Reads EPUB document Info from an existing regular file.
        /// </summary>
        /// <param name="absolutePath">Fully qualified filesystem path to an existing file.</param>
        /// <returns>A detached <see cref="EpubDocumentInfo"/> snapshot.</returns>
        /// <exception cref="ArgumentException"><paramref name="absolutePath"/> is empty, relative, missing, or a directory.</exception>
        /// <exception cref="EpubReaderException">The file is not a readable EPUB package.</exception>
        /// <exception cref="InvalidDataException">The file is not a ZIP/EPUB archive.</exception>
        public static EpubDocumentInfo Read(string absolutePath)
        {
            absolutePath.RequireExistingRegularFile();

            using var book = EpubReader.OpenBook(absolutePath);
            return _MapFrom(book.Schema.Package);
        }

        /// <summary>
        /// Maps package Dublin Core lists onto a detached Info snapshot (first non-blank entry each).
        /// </summary>
        private static EpubDocumentInfo _MapFrom(EpubPackage package)
        {
            var metadata = package.Metadata;
            return new EpubDocumentInfo
            {
                Title = _FirstNormalized(metadata.Titles, static t => t.Title),
                Creator = _FirstNormalized(metadata.Creators, static c => c.Creator),
                Publisher = _FirstNormalized(metadata.Publishers, static p => p.Publisher),
                Language = _FirstNormalized(metadata.Languages, static l => l.Language),
                Date = _FirstNormalized(metadata.Dates, static d => d.Date),
                Identifier = _ResolveIdentifier(package),
                Subject = _FirstNormalized(metadata.Subjects, static s => s.Subject),
                Description = _FirstNormalized(metadata.Descriptions, static d => d.Description),
            };
        }

        /// <summary>
        /// Prefers the identifier whose <c>id</c> matches <see cref="EpubPackage.UniqueIdentifier"/>; else first non-blank.
        /// </summary>
        private static string? _ResolveIdentifier(EpubPackage package)
        {
            var identifiers = package.Metadata.Identifiers;
            var uniqueId = package.UniqueIdentifier;
            if (!string.IsNullOrEmpty(uniqueId))
            {
                foreach (var entry in identifiers)
                {
                    if (!string.Equals(entry.Id, uniqueId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var matched = entry.Identifier.NormalizeMetadataText();
                    if (matched is not null)
                    {
                        return matched;
                    }

                    break;
                }
            }

            return _FirstNormalized(identifiers, static i => i.Identifier);
        }

        /// <summary>
        /// Returns the first non-blank normalized text from <paramref name="entries"/>, or <see langword="null"/>.
        /// </summary>
        private static string? _FirstNormalized<T>(IEnumerable<T> entries, Func<T, string?> selectText)
        {
            foreach (var entry in entries)
            {
                var normalized = selectText(entry).NormalizeMetadataText();
                if (normalized is not null)
                {
                    return normalized;
                }
            }

            return null;
        }
    }
}
