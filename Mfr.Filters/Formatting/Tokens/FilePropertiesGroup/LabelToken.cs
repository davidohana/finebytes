using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.FilePropertiesGroup
{
    /// <summary>
    /// Resolves the <c>&lt;label&gt;</c> token to the volume label of the drive holding the file.
    /// </summary>
    internal sealed class LabelToken : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["label"];

        /// <inheritdoc />
        public string Resolve(string arg, RenameItem item)
        {
            var root = Path.GetPathRoot(item.Original.DirectoryPath);
            if (string.IsNullOrEmpty(root))
                return string.Empty;
            return new DriveInfo(root).VolumeLabel;
        }
    }
}
