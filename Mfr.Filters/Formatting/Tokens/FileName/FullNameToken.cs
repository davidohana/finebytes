using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Parsed arguments for <c>&lt;full-name&gt;</c> (no parameters).
    /// </summary>
    internal readonly record struct FullNameFormatOptions
    {
        internal static FullNameFormatOptions Parse(string arg)
        {
            FormatOptionsParsing.RequireNoArgument(arg, "<full-name>");
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
            _ = FullNameFormatOptions.Parse(arg);
            return item.Original.Prefix + item.Original.Extension;
        }
    }
}
