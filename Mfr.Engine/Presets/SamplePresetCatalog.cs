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
        public static IReadOnlyList<FilterPreset> Presets { get; } = _CreatePresets();

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
