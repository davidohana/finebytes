using System.Globalization;
using Mfr.Models;
using Mfr.Utils;

namespace Mfr.Filters.Formatting.Tokens.FileNameGroup
{
    /// <summary>
    /// Resolves the <c>&lt;parent-folder&gt;</c> and <c>&lt;parent-folder:level&gt;</c> tokens.
    /// </summary>
    internal static class ParentFolderResolver
    {
        /// <summary>
        /// Returns the ancestor folder segment at the requested level (default 1).
        /// </summary>
        /// <param name="arg">Optional 1-based ancestor level.</param>
        /// <param name="item">Rename item providing the directory path.</param>
        /// <returns>The folder segment name, or an empty string when the level exceeds path depth.</returns>
        public static string Resolve(string arg, RenameItem item)
        {
            var level = string.IsNullOrWhiteSpace(arg) ? 1 : int.Parse(arg, CultureInfo.InvariantCulture);
            try
            {
                return DirectoryPathAncestor.GetSegmentName(item.Original.DirectoryPath, level);
            }
            catch (InvalidOperationException)
            {
                return string.Empty;
            }
        }
    }
}
