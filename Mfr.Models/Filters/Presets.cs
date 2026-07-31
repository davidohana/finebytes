using System.Text.Json.Serialization;

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
        [JsonPropertyName("id")]
        public required Guid Id { get; init; }

        /// <summary>
        /// Gets the display name for this preset.
        /// </summary>
        [JsonPropertyName("name")]
        public required string Name { get; init; }

        /// <summary>
        /// Gets an optional preset description.
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; init; }

        /// <summary>
        /// Gets the ordered filter chain for this preset.
        /// </summary>
        [JsonPropertyName("chain")]
        public required FilterChain Chain { get; init; }
    }
}
