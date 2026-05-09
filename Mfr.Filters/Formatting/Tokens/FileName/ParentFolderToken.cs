using System.Globalization;
using Mfr.Models;
using Mfr.Utils;

namespace Mfr.Filters.Formatting.Tokens.FileName
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
        /// <summary>
        /// Parsed arguments for <c>&lt;parent-folder&gt;</c>.
        /// </summary>
        /// <param name="Level">1-based ancestor segment index (<c>1</c> = immediate parent).</param>
        private sealed record Options(int Level);

        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["parent-folder"];

        /// <inheritdoc />
        public string Resolve(string arg, RenameItem item)
        {
            var options = _ParseOptions(arg);
            try
            {
                return DirectoryPathAncestor.GetSegmentName(item.Original.DirectoryPath, options.Level);
            }
            catch (InvalidOperationException)
            {
                return string.Empty;
            }
        }

        private Options _ParseOptions(string arg)
        {
            var level = string.IsNullOrWhiteSpace(arg) ? 1 : int.Parse(arg, CultureInfo.InvariantCulture);
            return new Options(Level: level);
        }
    }
}
