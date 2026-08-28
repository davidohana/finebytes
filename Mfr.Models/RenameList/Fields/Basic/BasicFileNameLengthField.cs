using Mfr.Models.Rename;

namespace Mfr.Models.RenameList.Fields.Basic
{
    /// <summary>
    /// Length of the full file name including extension.
    /// </summary>
    public sealed class BasicFileNameLengthField()
        : BasicRenameListField(propertyKey: Key, displayName: "File Name Length", order: 7, defaultWidth: 40)
    {
        /// <summary>
        /// MFR7 property key.
        /// </summary>
        public const string Key = "FileNameLength";

        /// <inheritdoc />
        public override string Resolve(FileMeta meta)
        {
            return (meta.Prefix + meta.Extension).Length.ToString();
        }
    }
}
