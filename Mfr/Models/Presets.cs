using Mfr.Filters;

namespace Mfr.Models
{
    /// <summary>
    /// Defines a named filter preset.
    /// </summary>
    public sealed record FilterPreset
    {
        /// <summary>
        /// Gets the unique preset identifier.
        /// </summary>
        public required Guid Id { get; init; }

        /// <summary>
        /// Gets the display name for this preset.
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Gets an optional preset description.
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// Gets the ordered list of filters for this preset.
        /// </summary>
        public required IReadOnlyList<Filter> Filters { get; init; }
    }
}
