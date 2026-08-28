using Mfr.Models.Rename;
using Mfr.Models.RenameList;
using Mfr.Models.RenameList.Fields.Basic;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// One row in the Rename List grid.
    /// </summary>
    public sealed class RenameListEntry
    {
        /// <summary>
        /// Gets the engine item this row represents.
        /// </summary>
        internal RenameItem EngineItem { get; init; } = null!;

        /// <summary>
        /// Returns display text for one catalog field on this row.
        /// </summary>
        /// <param name="key">Field key (original or preview).</param>
        /// <returns>Resolved display string.</returns>
        public string GetFieldText(RenameListFieldKey key)
        {
            return RenameListFieldCatalog.Resolve(EngineItem, key);
        }

        /// <summary>
        /// Gets the file-or-folder label shown in the File/Folder column.
        /// </summary>
        public string FileFolder => GetFieldText(_ItemTypeKey);

        /// <summary>
        /// Gets the parent folder path shown in the Parent Folder column.
        /// </summary>
        public string ParentFolder => GetFieldText(_FolderKey);

        /// <summary>
        /// Gets the original full file name shown in the Full File Name column.
        /// </summary>
        public string FullFileName => GetFieldText(_FullNameOriginalKey);

        /// <summary>
        /// Gets the absolute path used for Auto-Sort (and as a CollectionView sort member).
        /// </summary>
        public string FullPath => GetFieldText(_FullPathKey);

        /// <summary>
        /// Gets the preview full file name shown in the Full File Name (Preview) column.
        /// </summary>
        public string FullFileNamePreview => GetFieldText(_FullNamePreviewKey);

        /// <summary>
        /// Builds a grid row from a rename item (identity preview until filter preview exists).
        /// </summary>
        /// <param name="item">Engine rename item.</param>
        /// <returns>Row view model for the Rename List grid.</returns>
        public static RenameListEntry ToEntry(RenameItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            return new RenameListEntry { EngineItem = item };
        }

        private static readonly RenameListFieldKey _ItemTypeKey = RenameListFieldKey.Original(
            BasicRenameListField.Group,
            BasicItemTypeField.Key
        );

        private static readonly RenameListFieldKey _FolderKey = RenameListFieldKey.Original(
            BasicRenameListField.Group,
            BasicFolderField.Key
        );

        private static readonly RenameListFieldKey _FullNameOriginalKey = RenameListFieldKey.Original(
            BasicRenameListField.Group,
            BasicFullNameField.Key
        );

        private static readonly RenameListFieldKey _FullNamePreviewKey = RenameListFieldKey.Preview(
            BasicRenameListField.Group,
            BasicFullNameField.Key
        );

        private static readonly RenameListFieldKey _FullPathKey = RenameListFieldKey.Original(
            BasicRenameListField.Group,
            BasicFullPathField.Key
        );
    }
}
