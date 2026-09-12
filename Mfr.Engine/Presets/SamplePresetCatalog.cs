using System.Text.Json;
using Mfr.Engine.Presets.Samples;

namespace Mfr.Engine.Presets
{
    /// <summary>
    /// Provides the curated sample presets shipped with the application.
    /// </summary>
    public static class SamplePresetCatalog
    {
        /// <summary>
        /// Gets the sample presets sorted by display name.
        /// </summary>
        /// <remarks>
        /// Templates only — do not upsert these instances into <see cref="PresetManager"/>.
        /// Use <see cref="CreateIndependentCopy"/> so imported filters get their own
        /// instances (setup caches must not alias the catalog).
        /// </remarks>
        public static IReadOnlyList<FilterPreset> Presets { get; } = _CreatePresets();

        /// <summary>
        /// Deep-copies <paramref name="preset"/> (new filter instances) for the user preset store.
        /// </summary>
        /// <param name="preset">Preset to copy (typically a catalog template).</param>
        /// <returns>A deserialize-equivalent preset that does not share filter instances.</returns>
        public static FilterPreset CreateIndependentCopy(FilterPreset preset)
        {
            ArgumentNullException.ThrowIfNull(preset);

            var json = JsonSerializer.Serialize(preset, PresetJsonOptions.Default);
            return JsonSerializer.Deserialize<FilterPreset>(json, PresetJsonOptions.Default)
                ?? throw new InvalidOperationException("Failed to copy preset.");
        }

        private static IReadOnlyList<FilterPreset> _CreatePresets()
        {
            return
            [
                .. SamplePresetDefinitions
                    .CreateAll()
                    .OrderBy(preset => preset.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(preset => preset.Name, StringComparer.Ordinal),
            ];
        }
    }
}
