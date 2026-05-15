namespace Mfr.Filters.Formatting.Tokens.FileProperties
{
    /// <summary>
    /// Resolves the <c>&lt;label&gt;</c> token to the volume label of the drive holding the file.
    /// </summary>
    internal sealed class LabelToken : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["label"];

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Thrown when arguments are supplied.</exception>
        public Formatter Compile(string arg)
        {
            FormatOptionsParsing.RequireNoArgument(arg, FormatOptionsParsing.TokenDisplayName(this));
            return item =>
            {
                var root = Path.GetPathRoot(item.Original.DirectoryPath);
                if (string.IsNullOrEmpty(root))
                    return string.Empty;
                if (root.StartsWith(@"\\", StringComparison.Ordinal))
                    return string.Empty;
                return new DriveInfo(root).VolumeLabel;
            };
        }
    }
}
