using System.Text.Json;

namespace Mfr.Engine.Presets
{
    /// <summary>
    /// Provides the curated sample presets shipped with the application.
    /// </summary>
    public static class SamplePresetCatalog
    {
        private const string ResourceName = "Mfr.Engine.Presets.Samples.sample-presets.json";

        /// <summary>
        /// Gets the sample presets sorted by display name.
        /// </summary>
        public static IReadOnlyList<FilterPreset> Presets { get; } = _LoadPresets();

        private static IReadOnlyList<FilterPreset> _LoadPresets()
        {
            var assembly = typeof(SamplePresetCatalog).Assembly;
            using var stream =
                assembly.GetManifestResourceStream(ResourceName)
                ?? throw new InvalidOperationException(
                    $"Embedded sample preset resource '{ResourceName}' was not found in {assembly.GetName().Name}."
                );

            var container =
                JsonSerializer.Deserialize<PresetContainer>(stream, PresetJsonOptions.Default)
                ?? throw new InvalidDataException("Embedded sample preset JSON is null or invalid.");

            return
            [
                .. container
                    .Presets.OrderBy(preset => preset.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(preset => preset.Name, StringComparer.Ordinal),
            ];
        }
    }
}
