using System.Diagnostics.CodeAnalysis;
using Mfr.Models.Rename;
using Mfr.Utils;

namespace Mfr.Models.RenameList
{
    /// <summary>
    /// Product catalog of Rename List grid/sort fields.
    /// <para>
    /// Built by reflecting concrete <see cref="RenameListField"/> subclasses, like
    /// <c>FilterCatalog</c> does for filters.
    /// </para>
    /// </summary>
    public static class RenameListFieldCatalog
    {
        /// <summary>
        /// All registered fields in group order, then field order.
        /// </summary>
        public static IReadOnlyList<RenameListField> All { get; } = _DiscoverFields();

        private static readonly Dictionary<(string GroupId, string PropertyKey), RenameListField> _fieldByKey =
            All.ToDictionary(field => (field.GroupId, field.PropertyKey));

        /// <summary>
        /// Default visible columns (MFR7 <c>RenameGrid</c>).
        /// </summary>
        public static IReadOnlyList<RenameListFieldKey> DefaultVisibleColumns { get; } = _DefaultVisibleColumns(All);

        /// <summary>
        /// Returns fields for one property group.
        /// </summary>
        /// <param name="groupId">Property group id.</param>
        /// <returns>Fields in catalog order; empty when the group is unknown.</returns>
        public static IReadOnlyList<RenameListField> GetFieldsForGroup(string groupId)
        {
            return [.. All.Where(field => string.Equals(field.GroupId, groupId, StringComparison.Ordinal))];
        }

        /// <summary>
        /// Looks up a catalog field by group and property key.
        /// </summary>
        /// <param name="groupId">Property group id.</param>
        /// <param name="propertyKey">Property key within the group.</param>
        /// <param name="field">Matching field when found.</param>
        /// <returns><see langword="true"/> when the property is registered.</returns>
        public static bool TryGetField(
            string groupId,
            string propertyKey,
            [NotNullWhen(true)] out RenameListField? field
        )
        {
            if (string.IsNullOrEmpty(groupId) || string.IsNullOrEmpty(propertyKey))
            {
                field = null;
                return false;
            }

            return _fieldByKey.TryGetValue((groupId, propertyKey), out field);
        }

        /// <summary>
        /// Looks up a catalog field for <paramref name="key"/> (ignores preview flag).
        /// </summary>
        /// <param name="key">Field key.</param>
        /// <param name="field">Matching field when found.</param>
        /// <returns><see langword="true"/> when the property is registered.</returns>
        public static bool TryGetField(RenameListFieldKey key, [NotNullWhen(true)] out RenameListField? field)
        {
            return TryGetField(key.GroupId, key.PropertyKey, out field);
        }

        /// <summary>
        /// Maps an engine Auto-Sort column to the corresponding original field key.
        /// </summary>
        /// <param name="column">Engine sort column.</param>
        /// <param name="key">Mapped original field key when supported.</param>
        /// <returns><see langword="true"/> when <paramref name="column"/> maps to a catalog field.</returns>
        public static bool TryMapSortColumn(RenameListSortColumn column, out RenameListFieldKey key)
        {
            foreach (var field in All)
            {
                if (field.SortColumn != column)
                {
                    continue;
                }

                key = field.OriginalKey;
                return true;
            }

            key = default;
            return false;
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

            if (!TryGetField(key, out var field) || field.SortColumn is not { } sortColumn)
            {
                column = default;
                return false;
            }

            column = sortColumn;
            return true;
        }

        /// <summary>
        /// Returns the display text for one field on a rename item.
        /// </summary>
        /// <param name="item">Engine rename item.</param>
        /// <param name="key">Field key (original or preview).</param>
        /// <returns>Display string for the grid or sort shuttle.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not registered in the catalog.</exception>
        public static string Resolve(RenameItem item, RenameListFieldKey key)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (!TryGetField(key, out var field))
            {
                throw new ArgumentException(
                    $"Unknown Rename List field '{key.GroupId}/{key.PropertyKey}'.",
                    nameof(key)
                );
            }

            var meta = key.IsPreview ? item.Preview : item.Original;
            return field.Resolve(meta);
        }

        private static List<RenameListFieldKey> _DefaultVisibleColumns(IReadOnlyList<RenameListField> fields)
        {
            var keys = new List<RenameListFieldKey>();
            foreach (var field in fields)
            {
                if (field.IsDefaultVisible)
                {
                    keys.Add(field.OriginalKey);
                }

                if (field.IsDefaultVisiblePreview)
                {
                    keys.Add(field.PreviewKey);
                }
            }

            return keys;
        }

        private static List<RenameListField> _DiscoverFields()
        {
            var fields = typeof(RenameListFieldCatalog)
                .Assembly.GetExportedTypes()
                .Where(type =>
                    type is { IsClass: true, IsAbstract: false } && typeof(RenameListField).IsAssignableFrom(type)
                )
                .Select(_CreateField)
                .OrderBy(field => field.GroupOrder)
                .ThenBy(field => field.Order)
                .ThenBy(field => field.PropertyKey, StringComparer.Ordinal)
                .ToList();

            var seenKeys = new HashSet<(string GroupId, string PropertyKey)>();
            var seenSortColumns = new HashSet<RenameListSortColumn>();
            foreach (var field in fields)
            {
                Check.That(
                    seenKeys.Add((field.GroupId, field.PropertyKey)),
                    $"Duplicate Rename List field '{field.GroupId}/{field.PropertyKey}'."
                );

                if (field.IsDefaultVisiblePreview)
                {
                    Check.That(
                        field.SupportsPreview,
                        $"Preview default column '{field.GroupId}/{field.PropertyKey}' must support preview."
                    );
                }

                if (field.SortColumn is not { } sortColumn)
                {
                    continue;
                }

                Check.That(
                    seenSortColumns.Add(sortColumn),
                    $"Duplicate Rename List sort column mapping for '{sortColumn}'."
                );
            }

            return fields;
        }

        private static RenameListField _CreateField(Type fieldType)
        {
            var field = (RenameListField)
                Check.NotNull(
                    Activator.CreateInstance(fieldType),
                    $"Rename List field '{fieldType.FullName}' could not be constructed."
                );
            Check.That(
                !string.IsNullOrWhiteSpace(field.GroupId),
                $"Rename List field '{fieldType.FullName}' must declare a non-empty group id."
            );
            Check.That(
                !string.IsNullOrWhiteSpace(field.PropertyKey),
                $"Rename List field '{fieldType.FullName}' must declare a non-empty property key."
            );
            Check.That(
                !string.IsNullOrWhiteSpace(field.DisplayName),
                $"Rename List field '{fieldType.FullName}' must declare a non-empty display name."
            );
            return field;
        }
    }
}
