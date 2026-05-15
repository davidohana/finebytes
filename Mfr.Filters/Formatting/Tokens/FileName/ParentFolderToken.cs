using System.Globalization;
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
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["parent-folder"];

        /// <inheritdoc />
        public Formatter Compile(string tokenArgs)
        {
            var level = string.IsNullOrWhiteSpace(tokenArgs) ? 1 : int.Parse(tokenArgs, CultureInfo.InvariantCulture);
            return item =>
            {
                try
                {
                    return DirectoryPathAncestor.GetSegmentName(item.Original.DirectoryPath, level);
                }
                catch (InvalidOperationException)
                {
                    return string.Empty;
                }
            };
        }
    }
}
