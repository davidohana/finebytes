using Mfr.Models.Rename;
using Mfr.Utils;

namespace Mfr.Models.RenameList
{
    /// <summary>
    /// Disk presence checks for Rename List rows (MFR7 <c>RenameItem.Exists</c>).
    /// </summary>
    public static class RenameListDiskPaths
    {
        /// <summary>
        /// Returns whether <paramref name="meta"/>'s path still exists on disk.
        /// </summary>
        /// <param name="meta">Original row snapshot.</param>
        /// <returns><see langword="true"/> when the file or folder path is present.</returns>
        public static bool ExistsOnDisk(FileMeta meta)
        {
            ArgumentNullException.ThrowIfNull(meta);

            var path = meta.FullPath;
            return meta.Attributes.IsDirectory() ? Directory.Exists(path) : File.Exists(path);
        }

        /// <summary>
        /// Plain-language explanation for Show Load Errors when the row path is absent.
        /// </summary>
        public const string MissingUserExplanation = "The file or folder is missing from disk.";

        /// <summary>
        /// Returns whether the rename row path is absent from disk.
        /// </summary>
        /// <param name="item">Engine rename item.</param>
        /// <returns><see langword="true"/> when neither file nor folder exists at the stored path.</returns>
        public static bool IsMissingFromDisk(RenameItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            return !ExistsOnDisk(item.Original);
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
