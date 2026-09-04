using Mfr.Filters;
using Mfr.Models.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Eager metadata hydration for Rename List visible columns and Auto-Sort keys.
    /// </summary>
    public sealed partial class RenameListViewModel
    {
        /// <summary>
        /// Applies shuttle draft columns and sort keys, hydrating metadata off the UI thread when needed.
        /// </summary>
        /// <param name="columns">Draft visible columns in grid order.</param>
        /// <param name="sortKeys">Draft Auto-Sort keys in priority order.</param>
        internal async Task ApplyFieldShuttleAsync(
            IReadOnlyList<RenameListVisibleColumn> columns,
            IReadOnlyList<RenameListSortKey> sortKeys
        )
        {
            ArgumentNullException.ThrowIfNull(columns);
            ArgumentNullException.ThrowIfNull(sortKeys);

            if (IsBusy)
            {
                return;
            }

            var requirement = _CombinedMetadataRequirement(columns, sortKeys);
            if (!await _HydrateIfNeededAsync(requirement).ConfigureAwait(true))
            {
                return;
            }

            SetVisibleColumns(columns);
            SetSortKeys(sortKeys);
        }

        /// <summary>
        /// Combined metadata requirement for visible columns and sort keys.
        /// </summary>
        private RenameListMetadataRequirement _CurrentMetadataRequirement()
        {
            return _CombinedMetadataRequirement(_visibleColumns, _sortKeys);
        }

        private static RenameListMetadataRequirement _CombinedMetadataRequirement(
            IEnumerable<RenameListVisibleColumn> columns,
            IEnumerable<RenameListSortKey> sortKeys
        )
        {
            var keys = columns.Select(column => column.Key).Concat(sortKeys.Select(key => key.FieldKey));
            return RenameListFieldCatalog.GetCombinedMetadataRequirement(keys);
        }

        private bool _NeedsHydrate(RenameListMetadataRequirement requirement)
        {
            if (requirement == RenameListMetadataRequirement.None || _renameList.RenameItems.Count == 0)
            {
                return false;
            }

            return RenameListMetadataLoader.AnyItemNeedsLoad(_renameList.RenameItems, requirement);
        }

        private async Task<bool> _HydrateIfNeededAsync(RenameListMetadataRequirement requirement)
        {
            if (!_NeedsHydrate(requirement))
            {
                return true;
            }

            var completed = await _RunProgressAsync(
                    RenameListProgressOperation.MetadataHydrate,
                    (token, progress) => _renameList.EnsureMetadataLoaded(requirement, token, progress)
                )
                .ConfigureAwait(true);
            if (completed)
            {
                _RefreshFieldDisplay();
            }

            return completed;
        }

        private async Task _HydrateThenSetSortKeysAsync(IReadOnlyList<RenameListSortKey> keys, bool resort)
        {
            var requirement = _CombinedMetadataRequirement(_visibleColumns, keys);
            if (!await _HydrateIfNeededAsync(requirement).ConfigureAwait(true))
            {
                return;
            }

            _SetSortKeys(keys, resort);
        }
    }
}
