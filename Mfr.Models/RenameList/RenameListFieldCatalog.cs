using System.Diagnostics.CodeAnalysis;
using Mfr.Models.Rename;
using Mfr.Models.RenameList.Fields.AudioTag;
using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Models.RenameList.Fields.Extended;
using Mfr.Models.RenameList.Fields.Image;
using Mfr.Models.RenameList.Fields.Jpeg;
using Mfr.Models.RenameList.Fields.Media;
using Mfr.Models.RenameList.Fields.Mpeg;
using Mfr.Utils;

namespace Mfr.Models.RenameList
{
    /// <summary>
    /// Product catalog of Rename List grid/sort fields.
    /// </summary>
    public static class RenameListFieldCatalog
    {
        /// <summary>
        /// All registered fields in catalog order (group, then field).
        /// </summary>
        public static IReadOnlyList<RenameListField> All { get; } =
            _Register([
                .. BasicRenameListFields.All,
                .. ExtendedRenameListFields.All,
                .. AudioTagRenameListFields.All,
                .. MediaRenameListFields.All,
                .. MpegRenameListFields.All,
                .. ImageRenameListFields.All,
                .. JpegRenameListFields.All,
            ]);

        private static readonly Dictionary<(string GroupId, string PropertyKey), RenameListField> _fieldByKey =
            All.ToDictionary(field => (field.GroupId, field.PropertyKey));

        /// <summary>
        /// Default visible columns (MFR7 <c>RenameGrid</c>).
        /// </summary>
        public static IReadOnlyList<RenameListFieldKey> DefaultVisibleColumns { get; } =
            _RegisterDefaultVisibleColumns([
                RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.ItemType),
                RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Folder),
                RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName),
                RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName),
            ]);

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
        /// Looks up a catalog field by group and property key.
        /// </summary>
        /// <param name="groupId">Property group id.</param>
        /// <param name="propertyKey">Property key within the group.</param>
        /// <returns>The registered field.</returns>
        /// <exception cref="ArgumentException">The field is not registered in the catalog.</exception>
        public static RenameListField GetField(string groupId, string propertyKey)
        {
            if (TryGetField(groupId, propertyKey, out var field))
            {
                return field;
            }

            throw new ArgumentException($"Unknown Rename List field '{groupId}/{propertyKey}'.", nameof(propertyKey));
        }

        /// <summary>
        /// Looks up a catalog field for <paramref name="key"/> (ignores preview flag).
        /// </summary>
        /// <param name="key">Field key.</param>
        /// <returns>The registered field.</returns>
        /// <exception cref="ArgumentException">The field is not registered in the catalog.</exception>
        public static RenameListField GetField(RenameListFieldKey key)
        {
            if (TryGetField(key, out var field))
            {
                return field;
            }

            throw new ArgumentException($"Unknown Rename List field '{key.GroupId}/{key.PropertyKey}'.", nameof(key));
        }

        /// <summary>
        /// Returns whether an original field key may participate in Auto-Sort.
        /// </summary>
        /// <param name="key">Field key.</param>
        /// <returns><see langword="true"/> when the key is a sortable original field.</returns>
        public static bool IsSortableKey(RenameListFieldKey key)
        {
            if (key.IsPreview)
            {
                return false;
            }

            return TryGetField(key, out var field) && field.IsSortable;
        }

        /// <summary>
        /// Compares two rename items for Auto-Sort on one original field key.
        /// </summary>
        /// <param name="left">Left item.</param>
        /// <param name="key">Original field key.</param>
        /// <param name="right">Right item.</param>
        /// <returns>Comparison sign for sort.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not a sortable original field.</exception>
        public static int CompareForSort(RenameItem left, RenameListFieldKey key, RenameItem right)
        {
            ArgumentNullException.ThrowIfNull(left);
            ArgumentNullException.ThrowIfNull(right);

            if (!IsSortableKey(key))
            {
                throw new ArgumentException($"Field '{key.GroupId}/{key.PropertyKey}' is not sortable.", nameof(key));
            }

            var leftIsError = RenameListMetadataLoadErrors.HasLoadError(left, key);
            var rightIsError = RenameListMetadataLoadErrors.HasLoadError(right, key);
            if (leftIsError || rightIsError)
            {
                return RenameListFieldSortCompare.ErrorsLast(leftIsError, rightIsError);
            }

            return GetField(key).CompareForSort(left.Original, right.Original);
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
            return GetField(key).Resolve(item, key.IsPreview);
        }

        /// <summary>
        /// Grid text when original metadata load fails (MFR7 <c>PropDisplay</c> with exception).
        /// </summary>
        public const string FieldLoadErrorText = "Error";

        /// <summary>
        /// Returns whether an original field failed to load metadata from disk.
        /// </summary>
        /// <param name="item">Engine rename item.</param>
        /// <param name="key">Field key (original or preview).</param>
        /// <returns><see langword="true"/> when the cell should show <c>Error</c>.</returns>
        public static bool HasFieldLoadError(RenameItem item, RenameListFieldKey key)
        {
            return RenameListMetadataLoadErrors.HasLoadError(item, key);
        }

        /// <summary>
        /// Returns whether the row has any original metadata load failure.
        /// </summary>
        /// <param name="item">Engine rename item.</param>
        /// <returns><see langword="true"/> when TagLib or image metadata failed to load.</returns>
        public static bool HasAnyFieldLoadError(RenameItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            return RenameListMetadataLoadErrors.HasAny(item);
        }

        /// <summary>
        /// Lists distinct original metadata-reader failures on the row.
        /// </summary>
        /// <param name="item">Engine rename item.</param>
        /// <returns>At most one TagLib and one image failure, in that order.</returns>
        public static IReadOnlyList<RenameListLoadError> ListFieldLoadErrors(RenameItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            return RenameListMetadataLoadErrors.List(item);
        }

        /// <summary>
        /// Returns a plain-language explanation for a field-load failure on one original column.
        /// </summary>
        /// <param name="item">Engine rename item.</param>
        /// <param name="key">Original field key with a load error.</param>
        /// <returns>User-facing explanation, or an empty string when no error is stored.</returns>
        public static string DescribeFieldLoadError(RenameItem item, RenameListFieldKey key)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (!TryGetField(key, out var field))
            {
                return string.Empty;
            }

            if (!RenameListMetadataLoadErrors.TryGetLoadError(item, key, out var error) || error is null)
            {
                return string.Empty;
            }

            return RenameListMetadataLoadErrors.DescribeUserMessage(error, field.MetadataRequirement);
        }

        /// <summary>
        /// Returns the disk metadata requirement for one field key.
        /// </summary>
        /// <param name="key">Field key.</param>
        /// <returns>Metadata requirement for the registered field, or <see cref="RenameListMetadataRequirement.None"/>.</returns>
        public static RenameListMetadataRequirement GetMetadataRequirement(RenameListFieldKey key)
        {
            return TryGetField(key, out var field) ? field.MetadataRequirement : RenameListMetadataRequirement.None;
        }

        /// <summary>
        /// Combines disk metadata requirements for a set of field keys.
        /// </summary>
        /// <param name="keys">Field keys.</param>
        /// <returns>Combined requirement flags.</returns>
        public static RenameListMetadataRequirement GetCombinedMetadataRequirement(IEnumerable<RenameListFieldKey> keys)
        {
            ArgumentNullException.ThrowIfNull(keys);

            var combined = RenameListMetadataRequirement.None;
            foreach (var key in keys)
            {
                combined |= GetMetadataRequirement(key);
            }

            return combined;
        }

        private static List<RenameListField> _Register(List<RenameListField> fields)
        {
            var seenKeys = new HashSet<(string GroupId, string PropertyKey)>();
            foreach (var field in fields)
            {
                Check.That(
                    !string.IsNullOrWhiteSpace(field.GroupId),
                    $"Rename List field '{field.GetType().FullName}' must declare a non-empty group id."
                );
                Check.That(
                    !string.IsNullOrWhiteSpace(field.PropertyKey),
                    $"Rename List field '{field.GetType().FullName}' must declare a non-empty property key."
                );
                Check.That(
                    !string.IsNullOrWhiteSpace(field.DisplayName),
                    $"Rename List field '{field.GetType().FullName}' must declare a non-empty display name."
                );
                Check.That(
                    seenKeys.Add((field.GroupId, field.PropertyKey)),
                    $"Duplicate Rename List field '{field.GroupId}/{field.PropertyKey}'."
                );
            }

            return fields;
        }

        private static List<RenameListFieldKey> _RegisterDefaultVisibleColumns(List<RenameListFieldKey> keys)
        {
            foreach (var key in keys)
            {
                Check.That(
                    TryGetField(key, out var field),
                    $"Default visible column '{key.GroupId}/{key.PropertyKey}' is not a registered field."
                );
                if (!key.IsPreview)
                {
                    continue;
                }

                Check.That(
                    field is { SupportsPreview: true },
                    $"Preview default column '{key.GroupId}/{key.PropertyKey}' must support preview."
                );
            }

            return keys;
        }
    }
}
