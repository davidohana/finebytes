using System.Text.Json;

namespace Mfr8.Core
{
    /// <summary>
    /// Creates a preset manager that reads and caches presets from a single JSON file.
    /// </summary>
    /// <param name="presetsFilePath">Path to the JSON file containing all presets.</param>
    public sealed class PresetManager(string presetsFilePath)
    {
        private Dictionary<string, FilterPreset> _NameToPreset = [];

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
            return Path.Combine(appData, "MagicFileRenamer", "presets.json");
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
                var json = File.ReadAllText(PresetsFilePath);
                // Use source-generated JSON metadata for trim/AOT-safe polymorphic deserialization.
                container = JsonSerializer.Deserialize(json, PresetJsonSerializerContext.Default.PresetContainer)
                    ?? throw new InvalidDataException("Presets JSON payload is null or invalid for the expected schema.");
            }
            catch (Exception ex)
            {
                throw new UserException($"Failed to read presets file '{PresetsFilePath}': {ex.Message}", ex);
            }

            var presets = container.Presets;

            try
            {
                _NameToPreset = presets.ToDictionary(preset => preset.Name, StringComparer.Ordinal);
            }
            catch (ArgumentException ex)
            {
                throw new UserException($"Duplicate preset names found in '{PresetsFilePath}'. Preset names must be unique.", ex);
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
                    _ = Directory.CreateDirectory(directory);
                }

                var container = new PresetContainer([.. _NameToPreset.Values]);
                var json = JsonSerializer.Serialize(container, PresetJsonSerializerContext.Default.PresetContainer);
                File.WriteAllText(PresetsFilePath, json);
            }
            catch (Exception ex)
            {
                throw new UserException($"Failed to save presets file '{PresetsFilePath}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets a preset by its unique <c>name</c> from loaded presets.
        /// </summary>
        /// <param name="presetName">Preset name.</param>
        /// <returns>The matching preset.</returns>
        public FilterPreset GetByName(string presetName)
        {
            return string.IsNullOrWhiteSpace(presetName)
                ? throw new UserException("Preset name is required.")
                : _NameToPreset.TryGetValue(presetName, out var preset)
                ? preset
                : throw new UserException($"Preset not found: '{presetName}'.");
        }
    }

    internal sealed record PresetContainer(IReadOnlyList<FilterPreset> Presets);
}
