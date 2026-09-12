using System.Text.Json.Serialization;
using Mfr.Models.Rename;
using Mfr.Models.RenameList;

namespace Mfr.Models.Filters
{
    /// <summary>
    /// One step in a <see cref="FilterChain"/>: whether it runs and the filter configuration.
    /// </summary>
    /// <param name="Enabled">Whether this step participates when applying the chain.</param>
    /// <param name="Filter">The filter configuration for this step.</param>
    /// <param name="Name">
    /// Optional Applied Filters list label. When null or blank on load, the UI synthesizes a catalog
    /// display name (with <c>(2)</c>, <c>(3)</c>, … for duplicate types).
    /// </param>
    public sealed record FilterChainStep(
        [property: JsonPropertyName("enabled")] bool Enabled,
        [property: JsonPropertyName("filter")] BaseFilter Filter,
        [property: JsonPropertyName("name")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            string? Name = null
    );

    /// <summary>
    /// Ordered filter stack for a preset or session: each step has an enabled flag and a <see cref="BaseFilter"/>.
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
        internal static FilterChain CreateAllEnabled(IReadOnlyList<BaseFilter> filters)
        {
            return new FilterChain { Steps = [.. filters.Select(f => new FilterChainStep(Enabled: true, Filter: f))] };
        }

        /// <summary>
        /// Runs setup for every enabled filter in the chain before applying any transformations.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Disabled steps are skipped so a bad Casing List / Formatter template on an
        /// unchecked row does not fail (or reload) the whole preview.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when an enabled filter's setup fails. The message names the filter type; the original
        /// exception is the <see cref="Exception.InnerException"/>.
        /// </exception>
        public void SetupFilters()
        {
            foreach (var step in Steps)
            {
                if (!step.Enabled)
                {
                    continue;
                }

                try
                {
                    step.Filter.Setup();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Failed to initialize filter '{step.Filter.Type}': {ex.Message}",
                        ex
                    );
                }
            }
        }

        /// <summary>
        /// Clears preview and applies enabled steps in order to update the item's preview file name.
        /// </summary>
        /// <param name="item">The rename item receiving transformed preview metadata.</param>
        /// <remarks>
        /// <para>
        /// Call <see cref="SetupFilters"/> once before the first apply; this method does not run setup.
        /// After clearing preview, applies original-side manual overrides (MFR7 <c>PreviewStart</c>)
        /// so filters seed from overridden originals.
        /// </para>
        /// </remarks>
        public void ApplyFilters(RenameItem item)
        {
            item.ClearPreview();
            if (!RenameListFieldOverrides.TryApplyToPreview(item, isPreview: false))
            {
                return;
            }

            foreach (var step in Steps)
            {
                if (!step.Enabled)
                {
                    continue;
                }

                step.Filter.Apply(item);
            }
        }
    }
}
