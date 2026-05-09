using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Parsed arguments for <c>&lt;full-name&gt;</c> (no parameters).
    /// </summary>
    internal readonly record struct FullNameFormatOptions
    {
        internal static FullNameFormatOptions Parse(string arg, string tokenDisplayName)
        {
            FormatOptionsParsing.RequireNoArgument(arg, tokenDisplayName);
            return default;
        }
    }

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
            var tokenDisplayName = $"<{Names[0]}>";
            _ = FullNameFormatOptions.Parse(arg, tokenDisplayName: tokenDisplayName);
            return item.Original.Prefix + item.Original.Extension;
        }
    }
}
