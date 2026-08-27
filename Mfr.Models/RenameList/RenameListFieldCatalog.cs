namespace Mfr.Models.RenameList
{
    /// <summary>
    /// Static registry of Rename List field definitions (Phase 5: File Name group only).
    /// </summary>
    public static class RenameListFieldCatalog
    {
        /// <summary>
        /// MFR7 Basic property group id.
        /// </summary>
        public const string BasicGroupId = "Basic";

        /// <summary>
        /// MFR7 Basic group display name ("File Name").
        /// </summary>
        public const string BasicGroupDisplayName = "File Name";

        private static readonly RenameListFieldDefinition[] _BasicDefinitions =
        [
            _Define(
                RenameListBasicPropertyKeys.ItemType,
                "File/Folder",
                defaultWidth: 50,
                isSortable: true,
                supportsPreview: false
            ),
            _Define(
                RenameListBasicPropertyKeys.Folder,
                "Parent Folder",
                defaultWidth: 180,
                isSortable: true,
                supportsPreview: true
            ),
            _Define(
                RenameListBasicPropertyKeys.FullName,
                "Full File Name",
                defaultWidth: 180,
                isSortable: true,
                supportsPreview: true
            ),
            _Define(
                RenameListBasicPropertyKeys.FullPath,
                "Full File Path",
                defaultWidth: 180,
                isSortable: true,
                supportsPreview: true
            ),
            _Define(
                RenameListBasicPropertyKeys.Name,
                "File Name",
                defaultWidth: 150,
                isSortable: true,
                supportsPreview: true
            ),
            _Define(
                RenameListBasicPropertyKeys.Extension,
                "File Extension",
                defaultWidth: 100,
                isSortable: true,
                supportsPreview: true
            ),
            _Define(
                RenameListBasicPropertyKeys.FileNameNumeric,
                "File Name Numeric Value",
                defaultWidth: 50,
                isSortable: true,
                supportsPreview: false
            ),
            _Define(
                RenameListBasicPropertyKeys.FileNameLength,
                "File Name Length",
                defaultWidth: 40,
                isSortable: true,
                supportsPreview: true
            ),
            _Define(
                RenameListBasicPropertyKeys.FullPathLength,
                "Full Path Name Length",
                defaultWidth: 40,
                isSortable: true,
                supportsPreview: true
            ),
        ];

        private static readonly Dictionary<
            (string GroupId, string PropertyKey),
            RenameListFieldDefinition
        > _definitionByKey = _BuildDefinitionByKey();

        /// <summary>
        /// All registered field definitions in shuttle order.
        /// </summary>
        public static IReadOnlyList<RenameListFieldDefinition> All { get; } = _BasicDefinitions;

        /// <summary>
        /// File Name group definitions.
        /// </summary>
        public static IReadOnlyList<RenameListFieldDefinition> BasicFields { get; } = _BasicDefinitions;

        /// <summary>
        /// Default visible columns (MFR7 <c>RenameGrid</c>).
        /// </summary>
        public static IReadOnlyList<RenameListFieldKey> DefaultVisibleColumns { get; } =
        [
            RenameListFieldKey.Original(BasicGroupId, RenameListBasicPropertyKeys.ItemType),
            RenameListFieldKey.Original(BasicGroupId, RenameListBasicPropertyKeys.Folder),
            RenameListFieldKey.Original(BasicGroupId, RenameListBasicPropertyKeys.FullName),
            RenameListFieldKey.Preview(BasicGroupId, RenameListBasicPropertyKeys.FullName),
        ];

        /// <summary>
        /// Returns definitions for one property group.
        /// </summary>
        /// <param name="groupId">Property group id.</param>
        /// <returns>Definitions in catalog order; empty when the group is unknown.</returns>
        public static IReadOnlyList<RenameListFieldDefinition> GetDefinitionsForGroup(string groupId)
        {
            if (!string.Equals(groupId, BasicGroupId, StringComparison.Ordinal))
            {
                return [];
            }

            return BasicFields;
        }

        /// <summary>
        /// Looks up a field definition by group and property key.
        /// </summary>
        /// <param name="groupId">Property group id.</param>
        /// <param name="propertyKey">Property key within the group.</param>
        /// <param name="definition">Matching definition when found.</param>
        /// <returns><see langword="true"/> when the property is registered.</returns>
        public static bool TryGetDefinition(
            string groupId,
            string propertyKey,
            out RenameListFieldDefinition? definition
        )
        {
            if (groupId.Length == 0 || propertyKey.Length == 0)
            {
                definition = null;
                return false;
            }

            if (!_definitionByKey.TryGetValue((groupId, propertyKey), out var found))
            {
                definition = null;
                return false;
            }

            definition = found;
            return true;
        }

        /// <summary>
        /// Looks up a field definition for <paramref name="key"/> (ignores <see cref="RenameListFieldKey.IsPreview"/>).
        /// </summary>
        /// <param name="key">Field key.</param>
        /// <param name="definition">Matching definition when found.</param>
        /// <returns><see langword="true"/> when the property is registered.</returns>
        public static bool TryGetDefinition(RenameListFieldKey key, out RenameListFieldDefinition? definition)
        {
            return TryGetDefinition(key.GroupId, key.PropertyKey, out definition);
        }

        /// <summary>
        /// Maps an engine Auto-Sort column to the corresponding original field key.
        /// </summary>
        /// <param name="column">Engine sort column.</param>
        /// <param name="key">Mapped original field key when supported.</param>
        /// <returns><see langword="true"/> when <paramref name="column"/> maps to a catalog field.</returns>
        public static bool TryMapSortColumn(RenameListSortColumn column, out RenameListFieldKey key)
        {
            var propertyKey = column switch
            {
                RenameListSortColumn.FileFolder => RenameListBasicPropertyKeys.ItemType,
                RenameListSortColumn.ParentFolder => RenameListBasicPropertyKeys.Folder,
                RenameListSortColumn.FullFileName => RenameListBasicPropertyKeys.FullName,
                RenameListSortColumn.FullPath => RenameListBasicPropertyKeys.FullPath,
                _ => null,
            };

            if (propertyKey is null)
            {
                key = default;
                return false;
            }

            key = RenameListFieldKey.Original(BasicGroupId, propertyKey);
            return true;
        }

        /// <summary>
        /// Maps an original (non-preview) field key to an engine Auto-Sort column.
        /// </summary>
        /// <param name="key">Field key.</param>
        /// <param name="column">Mapped sort column when supported.</param>
        /// <returns>
        /// <see langword="true"/> when <paramref name="key"/> is a known original field with engine sort support.
        /// </returns>
        public static bool TryMapFieldKeyToSortColumn(RenameListFieldKey key, out RenameListSortColumn column)
        {
            if (key.IsPreview)
            {
                column = default;
                return false;
            }

            if (!string.Equals(key.GroupId, BasicGroupId, StringComparison.Ordinal))
            {
                column = default;
                return false;
            }

            var mapped = key.PropertyKey switch
            {
                RenameListBasicPropertyKeys.ItemType => RenameListSortColumn.FileFolder,
                RenameListBasicPropertyKeys.Folder => RenameListSortColumn.ParentFolder,
                RenameListBasicPropertyKeys.FullName => RenameListSortColumn.FullFileName,
                RenameListBasicPropertyKeys.FullPath => RenameListSortColumn.FullPath,
                _ => (RenameListSortColumn?)null,
            };

            if (mapped is null)
            {
                column = default;
                return false;
            }

            column = mapped.Value;
            return true;
        }

        private static Dictionary<
            (string GroupId, string PropertyKey),
            RenameListFieldDefinition
        > _BuildDefinitionByKey()
        {
            var definitionByKey = new Dictionary<(string GroupId, string PropertyKey), RenameListFieldDefinition>(
                _BasicDefinitions.Length,
                GroupPropertyKeyComparer.Instance
            );
            foreach (var definition in _BasicDefinitions)
            {
                definitionByKey[(definition.GroupId, definition.PropertyKey)] = definition;
            }

            return definitionByKey;
        }

        private static RenameListFieldDefinition _Define(
            string propertyKey,
            string displayName,
            int defaultWidth,
            bool isSortable,
            bool supportsPreview
        )
        {
            return new RenameListFieldDefinition(
                BasicGroupId,
                propertyKey,
                displayName,
                BasicGroupDisplayName,
                defaultWidth,
                isSortable,
                supportsPreview
            );
        }

        private sealed class GroupPropertyKeyComparer : IEqualityComparer<(string GroupId, string PropertyKey)>
        {
            public static GroupPropertyKeyComparer Instance { get; } = new();

            public bool Equals((string GroupId, string PropertyKey) x, (string GroupId, string PropertyKey) y)
            {
                return string.Equals(x.GroupId, y.GroupId, StringComparison.Ordinal)
                    && string.Equals(x.PropertyKey, y.PropertyKey, StringComparison.Ordinal);
            }

            public int GetHashCode((string GroupId, string PropertyKey) obj)
            {
                return HashCode.Combine(
                    StringComparer.Ordinal.GetHashCode(obj.GroupId),
                    StringComparer.Ordinal.GetHashCode(obj.PropertyKey)
                );
            }
        }
    }

    /// <summary>
    /// Property keys for the MFR7 Basic ("File Name") group.
    /// </summary>
    public static class RenameListBasicPropertyKeys
    {
        /// <summary>File vs folder label.</summary>
        public const string ItemType = "ItemType";

        /// <summary>Parent directory path.</summary>
        public const string Folder = "Folder";

        /// <summary>File name including extension.</summary>
        public const string FullName = "FullName";

        /// <summary>Absolute full path.</summary>
        public const string FullPath = "FullPath";

        /// <summary>File name without extension.</summary>
        public const string Name = "Name";

        /// <summary>File extension without leading dot.</summary>
        public const string Extension = "Extension";

        /// <summary>First numeric run in the full file name.</summary>
        public const string FileNameNumeric = "FileNameNumeric";

        /// <summary>Full file name character count.</summary>
        public const string FileNameLength = "FileNameLength";

        /// <summary>Full path character count.</summary>
        public const string FullPathLength = "FullPathLength";

        /// <summary>All Basic property keys in catalog order.</summary>
        public static IReadOnlyList<string> All { get; } =
        [ItemType, Folder, FullName, FullPath, Name, Extension, FileNameNumeric, FileNameLength, FullPathLength];
    }
}
