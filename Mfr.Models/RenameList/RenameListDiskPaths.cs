using Mfr.Models.Rename;

namespace Mfr.Models.RenameList
{
    /// <summary>
    /// Missing-path snapshot helpers for Rename List rows (updated on add/refresh, not on paint).
    /// </summary>
    public static class RenameListDiskPaths
    {
        /// <summary>
        /// Plain-language explanation for Show Error Details when the row path is absent.
        /// </summary>
        public const string MissingUserExplanation = "The file or folder is missing from disk.";

        /// <summary>
        /// Returns whether the last add or refresh found the row path absent from disk.
        /// </summary>
        /// <param name="item">Engine rename item.</param>
        /// <returns><see langword="true"/> when the stored snapshot says the path was missing.</returns>
        public static bool IsMissingFromDisk(RenameItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            return item.IsMissingFromDisk;
        }

        /// <summary>
        /// Builds the Show Error Details entry for a missing row.
        /// </summary>
        /// <param name="item">Engine rename item.</param>
        /// <returns>User and technical lines for the missing path.</returns>
        public static RenameListLoadError MissingLoadError(RenameItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            return MissingLoadError(item.Original.FullPath);
        }

        /// <summary>
        /// Builds the Show Error Details entry for a missing path.
        /// </summary>
        /// <param name="fullPath">Stored original path that is absent from disk.</param>
        /// <returns>User and technical lines for the missing path.</returns>
        public static RenameListLoadError MissingLoadError(string fullPath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fullPath);
            return new RenameListLoadError(MissingUserExplanation, fullPath, IsMissingFromDisk: true);
        }
    }
}
