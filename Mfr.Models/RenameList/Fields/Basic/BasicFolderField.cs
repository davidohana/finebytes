using Mfr.Models.Rename;

namespace Mfr.Models.RenameList.Fields.Basic
{
    /// <summary>
    /// Parent directory path.
    /// </summary>
    public sealed class BasicFolderField()
        : BasicRenameListField(
            propertyKey: Key,
            displayName: "Parent Folder",
            order: 1,
            sortColumn: RenameListSortColumn.ParentFolder,
            isDefaultVisible: true
        )
    {
        /// <summary>
        /// MFR7 property key.
        /// </summary>
        public const string Key = "Folder";

        /// <inheritdoc />
        public override string Resolve(FileMeta meta)
        {
            return meta.DirectoryPath;
        }
    }
}
