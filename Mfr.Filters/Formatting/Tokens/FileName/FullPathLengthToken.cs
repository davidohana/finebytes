using System.Globalization;
using Mfr.Models.RenameList.Fields.Basic;

namespace Mfr.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Resolves the <c>&lt;full-path-length&gt;</c> token to the character length of the preview full path.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uses preview so the value tracks predicted renames and moves.
    /// </para>
    /// </remarks>
    [FormatTokenInfo(
        PathFieldLabels.FullPathLength,
        PathFieldLabels.FileName,
        "Character length of the preview full path",
        "full-path-length"
    )]
    internal sealed class FullPathLengthToken : IFormatToken, IRenameListMappedFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["full-path-length"];

        /// <inheritdoc />
        public bool TryGetFixedField(out string groupId, out string propertyKey)
        {
            groupId = BasicRenameListField.Group;
            propertyKey = BasicRenameListFields.Key.FullPathLength;
            return true;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentException">Thrown when arguments are supplied.</exception>
        public Formatter Compile(string tokenArgs)
        {
            FormatOptionsParsing.RequireNoArgument(tokenArgs, FormatOptionsParsing.TokenDisplayName(this));
            return item => item.Preview.FullPath.Length.ToString(CultureInfo.InvariantCulture);
        }
    }
}
