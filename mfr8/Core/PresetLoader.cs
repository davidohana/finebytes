using System.Text.Json;

namespace Mfr8.Core;

public sealed class PresetLoader
{
    public string PresetsDirectory { get; }

    /// <summary>
    /// Creates a preset loader that reads JSON preset files from <paramref name="presetsDirectory"/>.
    /// </summary>
    /// <param name="presetsDirectory">Directory containing <c>*.json</c> preset files.</param>
    public PresetLoader(string presetsDirectory)
    {
        PresetsDirectory = presetsDirectory;
    }

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
            throw new DirectoryNotFoundException($"Presets directory not found: '{PresetsDirectory}'.");

        Guid? presetId = _TryParseGuid(presetNameOrId);

        var presetFiles = Directory.EnumerateFiles(PresetsDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .ToArray();

        if (presetFiles.Length == 0)
            throw new FileNotFoundException($"No preset JSON files found in '{PresetsDirectory}'.");

        foreach (var file in presetFiles)
        {
            var doc = JsonDocument.Parse(File.ReadAllText(file));
            var root = doc.RootElement;

            var id = _TryParseGuid(_GetString(root, "id")) ?? Guid.Empty;
            if (presetId is not null && id == presetId.Value)
                return _ParsePreset(root);

            var name = _GetString(root, "name") ?? "";
            if (string.Equals(name, presetNameOrId, StringComparison.OrdinalIgnoreCase))
                return _ParsePreset(root);
        }

        throw new InvalidOperationException($"Preset not found: '{presetNameOrId}'.");
    }

    private static Guid? _TryParseGuid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (Guid.TryParse(value, out var g)) return g;
        return null;
    }

    private static string? _GetString(JsonElement root, string name)
        => root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String ? prop.GetString() : null;

    private static FilterPreset _ParsePreset(JsonElement root)
    {
        var id = Guid.Parse(_GetString(root, "id") ?? throw new InvalidOperationException("Preset missing 'id'."));
        var name = _GetString(root, "name") ?? throw new InvalidOperationException("Preset missing 'name'.");
        string? description = _GetString(root, "description");

        if (!root.TryGetProperty("filters", out var filtersEl) || filtersEl.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Preset missing 'filters' array.");

        var filters = new List<Filter>();
        foreach (var filterEl in filtersEl.EnumerateArray())
            filters.Add(FilterParser.ParseFilter(filterEl));

        return new FilterPreset
        {
            Id = id,
            Name = name,
            Description = description,
            Filters = filters
        };
    }
}

