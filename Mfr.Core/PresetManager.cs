using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Mfr.Models;
using Mfr.Utils;

namespace Mfr.Core
{
    /// <summary>
    /// Creates a preset manager that reads and caches presets from a single JSON file.
    /// </summary>
    /// <param name="presetsFilePath">Path to the JSON file containing all presets.</param>
    public sealed class PresetManager(string presetsFilePath)
    {
        /// <summary>
        /// Gets loaded presets keyed by preset name.
        /// </summary>
        public Dictionary<string, FilterPreset> NameToPreset { get; } = [];

        /// <summary>
        /// Gets the JSON file path containing all presets.
        /// </summary>
        public string PresetsFilePath { get; } = presetsFilePath;

        /// <summary>
        /// Gets the default presets file path for the current user.
        /// </summary>
        /// <returns>Absolute path to the default presets JSON file.</returns>
        public static string DefaultPresetsFilePath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return appData.CombinePath("MagicFileRenamer", "presets.json");
        }

        /// <summary>
        /// Loads and caches all presets from the configured presets file.
        /// </summary>
        public void LoadPresets()
        {
            if (!File.Exists(PresetsFilePath))
            {
                throw new UserException($"Presets file not found: '{PresetsFilePath}'.");
            }

            PresetContainer container;
            try
            {
                var json = File.ReadAllText(PresetsFilePath, Encoding.UTF8);
                container = JsonSerializer.Deserialize<PresetContainer>(json, PresetJsonOptions.Default)
                    ?? throw new InvalidDataException("Presets JSON payload is null or invalid for the expected schema.");
            }
            catch (Exception ex)
            {
                throw new UserException($"Failed to read presets file '{PresetsFilePath}': {ex.Message}", ex);
            }

            var presets = container.Presets;

            NameToPreset.Clear();
            foreach (var preset in presets)
            {
                if (!NameToPreset.TryAdd(preset.Name, preset))
                {
                    throw new UserException($"Duplicate preset names found in '{PresetsFilePath}'. Preset names must be unique.");
                }
            }
        }

        /// <summary>
        /// Saves currently loaded presets to the configured presets file.
        /// </summary>
        public void SavePresets()
        {
            try
            {
                var directory = Path.GetDirectoryName(PresetsFilePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var sortedPresets = NameToPreset.Values
                    .OrderBy(preset => preset.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(preset => preset.Name, StringComparer.Ordinal)
                    .ToList();
                var container = new PresetContainer(sortedPresets);
                var json = JsonSerializer.Serialize(container, PresetJsonOptions.Default);
                File.WriteAllText(PresetsFilePath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new UserException($"Failed to save presets file '{PresetsFilePath}': {ex.Message}", ex);
            }
        }

    }

    internal sealed record PresetContainer(
        [property: JsonPropertyName("presets")] IReadOnlyList<FilterPreset> Presets);
}
