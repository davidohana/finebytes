using System.Globalization;
using Mfr.Models;
using Mfr.Utils;

namespace Mfr.Filters.Formatting.Tokens.FileNameGroup
{
    /// <summary>
    /// Resolves the <c>&lt;parent-folder&gt;</c> and <c>&lt;parent-folder:level&gt;</c> tokens.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Argument is an optional 1-based ancestor level: <c>1</c> = immediate parent (default),
    /// <c>2</c> = grandparent, and so on. Returns an empty string when the level exceeds path depth.
    /// </para>
    /// </remarks>
    internal sealed class ParentFolderToken : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["parent-folder"];

        /// <inheritdoc />
        public string Resolve(string arg, RenameItem item)
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
