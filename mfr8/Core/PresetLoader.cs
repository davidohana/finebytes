using System.Text.Json;

namespace Mfr8.Core
{
    /// <summary>
    /// Creates a preset loader that reads all presets from a single JSON file.
    /// </summary>
    /// <param name="presetsFilePath">Path to the JSON file containing all presets.</param>
    public sealed class PresetLoader(string presetsFilePath)
    {
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
        /// Loads a preset by its unique <c>name</c>.
        /// </summary>
        /// <param name="presetName">Preset name.</param>
        /// <returns>The loaded preset.</returns>
        public FilterPreset Load(string presetName)
        {
            if (string.IsNullOrWhiteSpace(presetName))
            {
                throw new UserException("Preset name is required.");
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

                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var presetElement in presetElements)
                {
                    var parsedName = _GetString(presetElement, "name") ?? "";
                    if (!names.Add(parsedName))
                    {
                        throw new UserException($"Duplicate preset name '{parsedName}' in '{PresetsFilePath}'. Preset names must be unique.");
                    }
                }

                foreach (var presetElement in presetElements)
                {
                    var name = _GetString(presetElement, "name") ?? "";
                    if (string.Equals(name, presetName, StringComparison.OrdinalIgnoreCase))
                    {
                        return _ParsePreset(presetElement, PresetsFilePath, presetName);
                    }
                }
            }

            throw new UserException($"Preset not found: '{presetName}'.");
        }

        private static IReadOnlyList<JsonElement> _GetPresetElements(JsonElement root)
        {
            if (root.ValueKind == JsonValueKind.Array)
            {
                return [.. root.EnumerateArray()];
            }

            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("presets", out var presetsEl))
            {
                return presetsEl.ValueKind == JsonValueKind.Array
                    ? [.. presetsEl.EnumerateArray()]
                    : throw new UserException("The 'presets' property must be a JSON array.");
            }

            throw new UserException("Presets JSON must be an array or an object with a 'presets' array.");
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
