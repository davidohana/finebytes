using System.Text.Json.Serialization;

namespace Mfr.Models
{
    /// <summary>
    /// One step in a <see cref="FilterChain"/>: whether it runs and the filter configuration.
    /// </summary>
    /// <param name="Enabled">Whether this step participates when applying the chain.</param>
    /// <param name="Filter">The filter configuration for this step.</param>
    public sealed record FilterChainStep(
        bool Enabled,
        [property: JsonPropertyName("filter")] BaseFilter Filter);

    /// <summary>
    /// Ordered filter stack for a preset: each step has an enabled flag and a <see cref="BaseFilter"/>.
    /// </summary>
    public sealed record FilterChain
    {
        /// <summary>
        /// Gets the ordered steps.
        /// </summary>
        [JsonPropertyName("steps")]
        public required IReadOnlyList<FilterChainStep> Steps { get; init; }

        /// <summary>
        /// Creates a chain where every step is enabled, preserving order.
        /// </summary>
        /// <param name="filters">Filters to wrap as enabled steps.</param>
        /// <returns>A new chain.</returns>
        public static FilterChain CreateAllEnabled(IReadOnlyList<BaseFilter> filters)
        {
            var steps = new FilterChainStep[filters.Count];
            for (var i = 0; i < filters.Count; i++)
            {
                steps[i] = new FilterChainStep(Enabled: true, Filter: filters[i]);
            }

            return new FilterChain { Steps = steps };
        }

        /// <summary>
        /// Runs setup for every filter in the chain before applying any transformations.
        /// </summary>
        public void SetupFilters()
        {
            foreach (var step in Steps)
            {
                step.Filter.Setup();
            }
        }
    }
}
