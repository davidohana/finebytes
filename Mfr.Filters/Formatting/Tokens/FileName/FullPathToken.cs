using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Parsed arguments for <c>&lt;full-path&gt;</c> (no parameters).
    /// </summary>
    internal readonly record struct FullPathFormatOptions
    {
        internal static FullPathFormatOptions Parse(string arg, string tokenDisplayName)
        {
            FormatOptionsParsing.RequireNoArgument(arg, tokenDisplayName);
            return default;
        }
    }

    /// <summary>
    /// Resolves the <c>&lt;full-path&gt;</c> token to the full file path.
    /// </summary>
    internal sealed class FullPathToken : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["full-path"];

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Thrown when arguments are supplied.</exception>
        public string Resolve(string arg, RenameItem item)
        {
            var tokenDisplayName = $"<{Names[0]}>";
            _ = FullPathFormatOptions.Parse(arg, tokenDisplayName: tokenDisplayName);
            return item.Original.FullPath;
        }
    }
}
