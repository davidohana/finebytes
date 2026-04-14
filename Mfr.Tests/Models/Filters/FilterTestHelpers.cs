using Mfr.Filters;
using Mfr.Models;


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
            return new RenameItem(new FileMeta(globalIndex, inFolderIndex, directory, prefix, extension));
        }

        /// <summary>
        /// Applies a filter to a prefix-targeted rename item and returns the resulting preview prefix.
        /// </summary>
        /// <param name="filter">Filter to apply.</param>
        /// <param name="inputPrefix">Input prefix used for the test item.</param>
        /// <param name="extension">Input extension used for the test item.</param>
        /// <param name="globalIndex">Zero-based index across all files.</param>
        /// <param name="inFolderIndex">Zero-based index within the folder.</param>
        /// <param name="directory">Parent directory path, or a default when null.</param>
        /// <returns>The resulting preview prefix after applying the filter.</returns>
        public static string ApplyToPrefix(
            BaseFilter filter,
            string inputPrefix,
            string extension = ".mp3",
            int globalIndex = 0,
            int inFolderIndex = 0,
            string? directory = null)
        {
            var file = CreateFile(inputPrefix, extension, globalIndex, inFolderIndex, directory);
            var filterChainContext = new FilterChainContext();
            filter.Apply(file, filterChainContext);
            return file.Preview.Prefix;
        }
    }
}
