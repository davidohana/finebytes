using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Resolves the <c>&lt;full-name&gt;</c> token to the file name including extension.
    /// </summary>
    internal sealed class FullNameToken : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["full-name"];

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Thrown when arguments are supplied.</exception>
        public string Resolve(string arg, RenameItem item)
        {
            FormatOptionsParsing.RequireNoArgument(arg, FormatOptionsParsing.TokenDisplayName(this));
            return item.Original.Prefix + item.Original.Extension;
        }
    }
}
