using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.FilePropertiesGroup
{
    /// <summary>
    /// Resolves the <c>&lt;label&gt;</c> token.
    /// </summary>
    internal static class LabelResolver
    {
        /// <summary>
        /// Returns the volume label of the drive that holds the file.
        /// </summary>
        /// <param name="item">Rename item providing the directory path.</param>
        /// <returns>The volume label, or an empty string when the path has no resolvable root.</returns>
        public static string Resolve(RenameItem item)
        {
            var root = Path.GetPathRoot(item.Original.DirectoryPath);
            if (string.IsNullOrEmpty(root))
                return string.Empty;
            return new DriveInfo(root).VolumeLabel;
        }
    }
}
