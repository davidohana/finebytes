using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.FilePropertiesGroup
{
    /// <summary>
    /// Resolves the <c>&lt;drive-letter&gt;</c> token.
    /// </summary>
    internal static class DriveLetterResolver
    {
        /// <summary>
        /// Returns the drive letter (e.g. <c>C:</c>) for local paths, or <c>$</c> for UNC paths.
        /// </summary>
        /// <param name="item">Rename item providing the directory path.</param>
        /// <returns>The drive letter without a trailing separator, or <c>$</c> for network paths.</returns>
        public static string Resolve(RenameItem item)
        {
            var root = Path.GetPathRoot(item.Original.DirectoryPath) ?? string.Empty;
            if (root.StartsWith(@"\\", StringComparison.Ordinal))
                return "$";
            return root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}
