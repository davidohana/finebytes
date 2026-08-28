using Mfr.Models.Rename;

namespace Mfr.Models.RenameList.Fields.Basic
{
    /// <summary>
    /// Full file name including extension.
    /// </summary>
    public sealed class BasicFullNameField()
        : BasicRenameListField(
            propertyKey: Key,
            displayName: "Full File Name",
            order: 2,
            sortColumn: RenameListSortColumn.FullFileName,
            isDefaultVisible: true,
            isDefaultVisiblePreview: true
        )
    {
        /// <summary>
        /// MFR7 property key.
        /// </summary>
        public const string Key = "FullName";

        /// <inheritdoc />
        public override string Resolve(FileMeta meta)
        {
            return meta.Prefix + meta.Extension;
        }
    }
}
