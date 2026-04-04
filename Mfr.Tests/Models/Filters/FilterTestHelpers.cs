using Mfr.Models;
using Mfr.Utils;

namespace Mfr.Tests.Models.Filters
{
    internal static class FilterTestHelpers
    {
        /// <summary>
        /// Builds a <see cref="RenameItem"/> for filter tests with predictable paths and indices.
        /// </summary>
        /// <param name="prefix">File name without extension.</param>
        /// <param name="extension">Extension including the leading dot.</param>
        /// <param name="globalIndex">Zero-based index across all files.</param>
        /// <param name="inFolderIndex">Zero-based index within the folder.</param>
        /// <param name="directory">Parent directory path, or a default when null.</param>
        /// <returns>A rename item with original and preview snapshots initialized.</returns>
        public static RenameItem CreateFile(
            string prefix = "track",
            string extension = ".mp3",
            int globalIndex = 0,
            int inFolderIndex = 0,
            string? directory = null)
        {
            directory ??= @"C:\Music\Album";
            var fullName = prefix + extension;
            var fullPath = directory.CombinePath(fullName);
            return new RenameItem(new FileEntryLite(globalIndex, inFolderIndex, fullPath, directory, prefix, extension));
        }
    }
}
