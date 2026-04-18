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
        /// <param name="attributes">Filesystem attributes for the synthetic item.</param>
        /// <param name="creationTime">Optional creation time; defaults to a fixed test value.</param>
        /// <param name="lastWriteTime">Optional last write time; defaults to a fixed test value.</param>
        /// <param name="lastAccessTime">Optional last access time; defaults to a fixed test value.</param>
        /// <returns>A rename item with original and preview snapshots initialized.</returns>
        public static RenameItem CreateRenameItem(
            string prefix = "track",
            string extension = ".mp3",
            int globalIndex = 0,
            int inFolderIndex = 0,
            string? directory = null,
            FileAttributes attributes = FileAttributes.Normal,
            DateTime? creationTime = null,
            DateTime? lastWriteTime = null,
            DateTime? lastAccessTime = null)
        {
            directory ??= @"C:\Music\Album";
            var baseline = new DateTime(2024, 6, 1, 12, 30, 45, DateTimeKind.Unspecified);
            return new RenameItem(new FileMeta(
                globalIndex,
                inFolderIndex,
                directory,
                prefix,
                extension,
                attributes,
                creationTime: creationTime ?? baseline,
                lastWriteTime: lastWriteTime ?? baseline,
                lastAccessTime: lastAccessTime ?? baseline));
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
            var item = CreateRenameItem(inputPrefix, extension, globalIndex, inFolderIndex, directory);
            filter.Setup();
            filter.Apply(item);
            return item.Preview.Prefix;
        }
    }
}
