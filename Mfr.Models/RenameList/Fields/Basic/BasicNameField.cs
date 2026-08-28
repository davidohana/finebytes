using Mfr.Models.Rename;

namespace Mfr.Models.RenameList.Fields.Basic
{
    /// <summary>
    /// File name without extension.
    /// </summary>
    public sealed class BasicNameField()
        : BasicRenameListField(propertyKey: Key, displayName: "File Name", defaultWidth: 150)
    {
        /// <summary>
        /// MFR7 property key.
        /// </summary>
        public const string Key = "Name";

        /// <inheritdoc />
        public override string Resolve(FileMeta meta)
        {
            return meta.Prefix;
        }
    }
}
