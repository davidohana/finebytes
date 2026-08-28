using Mfr.Models.RenameList;

namespace Mfr.Filters.Formatting.Tokens.FileProperties
{
    /// <summary>
    /// Resolves the <c>&lt;file-count&gt;</c> token to the non-recursive file count for a folder or its parent.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For folder items, counts files inside the folder. For file items, counts files in the parent folder.
    /// Subfolders are not counted. Returns an empty string when the directory does not exist.
    /// </para>
    /// </remarks>
    internal sealed class FileCountToken : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["file-count"];

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Thrown when arguments are supplied.</exception>
        public Formatter Compile(string tokenArgs)
        {
            FormatOptionsParsing.RequireNoArgument(tokenArgs, FormatOptionsParsing.TokenDisplayName(this));
            return item => RenameListFieldDisplay.FormatFolderFileCount(item.Original);
        }
    }
}
