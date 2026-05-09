using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Resolves the <c>&lt;full-path&gt;</c> token to the full file path.
    /// </summary>
    internal sealed class FullPathToken : IFormatToken
    {
        /// <summary>
        /// Parsed arguments for <c>&lt;full-path&gt;</c> (no parameters).
        /// </summary>
        private readonly record struct FullPathFormatOptions;

        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["full-path"];

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Thrown when arguments are supplied.</exception>
        public string Resolve(string arg, RenameItem item)
        {
            _ = _ParseOptions(arg);
            return item.Original.FullPath;
        }

        private FullPathFormatOptions _ParseOptions(string arg)
        {
            FormatOptionsParsing.RequireNoArgument(arg, FormatOptionsParsing.TokenDisplayName(this));
            return default;
        }
    }
}
