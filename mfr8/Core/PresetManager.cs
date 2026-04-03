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

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(File.ReadAllText(PresetsFilePath));
            }
            catch (Exception ex)
            {
                throw new UserException($"Failed to read presets file '{PresetsFilePath}': {ex.Message}", ex);
            }

            using (doc)
            {
                var presetElements = _GetPresetElements(doc.RootElement);
                if (presetElements.Count == 0)
                {
                    throw new UserException($"No presets found in '{PresetsFilePath}'.");
                }

                var presets = presetElements
                    .Select(presetElement => _ParsePreset(presetElement, PresetsFilePath, _GetString(presetElement, "name") ?? ""))
                    .ToList();

                var presetsByName = new Dictionary<string, FilterPreset>(StringComparer.Ordinal);
                foreach (var preset in presets)
                {
                    var normalizedName = _NormalizePresetName(preset.Name);
                    if (!presetsByName.TryAdd(normalizedName, preset))
                    {
                        throw new UserException($"Duplicate preset name '{preset.Name}' in '{PresetsFilePath}'. Preset names must be unique.");
                    }
                }

                _loadedPresetsByName = presetsByName;
            }
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

        private static IReadOnlyList<JsonElement> _GetPresetElements(JsonElement root)
        {
            return root.ValueKind == JsonValueKind.Object && root.TryGetProperty("presets", out var presetsEl)
                ? presetsEl.ValueKind == JsonValueKind.Array
                    ? [.. presetsEl.EnumerateArray()]
                    : throw new UserException("The 'presets' property must be a JSON array.")
                : throw new UserException("Presets JSON must be an object containing a 'presets' array.");
        }

        private static string? _GetString(JsonElement root, string name)
        {
            return root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String ? prop.GetString() : null;
        }

        private static FilterPreset _ParsePreset(JsonElement root, string filePath, string presetName)
        {
            try
            {
                var id = Guid.Parse(_GetString(root, "id") ?? throw new UserException("Preset missing 'id'."));
                var name = _GetString(root, "name") ?? throw new UserException("Preset missing 'name'.");
                var description = _GetString(root, "description");

                if (!root.TryGetProperty("filters", out var filtersEl) || filtersEl.ValueKind != JsonValueKind.Array)
                {
                    throw new UserException("Preset missing 'filters' array.");
                }

                var filters = new List<Filter>();
                foreach (var filterEl in filtersEl.EnumerateArray())
                {
                    filters.Add(FilterParser.ParseFilter(filterEl));
                }

                return new FilterPreset
                {
                    Id = id,
                    Name = name,
                    Description = description,
                    Filters = filters
                };
            }
            catch (UserException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new UserException($"Preset '{presetName}' in '{filePath}' is invalid: {ex.Message}", ex);
            }
        }
    }
}
