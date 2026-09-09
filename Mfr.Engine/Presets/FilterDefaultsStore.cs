using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mfr.Utils;
using Serilog;

namespace Mfr.Engine.Presets
{
    /// <summary>
    /// Loads and saves per-filter-type add defaults (Filter Configuration “save as default”).
    /// <para>
    /// One snapshot per <see cref="BaseFilter.Type"/> discriminator in <c>filter-defaults.json</c>.
    /// Used when adding from the palette; not used by reset, presets, or session restore.
    /// </para>
    /// </summary>
    /// <param name="defaultsFilePath">Path to the JSON file of type defaults.</param>
    public sealed class FilterDefaultsStore(string defaultsFilePath)
    {
        private readonly Dictionary<string, BaseFilter> _typeToDefault = new(StringComparer.Ordinal);

        /// <summary>
        /// Gets the JSON file path for type defaults.
        /// </summary>
        public string DefaultsFilePath { get; } =
            string.IsNullOrWhiteSpace(defaultsFilePath)
                ? throw new ArgumentException("Defaults file path must not be blank.", nameof(defaultsFilePath))
                : defaultsFilePath;

        /// <summary>
        /// Gets the default AppData path for filter type defaults.
        /// </summary>
        /// <returns>Absolute path to <c>filter-defaults.json</c>.</returns>
        public static string DefaultFilePath()
        {
            return AppDataPaths.RoamingRoot().CombinePath("filter-defaults.json");
        }

        /// <summary>
        /// Opens the AppData store, loading when the file exists (missing or unreadable → empty).
        /// </summary>
        /// <returns>A store ready for get/set.</returns>
        public static FilterDefaultsStore OpenDefault()
        {
            var store = new FilterDefaultsStore(DefaultFilePath());
            store.TryLoad();
            return store;
        }

        /// <summary>
        /// Creates an empty store that does not read AppData (tests and isolated UI hosts).
        /// </summary>
        /// <returns>A store with no type defaults loaded.</returns>
        public static FilterDefaultsStore CreateEmpty()
        {
            return new FilterDefaultsStore(
                Path.Combine(Path.GetTempPath(), $"mfr-empty-filter-defaults-{Guid.NewGuid():N}.json")
            );
        }

        /// <summary>
        /// Loads defaults from disk when the file exists and is readable; otherwise leaves the cache empty.
        /// <para>
        /// Missing, corrupt, or wrong-shaped files leave the cache empty (same soft load as session).
        /// Unknown or invalid entries are skipped (factory defaults remain for those types).
        /// </para>
        /// </summary>
        public void TryLoad()
        {
            _typeToDefault.Clear();
            if (!File.Exists(DefaultsFilePath))
            {
                return;
            }

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(File.ReadAllText(DefaultsFilePath));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                // Preference file — do not fail app startup or hosts; add uses factory.
                Log.Warning(ex, "Failed to read filter defaults file '{DefaultsFilePath}'.", DefaultsFilePath);
                return;
            }

            using (doc)
            {
                if (
                    !doc.RootElement.TryGetProperty("defaults", out var defaultsElement)
                    || defaultsElement.ValueKind != JsonValueKind.Object
                )
                {
                    return;
                }

                foreach (var property in defaultsElement.EnumerateObject())
                {
                    try
                    {
                        var filter = JsonSerializer.Deserialize<BaseFilter>(
                            property.Value.GetRawText(),
                            PresetJsonOptions.Default
                        );
                        if (filter is null)
                        {
                            continue;
                        }

                        _typeToDefault[filter.Type] = filter;
                    }
                    catch (Exception ex) when (ex is JsonException or NotSupportedException)
                    {
                        // Unknown type discriminator or bad payload — skip; add uses factory.
                    }
                }
            }
        }

        /// <summary>
        /// Clears the in-memory type defaults without touching disk.
        /// </summary>
        public void Clear()
        {
            _typeToDefault.Clear();
        }

        /// <summary>
        /// Deletes the defaults JSON file when it exists and clears the in-memory cache (Reset Configuration).
        /// <para>Missing files are a no-op aside from clearing the cache.</para>
        /// </summary>
        /// <exception cref="IOException">Thrown when the file exists but cannot be deleted.</exception>
        public void DeleteFile()
        {
            Clear();
            DeleteFileAt(DefaultsFilePath);
        }

        /// <summary>
        /// Deletes the filter-defaults JSON file when it exists (Reset Configuration).
        /// <para>Missing files are a no-op.</para>
        /// </summary>
        /// <param name="defaultsFilePath">
        /// Absolute path to <c>filter-defaults.json</c>. When <c>null</c> or whitespace,
        /// <see cref="DefaultFilePath"/> is used.
        /// </param>
        /// <exception cref="IOException">Thrown when the file exists but cannot be deleted.</exception>
        public static void DeleteFileAt(string? defaultsFilePath = null)
        {
            var path = string.IsNullOrWhiteSpace(defaultsFilePath) ? DefaultFilePath() : defaultsFilePath.Trim();
            if (!File.Exists(path))
            {
                return;
            }

            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                throw new IOException($"Error deleting filter defaults file '{path}'.", ex);
            }
        }

        /// <summary>
        /// Saves the current type defaults to disk.
        /// </summary>
        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(DefaultsFilePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var sorted = _typeToDefault
                    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
                var container = new FilterDefaultsContainer(sorted);
                var json = JsonSerializer.Serialize(container, PresetJsonOptions.Default);
                File.WriteAllText(DefaultsFilePath, json);
            }
            catch (Exception ex)
            {
                throw new UserException($"Failed to save filter defaults file '{DefaultsFilePath}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Tries to get a cloned default for <paramref name="type"/> (JSON discriminator).
        /// </summary>
        /// <param name="type">Filter type discriminator (e.g. <c>LettersCase</c>).</param>
        /// <param name="filter">Cloned default when found.</param>
        /// <returns><see langword="true"/> when a saved default exists.</returns>
        public bool TryGetDefault(string type, [NotNullWhen(true)] out BaseFilter? filter)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(type);

            if (!_typeToDefault.TryGetValue(type, out var stored))
            {
                filter = null;
                return false;
            }

            filter = _Clone(stored);
            return true;
        }

        /// <summary>
        /// Stores a clone of <paramref name="filter"/> as the add default for its type and writes disk.
        /// </summary>
        /// <param name="filter">Current applied filter configuration to remember.</param>
        public void SetDefault(BaseFilter filter)
        {
            ArgumentNullException.ThrowIfNull(filter);
            _typeToDefault[filter.Type] = _Clone(filter);
            Save();
        }

        private static BaseFilter _Clone(BaseFilter filter)
        {
            var json = JsonSerializer.Serialize(filter, PresetJsonOptions.Default);
            return JsonSerializer.Deserialize<BaseFilter>(json, PresetJsonOptions.Default)
                ?? throw new InvalidOperationException($"Failed to clone filter type '{filter.Type}'.");
        }
    }

    internal sealed record FilterDefaultsContainer(
        [property: JsonPropertyName("defaults")] Dictionary<string, BaseFilter> Defaults
    );
}
