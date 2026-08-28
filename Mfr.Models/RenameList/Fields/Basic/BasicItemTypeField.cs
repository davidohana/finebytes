using Mfr.Models.Rename;
using Mfr.Utils;

namespace Mfr.Models.RenameList.Fields.Basic
{
    /// <summary>
    /// File vs folder label.
    /// </summary>
    public sealed class BasicItemTypeField()
        : BasicRenameListField(
            propertyKey: Key,
            displayName: "File/Folder",
            defaultWidth: 100,
            supportsPreview: false,
            sortColumn: RenameListSortColumn.FileFolder
        )
    {
        /// <summary>
        /// MFR7 property key.
        /// </summary>
        public const string Key = "ItemType";

        /// <inheritdoc />
        public override string Resolve(FileMeta meta)
        {
            return meta.Attributes.IsDirectory() ? "Folder" : "File";
        }
    }
}
