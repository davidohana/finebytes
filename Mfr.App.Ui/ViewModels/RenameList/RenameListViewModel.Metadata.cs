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
        /// Applies shuttle draft columns, sort keys, and A/B Mode, hydrating metadata off the UI thread when needed.
        /// </summary>
        /// <param name="columns">Draft visible columns in grid order.</param>
        /// <param name="sortKeys">Draft Auto-Sort keys in priority order.</param>
        /// <param name="abModeEnabled">Draft A/B Mode from the field shuttle (committed on OK).</param>
        internal async Task ApplyFieldShuttleAsync(
            IReadOnlyList<RenameListVisibleColumn> columns,
            IReadOnlyList<RenameListSortKey> sortKeys,
            bool abModeEnabled = false
        )
        {
            ArgumentNullException.ThrowIfNull(columns);
            ArgumentNullException.ThrowIfNull(sortKeys);

            if (IsBusy)
            {
                return;
            }

            // Normalize before hydrate so cancel leaves AbMode and columns untouched. Commit AbMode before
            // SetVisibleColumns so an A/B-off draft can keep preview keys (SetVisibleColumns normalizes only
            // while AbMode is already on).
            var columnsToApply = abModeEnabled ? RenameListVisibleColumn.NormalizeToOriginals(columns) : columns;

            if (!await _HydrateForColumnsAndSortAsync(columnsToApply, sortKeys).ConfigureAwait(true))
            {
                return;
            }

            IsAbModeEnabled = abModeEnabled;
            if (!columnsToApply.SequenceEqual(_visibleColumns))
            {
                SetVisibleColumns(columnsToApply);
            }

            SetSortKeys(sortKeys);
        }

        /// <summary>
        /// Applies visible columns after the same metadata hydrate path as field-shuttle apply.
        /// </summary>
        /// <param name="columns">New visible columns in grid order.</param>
        private async Task _ApplyVisibleColumnsWithHydrateAsync(IReadOnlyList<RenameListVisibleColumn> columns)
        {
            if (IsBusy)
            {
                return;
            }

            if (!await _HydrateForColumnsAndSortAsync(columns, _sortKeys).ConfigureAwait(true))
            {
                return;
            }

            if (columns.SequenceEqual(_visibleColumns))
            {
                return;
            }

            SetVisibleColumns(columns);
        }

        /// <summary>
        /// Hydrates metadata required by <paramref name="columns"/> and <paramref name="sortKeys"/>.
        /// </summary>
        private async Task<bool> _HydrateForColumnsAndSortAsync(
            IEnumerable<RenameListVisibleColumn> columns,
            IEnumerable<RenameListSortKey> sortKeys
        )
        {
            var requirement = _CombinedMetadataRequirement(columns, sortKeys);
            return await _HydrateIfNeededAsync(requirement).ConfigureAwait(true);
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
            if (!await _HydrateForColumnsAndSortAsync(_visibleColumns, keys).ConfigureAwait(true))
            {
                return;
            }

            _SetSortKeys(keys, resort);
        }
    }
}
