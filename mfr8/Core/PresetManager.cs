using System.Text.Json;

namespace Mfr8.Core
{
    /// <summary>
    /// Creates a preset manager that reads and caches presets from a single JSON file.
    /// </summary>
    /// <param name="presetsFilePath">Path to the JSON file containing all presets.</param>
    public sealed class PresetManager(string presetsFilePath)
    {
        private Dictionary<string, FilterPreset>? _loadedPresetsByName;

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
        public void LoadAll()
        {
            if (_loadedPresetsByName is not null)
            {
                return;
            }

            if (!File.Exists(PresetsFilePath))
            {
                throw new UserException($"Presets file not found: '{PresetsFilePath}'.");
            }

            PresetContainer? document;
            try
            {
                var json = File.ReadAllText(PresetsFilePath);
                // Use source-generated JSON metadata for trim/AOT-safe polymorphic deserialization.
                document = JsonSerializer.Deserialize(json, PresetJsonSerializerContext.Default.PresetContainer);
            }
            catch (Exception ex)
            {
                throw new UserException($"Failed to read presets file '{PresetsFilePath}': {ex.Message}", ex);
            }

            if (document?.Presets is null)
            {
                throw new UserException("Presets JSON must be an object containing a 'presets' array.");
            }

            var presets = document.Presets;
            if (presets.Count == 0)
            {
                throw new UserException($"No presets found in '{PresetsFilePath}'.");
            }

            var presetsByName = new Dictionary<string, FilterPreset>(StringComparer.Ordinal);
            try
            {
                foreach (var preset in presets)
                {
                    var normalizedName = _NormalizePresetName(preset.Name);
                    if (!presetsByName.TryAdd(normalizedName, preset))
                    {
                        throw new UserException($"Duplicate preset name '{preset.Name}' in '{PresetsFilePath}'. Preset names must be unique.");
                    }
                }
            }
            catch (UserException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new UserException($"One or more presets in '{PresetsFilePath}' are invalid: {ex.Message}", ex);
            }

            _loadedPresetsByName = presetsByName;
        }

        /// <summary>
        /// Gets a preset by its unique <c>name</c> from loaded presets.
        /// </summary>
        /// <param name="presetName">Preset name.</param>
        /// <returns>The matching preset.</returns>
        public FilterPreset GetByName(string presetName)
        {
            if (string.IsNullOrWhiteSpace(presetName))
            {
                throw new UserException("Preset name is required.");
            }

            if (_loadedPresetsByName is null)
            {
                throw new InvalidOperationException("Presets are not loaded. Call LoadAll() before GetByName().");
            }

            var normalizedName = _NormalizePresetName(presetName);
            return _loadedPresetsByName.TryGetValue(normalizedName, out var preset)
                ? preset
                : throw new UserException($"Preset not found: '{presetName}'.");
        }

        private static string _NormalizePresetName(string presetName)
        {
            return presetName.Trim().ToLowerInvariant();
        }
    }

    internal sealed record PresetContainer(IReadOnlyList<FilterPreset> Presets);
}
