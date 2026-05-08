using System.Globalization;
using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.FilePropertiesGroup
{
    /// <summary>
    /// Parsed arguments for <c>&lt;file-count&gt;</c> (no parameters).
    /// </summary>
    internal readonly record struct FileCountFormatOptions
    {
        internal static FileCountFormatOptions Parse(string arg)
        {
            FormatOptionsParsing.RequireNoArgument(arg, "<file-count>");
            return default;
        }
    }

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
        public string Resolve(string arg, RenameItem item)
        {
            _ = FileCountFormatOptions.Parse(arg);
            var dir = item.Original.DirectoryPath;
            if (!Directory.Exists(dir))
                return string.Empty;
            return Directory.GetFileSystemEntries(dir).Length.ToString(CultureInfo.InvariantCulture);
        }
    }
}
