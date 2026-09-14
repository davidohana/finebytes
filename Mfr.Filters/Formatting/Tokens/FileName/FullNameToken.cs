using Mfr.Models.RenameList.Fields.Basic;

namespace Mfr.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Resolves the <c>&lt;full-name&gt;</c> token to the preview <see cref="FileMeta.FullFileName"/>.
    /// </summary>
    [FormatTokenInfo(PathFieldLabels.FullFileName, PathFieldLabels.FileName, PathFieldTips.FullFileName, "full-name")]
    internal sealed class FullNameToken : IFormatToken, IRenameListMappedFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["full-name"];

        /// <inheritdoc />
        public bool TryGetFixedField(out string groupId, out string propertyKey)
        {
            groupId = BasicRenameListField.Group;
            propertyKey = BasicRenameListFields.Key.FullName;
            return true;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentException">Thrown when arguments are supplied.</exception>
        public Formatter Compile(string tokenArgs)
        {
            FormatOptionsParsing.RequireNoArgument(tokenArgs, FormatOptionsParsing.TokenDisplayName(this));
            return item => item.Preview.FullFileName;
        }
    }
}
