namespace Mfr.Filters
{
    /// <summary>
    /// Extension methods for filter collections.
    /// </summary>
    public static class FilterExtensions
    {
        /// <summary>
        /// Runs setup for all filters before applying any transformations.
        /// </summary>
        /// <param name="filters">The configured filters.</param>
        public static void SetupFilters(this IReadOnlyList<BaseFilter> filters)
        {
            foreach (var filter in filters)
            {
                filter.Setup();
            }
        }
    }
}
