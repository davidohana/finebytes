using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.FileProperties
{
    /// <summary>
    /// Parsed arguments for <c>&lt;drive-letter&gt;</c> (no parameters).
    /// </summary>
    internal readonly record struct DriveLetterFormatOptions
    {
        internal static DriveLetterFormatOptions Parse(string arg, string tokenDisplayName)
        {
            FormatOptionsParsing.RequireNoArgument(arg, tokenDisplayName);
            return default;
        }
    }

    /// <summary>
    /// Resolves the <c>&lt;drive-letter&gt;</c> token.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns the drive letter (e.g. <c>C:</c>) for local paths, or <c>$</c> for UNC paths.
    /// </para>
    /// </remarks>
    internal sealed class DriveLetterToken : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["drive-letter"];

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Thrown when arguments are supplied.</exception>
        public string Resolve(string arg, RenameItem item)
        {
            var tokenDisplayName = $"<{Names[0]}>";
            _ = DriveLetterFormatOptions.Parse(arg, tokenDisplayName: tokenDisplayName);
            var root = Path.GetPathRoot(item.Original.DirectoryPath) ?? string.Empty;
            if (root.StartsWith(@"\\", StringComparison.Ordinal))
                return "$";
            return root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}
