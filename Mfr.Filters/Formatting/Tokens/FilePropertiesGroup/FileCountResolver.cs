using System.Globalization;
using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.FilePropertiesGroup
{
    /// <summary>
    /// Resolves the <c>&lt;file-count&gt;</c> token.
    /// </summary>
    internal static class FileCountResolver
    {
        /// <summary>
        /// Returns the number of file system entries in the file's parent directory (non-recursive).
        /// </summary>
        /// <param name="item">Rename item providing the directory path.</param>
        /// <returns>The entry count, or an empty string when the directory does not exist.</returns>
        public static string Resolve(RenameItem item)
        {
            var dir = item.Original.DirectoryPath;
            if (!Directory.Exists(dir))
                return string.Empty;
            return Directory.GetFileSystemEntries(dir).Length.ToString(CultureInfo.InvariantCulture);
        }
    }
}
