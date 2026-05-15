using System.Globalization;

namespace Mfr.Filters.Formatting.Tokens.FileProperties
{
    /// <summary>
    /// Resolves the <c>&lt;file-count&gt;</c> token to the entry count in the file's parent directory (non-recursive).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns an empty string when the directory does not exist.
    /// </para>
    /// </remarks>
    internal sealed class FileCountToken : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["file-count"];

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Thrown when arguments are supplied.</exception>
        public Formatter Compile(string arg)
        {
            FormatOptionsParsing.RequireNoArgument(arg, FormatOptionsParsing.TokenDisplayName(this));
            return item =>
            {
                var dir = item.Original.DirectoryPath;
                if (!Directory.Exists(dir))
                    return string.Empty;
                return Directory.GetFileSystemEntries(dir).Length.ToString(CultureInfo.InvariantCulture);
            };
        }
    }
}
