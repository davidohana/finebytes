using System.Text.Json;

namespace Mfr8.Core
{
    /// <summary>
    /// Creates a preset loader that reads JSON preset files from <paramref name="presetsDirectory"/>.
    /// </summary>
    /// <param name="presetsDirectory">Directory containing <c>*.json</c> preset files.</param>
    public sealed class PresetLoader(string presetsDirectory)
    {
        public string PresetsDirectory { get; } = presetsDirectory;

        /// <summary>
        /// Gets the default presets directory for the current user.
        /// </summary>
        /// <returns>Absolute path to the default presets directory.</returns>
        public static string DefaultPresetsDirectory()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "MagicFileRenamer", "presets");
        }

        /// <summary>
        /// Loads a preset by either its <c>id</c> or <c>name</c>.
        /// </summary>
        /// <param name="presetNameOrId">Preset id (GUID) or preset name.</param>
        /// <returns>The loaded preset.</returns>
        public FilterPreset Load(string presetNameOrId)
        {
            if (!Directory.Exists(PresetsDirectory))
            {
                throw new UserException($"Presets directory not found: '{PresetsDirectory}'.");
            }

            var presetId = _TryParseGuid(presetNameOrId);

            string[] presetFiles = [.. Directory.EnumerateFiles(PresetsDirectory, "*.json", SearchOption.TopDirectoryOnly)];

            if (presetFiles.Length == 0)
            {
                throw new UserException($"No preset JSON files found in '{PresetsDirectory}'.");
            }

            foreach (var file in presetFiles)
            {
                JsonDocument doc;
                try
                {
                    doc = JsonDocument.Parse(File.ReadAllText(file));
                }
                catch (Exception ex)
                {
                    throw new UserException($"Failed to read preset file '{file}': {ex.Message}", ex);
                }

                using (doc)
                {
                    var root = doc.RootElement;

                    var id = _TryParseGuid(_GetString(root, "id")) ?? Guid.Empty;
                    if (presetId is not null && id == presetId.Value)
                    {
                        return _ParsePreset(root, file, presetNameOrId);
                    }

                    var name = _GetString(root, "name") ?? "";
                    if (string.Equals(name, presetNameOrId, StringComparison.OrdinalIgnoreCase))
                    {
                        return _ParsePreset(root, file, presetNameOrId);
                    }
                }
            }

            throw new UserException($"Preset not found: '{presetNameOrId}'.");
        }

        private static Guid? _TryParseGuid(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : Guid.TryParse(value, out var g) ? g : null;
        }

        private static string? _GetString(JsonElement root, string name)
        {
            return root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String ? prop.GetString() : null;
        }

        private static FilterPreset _ParsePreset(JsonElement root, string filePath, string presetNameOrId)
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
                throw new UserException($"Preset '{presetNameOrId}' in '{filePath}' is invalid: {ex.Message}", ex);
            }
        }
    }

}
