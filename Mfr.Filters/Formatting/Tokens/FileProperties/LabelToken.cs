using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.FileProperties
{
    /// <summary>
    /// Resolves the <c>&lt;label&gt;</c> token to the volume label of the drive holding the file.
    /// </summary>
    internal sealed class LabelToken : IFormatToken
    {
        /// <summary>
        /// Parsed arguments for <c>&lt;label&gt;</c> (no parameters).
        /// </summary>
        private readonly record struct LabelFormatOptions;

        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["label"];

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Thrown when arguments are supplied.</exception>
        public string Resolve(string arg, RenameItem item)
        {
            _ = _ParseOptions(arg);
            var root = Path.GetPathRoot(item.Original.DirectoryPath);
            if (string.IsNullOrEmpty(root))
                return string.Empty;
            if (root.StartsWith(@"\\", StringComparison.Ordinal))
                return string.Empty;
            return new DriveInfo(root).VolumeLabel;
        }

        private LabelFormatOptions _ParseOptions(string arg)
        {
            FormatOptionsParsing.RequireNoArgument(arg, FormatOptionsParsing.TokenDisplayName(this));
            return default;
        }
    }
}
