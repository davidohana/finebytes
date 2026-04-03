using System.Text.Json;

namespace Mfr8.Core
{
    /// <summary>
    /// Creates a preset loader that reads JSON preset files from <paramref name="presetsDirectory"/>.
    /// </summary>
    /// <param name="presetsDirectory">Directory containing <c>*.json</c> preset files.</param>
    public sealed class PresetLoader(String presetsDirectory)
    {
        public String PresetsDirectory { get; } = presetsDirectory;

        /// <summary>
        /// Gets the default presets directory for the current user.
        /// </summary>
        /// <returns>Absolute path to the default presets directory.</returns>
        public static String DefaultPresetsDirectory()
        {
            String appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "MagicFileRenamer", "presets");
        }

        /// <summary>
        /// Loads a preset by either its <c>id</c> or <c>name</c>.
        /// </summary>
        /// <param name="presetNameOrId">Preset id (GUID) or preset name.</param>
        /// <returns>The loaded preset.</returns>
        public FilterPreset Load(String presetNameOrId)
        {
            if (!Directory.Exists(PresetsDirectory))
            {
                throw new DirectoryNotFoundException($"Presets directory not found: '{PresetsDirectory}'.");
            }

            Guid? presetId = _TryParseGuid(presetNameOrId);

            String[] presetFiles = [.. Directory.EnumerateFiles(PresetsDirectory, "*.json", SearchOption.TopDirectoryOnly)];

            if (presetFiles.Length == 0)
            {
                throw new FileNotFoundException($"No preset JSON files found in '{PresetsDirectory}'.");
            }

            foreach (String? file in presetFiles)
            {
                var doc = JsonDocument.Parse(File.ReadAllText(file));
                JsonElement root = doc.RootElement;

                Guid id = _TryParseGuid(_GetString(root, "id")) ?? Guid.Empty;
                if (presetId is not null && id == presetId.Value)
                {
                    return _ParsePreset(root);
                }

                String name = _GetString(root, "name") ?? "";
                if (String.Equals(name, presetNameOrId, StringComparison.OrdinalIgnoreCase))
                {
                    return _ParsePreset(root);
                }
            }

            throw new InvalidOperationException($"Preset not found: '{presetNameOrId}'.");
        }

        private static Guid? _TryParseGuid(String? value)
        {
            return String.IsNullOrWhiteSpace(value) ? null : Guid.TryParse(value, out Guid g) ? g : null;
        }

        private static String? _GetString(JsonElement root, String name)
        {
            return root.TryGetProperty(name, out JsonElement prop) && prop.ValueKind == JsonValueKind.String ? prop.GetString() : null;
        }

        private static FilterPreset _ParsePreset(JsonElement root)
        {
            var id = Guid.Parse(_GetString(root, "id") ?? throw new InvalidOperationException("Preset missing 'id'."));
            String name = _GetString(root, "name") ?? throw new InvalidOperationException("Preset missing 'name'.");
            String? description = _GetString(root, "description");

            if (!root.TryGetProperty("filters", out JsonElement filtersEl) || filtersEl.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("Preset missing 'filters' array.");
            }

            var filters = new List<Filter>();
            foreach (JsonElement filterEl in filtersEl.EnumerateArray())
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
    }

}
