using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Resolves the <c>&lt;full-name&gt;</c> token to the file name including extension.
    /// </summary>
    internal sealed class FullNameToken : IFormatToken
    {
        /// <summary>
        /// Parsed arguments for <c>&lt;full-name&gt;</c> (no parameters).
        /// </summary>
        private readonly record struct FullNameFormatOptions;

        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["full-name"];

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Thrown when arguments are supplied.</exception>
        public string Resolve(string arg, RenameItem item)
        {
            _ = _ParseOptions(arg);
            return item.Original.Prefix + item.Original.Extension;
        }

        private FullNameFormatOptions _ParseOptions(string arg)
        {
            FormatOptionsParsing.RequireNoArgument(arg, FormatOptionsParsing.TokenDisplayName(this));
            return default;
        }
    }
}
