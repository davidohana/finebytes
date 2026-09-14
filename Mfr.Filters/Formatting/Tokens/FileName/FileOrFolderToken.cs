using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Utils;

namespace Mfr.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Resolves the <c>&lt;file-or-folder&gt;</c> token to <c>File</c> or <c>Folder</c> from original attributes.
    /// </summary>
    [FormatTokenInfo(
        PathFieldLabels.FileOrFolder,
        PathFieldLabels.FileName,
        "Whether the item is a file or a folder",
        "file-or-folder"
    )]
    internal sealed class FileOrFolderToken : IFormatToken, IRenameListMappedFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["file-or-folder"];

        /// <inheritdoc />
        public bool TryGetFixedField(out string groupId, out string propertyKey)
        {
            groupId = BasicRenameListField.Group;
            propertyKey = BasicRenameListFields.Key.ItemType;
            return true;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentException">Thrown when arguments are supplied.</exception>
        public Formatter Compile(string tokenArgs)
        {
            FormatOptionsParsing.RequireNoArgument(tokenArgs, FormatOptionsParsing.TokenDisplayName(this));
            return item => item.Original.Attributes.IsDirectory() ? "Folder" : "File";
        }
    }
}
