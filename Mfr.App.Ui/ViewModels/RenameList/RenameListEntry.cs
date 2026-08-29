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
        private static readonly RenameListFieldKey _itemTypeKey = RenameListFieldKey.Original(
            BasicRenameListField.Group,
            BasicRenameListFields.Key.ItemType
        );

        private static readonly RenameListFieldKey _folderKey = RenameListFieldKey.Original(
            BasicRenameListField.Group,
            BasicRenameListFields.Key.Folder
        );

        private static readonly RenameListFieldKey _fullNameKey = RenameListFieldKey.Original(
            BasicRenameListField.Group,
            BasicRenameListFields.Key.FullName
        );

        private static readonly RenameListFieldKey _fullPathKey = RenameListFieldKey.Original(
            BasicRenameListField.Group,
            BasicRenameListFields.Key.FullPath
        );

        private static readonly RenameListFieldKey _fullNamePreviewKey = RenameListFieldKey.Preview(
            BasicRenameListField.Group,
            BasicRenameListFields.Key.FullName
        );

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
        /// Returns whether this row path is missing from disk (whole-row gray; not a metadata load error).
        /// </summary>
        public bool IsMissingFromDisk => RenameListDiskPaths.IsMissingFromDisk(EngineItem);

        /// <summary>
        /// Returns whether this original cell failed to load metadata from disk.
        /// </summary>
        /// <param name="key">Field key (original or preview).</param>
        /// <returns><see langword="true"/> when the cell should show the load-error sentinel.</returns>
        public bool IsLoadError(RenameListFieldKey key)
        {
            return RenameListFieldCatalog.HasLoadError(EngineItem, key);
        }

        /// <summary>
        /// Gets the file-or-folder label shown in the File/Folder column.
        /// </summary>
        public string FileFolder => GetFieldText(_itemTypeKey);

        /// <summary>
        /// Gets the parent folder path shown in the Parent Folder column.
        /// </summary>
        public string ParentFolder => GetFieldText(_folderKey);

        /// <summary>
        /// Gets the original full file name shown in the Full File Name column.
        /// </summary>
        public string FullFileName => GetFieldText(_fullNameKey);

        /// <summary>
        /// Gets the original absolute path.
        /// </summary>
        public string FullPath => GetFieldText(_fullPathKey);

        /// <summary>
        /// Gets the preview full file name shown in the Full File Name (Preview) column.
        /// </summary>
        public string FullFileNamePreview => GetFieldText(_fullNamePreviewKey);

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
    }
}
