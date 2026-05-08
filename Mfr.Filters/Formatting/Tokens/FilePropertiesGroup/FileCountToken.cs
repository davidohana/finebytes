using System.Globalization;
using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.FilePropertiesGroup
{
    /// <summary>
    /// Resolves the <c>&lt;file-count&gt;</c> token to the entry count in the file's parent directory (non-recursive).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns an empty string when the directory does not exist.
    /// </para>
    /// </remarks>
    internal sealed class FileCountToken : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["file-count"];

        /// <inheritdoc />
        public string Resolve(string arg, RenameItem item)
        {
            var dir = item.Original.DirectoryPath;
            if (!Directory.Exists(dir))
                return string.Empty;
            return Directory.GetFileSystemEntries(dir).Length.ToString(CultureInfo.InvariantCulture);
        }
    }
}
