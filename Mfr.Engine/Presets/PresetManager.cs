using System.Text.Json;
using System.Text.Json.Serialization;
using Mfr.Utils;

namespace Mfr.Engine.Presets
{
    /// <summary>
    /// Creates a preset manager that reads and caches presets from a single JSON file.
    /// </summary>
    /// <remarks>
    /// Hard-fail dialect: invalid <c>presets.json</c> → <see cref="LoadPresets"/> throws
    /// (<see cref="UserException"/> / wrapped IO) and aborts load. Missing AppData file is created
    /// empty by <see cref="OpenDefault"/> then loaded. Same family as
    /// <see cref="Models.Config.ConfigStore"/>; opposite of soft-load
    /// <see cref="Models.Config.SessionStore"/> and <see cref="FilterDefaultsStore"/>.
    /// Do not unify these modes — the split is intentional product dialect.
    /// </remarks>
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
            return AppDataPaths.RoamingRoot().CombinePath("presets.json");
        }

        /// <summary>
        /// Opens the AppData presets file, creating an empty container when the file is missing.
        /// </summary>
        /// <returns>A manager with presets loaded from disk.</returns>
        /// <exception cref="UserException">
        /// Thrown when the file exists but cannot be read or is not a valid presets document.
        /// </exception>
        public static PresetManager OpenDefault()
        {
            return OpenOrCreate(DefaultPresetsFilePath());
        }

        /// <summary>
        /// Creates an empty manager that does not read AppData (tests and isolated UI hosts).
        /// </summary>
        /// <returns>A manager with no presets loaded.</returns>
        public static PresetManager CreateEmpty()
        {
            return new PresetManager(Path.Combine(Path.GetTempPath(), $"mfr-empty-presets-{Guid.NewGuid():N}.json"));
        }

        /// <summary>
        /// Opens <paramref name="presetsFilePath"/>, writing an empty container when the file is missing.
        /// </summary>
        /// <param name="presetsFilePath">Path to the presets JSON file.</param>
        /// <returns>A manager with presets loaded from disk.</returns>
        /// <exception cref="UserException">
        /// Thrown when the file exists but cannot be read or is not a valid presets document.
        /// </exception>
        internal static PresetManager OpenOrCreate(string presetsFilePath)
        {
            var manager = new PresetManager(presetsFilePath);
            if (!File.Exists(manager.PresetsFilePath))
            {
                manager.SavePresets();
            }

            manager.LoadPresets();
            return manager;
        }

        /// <summary>
        /// Loads and caches all presets from the configured presets file.
        /// </summary>
        /// <exception cref="UserException">
        /// Thrown when the file is missing, unreadable, or not a valid presets document.
        /// </exception>
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
                container =
                    JsonSerializer.Deserialize<PresetContainer>(json, PresetJsonOptions.Default)
                    ?? throw new InvalidDataException(
                        "Presets JSON payload is null or invalid for the expected schema."
                    );
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
                    throw new UserException(
                        $"Duplicate preset names found in '{PresetsFilePath}'. Preset names must be unique."
                    );
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

                var sortedPresets = NameToPreset
                    .Values.OrderBy(preset => preset.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(preset => preset.Name, StringComparer.Ordinal)
                    .ToList();
                var container = new PresetContainer(sortedPresets);
                var json = JsonSerializer.Serialize(container, PresetJsonOptions.Default);
                File.WriteAllText(PresetsFilePath, json);
            }
            catch (Exception ex)
            {
                throw new UserException($"Failed to save presets file '{PresetsFilePath}': {ex.Message}", ex);
            }
        }
    }

    internal sealed record PresetContainer([property: JsonPropertyName("presets")] IReadOnlyList<FilterPreset> Presets);
}
