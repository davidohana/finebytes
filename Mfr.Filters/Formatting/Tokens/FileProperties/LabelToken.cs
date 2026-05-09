using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.FileProperties
{
    /// <summary>
    /// Parsed arguments for <c>&lt;label&gt;</c> (no parameters).
    /// </summary>
    internal readonly record struct LabelFormatOptions
    {
        internal static LabelFormatOptions Parse(string arg, string tokenDisplayName)
        {
            FormatOptionsParsing.RequireNoArgument(arg, tokenDisplayName);
            return default;
        }
    }

    /// <summary>
    /// Resolves the <c>&lt;label&gt;</c> token to the volume label of the drive holding the file.
    /// </summary>
    internal sealed class LabelToken : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["label"];

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Thrown when arguments are supplied.</exception>
        public string Resolve(string arg, RenameItem item)
        {
            var tokenDisplayName = $"<{Names[0]}>";
            _ = LabelFormatOptions.Parse(arg, tokenDisplayName: tokenDisplayName);
            var root = Path.GetPathRoot(item.Original.DirectoryPath);
            if (string.IsNullOrEmpty(root))
                return string.Empty;
            if (root.StartsWith(@"\\", StringComparison.Ordinal))
                return string.Empty;
            return new DriveInfo(root).VolumeLabel;
        }
    }
}
