using Mfr.Models.Rename;

namespace Mfr.Models.RenameList.Fields.Basic
{
    /// <summary>
    /// Absolute full path.
    /// </summary>
    public sealed class BasicFullPathField()
        : BasicRenameListField(
            propertyKey: Key,
            displayName: "Full File Path",
            order: 3,
            sortColumn: RenameListSortColumn.FullPath
        )
    {
        /// <summary>
        /// MFR7 property key.
        /// </summary>
        public const string Key = "FullPath";

        /// <inheritdoc />
        public override string Resolve(FileMeta meta)
        {
            return meta.FullPath;
        }
    }
}
