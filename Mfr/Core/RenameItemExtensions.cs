using Mfr.Models;
using Mfr.Models.Filters;

namespace Mfr.Core
{
    /// <summary>
    /// Provides rename-item helpers for filter-based preview name transformation.
    /// </summary>
    public static class RenameItemExtensions
    {
        /// <summary>
        /// Applies enabled filters to update the item's preview file name.
        /// </summary>
        /// <param name="item">The rename item receiving transformed preview metadata.</param>
        /// <param name="filters">The configured filters to apply in order.</param>
        public static void ApplyFilters(this RenameItem item, IReadOnlyList<Filter> filters)
        {
            item.ResetPreview();

            foreach (var filter in filters)
            {
                filter.Apply(item);
            }

            // If no preview was generated, copy from the original.
            if (item.Preview is null)
            {
                item.CopyPreviewFromOriginal();
            }
        }
    }
}
