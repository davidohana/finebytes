using Mfr.Models.RenameList.Fields.Basic;

namespace Mfr.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Resolves the <c>&lt;file-extension&gt;</c> and <c>&lt;ext&gt;</c> tokens to the preview extension (without leading dot).
    /// </summary>
    [FormatTokenInfo(
        PathFieldLabels.FileExtension,
        PathFieldLabels.FileName,
        "Use the filename extension",
        "file-extension"
    )]
    internal sealed class FileExtensionToken : IFormatToken, IRenameListMappedFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["file-extension", "ext"];

        /// <inheritdoc />
        public bool TryGetFixedField(out string groupId, out string propertyKey)
        {
            groupId = BasicRenameListField.Group;
            propertyKey = BasicRenameListFields.Key.Extension;
            return true;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentException">Thrown when arguments are supplied.</exception>
        public Formatter Compile(string tokenArgs)
        {
            FormatOptionsParsing.RequireNoArgument(tokenArgs, FormatOptionsParsing.TokenDisplayName(this));
            return item => item.Preview.Extension;
        }
    }
}
