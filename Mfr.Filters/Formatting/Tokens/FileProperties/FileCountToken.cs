using Mfr.Models.RenameList;
using Mfr.Models.RenameList.Fields.Extended;

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
    [FormatTokenInfo(
        "Folder File Count",
        "File Properties",
        "Use the count of files in the item's folder",
        "file-count"
    )]
    internal sealed class FileCountToken : IFormatToken, IRenameListMappedFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["file-count"];

        /// <inheritdoc />
        public bool TryGetFixedField(out string groupId, out string propertyKey)
        {
            groupId = ExtendedRenameListFields.Group;
            propertyKey = ExtendedRenameListFields.Key.FileCount;
            return true;
        }

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Thrown when arguments are supplied.</exception>
        public Formatter Compile(string tokenArgs)
        {
            FormatOptionsParsing.RequireNoArgument(tokenArgs, FormatOptionsParsing.TokenDisplayName(this));
            return item => RenameListFieldDisplay.FormatFolderFileCount(item.Original);
        }
    }
}
