using Mfr.Models.RenameList.Fields.Basic;

namespace Mfr.Models.RenameList
{
    /// <summary>
    /// One Auto-Sort key: original catalog field plus ascending/descending.
    /// </summary>
    /// <param name="FieldKey">Original field key to compare.</param>
    /// <param name="Descending">When <see langword="true"/>, reverse that field's order.</param>
    public readonly record struct RenameListSortKey(RenameListFieldKey FieldKey, bool Descending = false)
    {
        /// <summary>
        /// Default Auto-Sort keys: File/Folder, Parent Folder, then Full File Name. Empty session value disables Auto-Sort.
        /// </summary>
        public static IReadOnlyList<RenameListSortKey> DefaultKeys { get; } =
        [
            new(RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.ItemType)),
            new(RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Folder)),
            new(RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName)),
        ];
    }
}
