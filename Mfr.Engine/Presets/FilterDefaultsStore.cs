using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using Mfr.Models.Config;

namespace Mfr.Engine.Presets
{
    /// <summary>
    /// In-memory cache of per-filter-type add defaults (Filter Configuration “save as default”).
    /// <para>
    /// One snapshot per <see cref="BaseFilter.Type"/> discriminator, persisted as the opaque
    /// <see cref="ConfigStore.FilterDefaultsJson"/> map inside <c>config.json</c> (not a separate file).
    /// Used when adding from the palette; not used by reset, presets, or session restore.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Soft-load dialect for entries: unknown or invalid type payloads are skipped (factory defaults on add).
    /// The prefs document itself is soft-loaded by <see cref="ConfigStore"/>. Opposite of hard-fail
    /// <see cref="PresetManager"/>. Does not own a file path — <see cref="ConfigStore"/> is the only
    /// writer of <c>config.json</c>.
    /// </remarks>
    public sealed class FilterDefaultsStore
    {
        private readonly Dictionary<string, BaseFilter> _typeToDefault = new(StringComparer.Ordinal);

        /// <summary>
        /// Opens a store and loads typed defaults from <see cref="ConfigStore.FilterDefaultsJson"/>
        /// (call after <see cref="ConfigStore.Load"/>).
        /// </summary>
        /// <returns>A store ready for get/set.</returns>
        public static FilterDefaultsStore OpenDefault()
        {
            var store = new FilterDefaultsStore();
            store.TryLoad();
            return store;
        }

        /// <summary>
        /// Creates an empty store that does not read <see cref="ConfigStore.FilterDefaultsJson"/>
        /// (tests and isolated UI hosts).
        /// </summary>
        /// <returns>A store with no type defaults loaded.</returns>
        public static FilterDefaultsStore CreateEmpty()
        {
            return new FilterDefaultsStore();
        }

        /// <summary>
        /// Loads defaults from <see cref="ConfigStore.FilterDefaultsJson"/> into the cache.
        /// <para>
        /// Missing or empty maps leave the cache empty. Unknown or invalid entries are skipped
        /// (factory defaults remain for those types).
        /// </para>
        /// </summary>
        public void TryLoad()
        {
            _typeToDefault.Clear();
            var defaults = ConfigStore.FilterDefaultsJson;
            if (defaults.Count == 0)
            {
                return;
            }

            foreach (var property in defaults)
            {
                if (property.Value is null)
                {
                    continue;
                }

                try
                {
                    var filter = JsonSerializer.Deserialize<BaseFilter>(property.Value, PresetJsonOptions.Default);
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

        /// <summary>
        /// Clears the in-memory type defaults without writing <c>config.json</c>.
        /// </summary>
        public void Clear()
        {
            _typeToDefault.Clear();
        }

        /// <summary>
        /// Merges the current type defaults into <see cref="ConfigStore.FilterDefaultsJson"/> and saves
        /// the whole prefs document via <see cref="ConfigStore.Save"/>.
        /// </summary>
        /// <exception cref="UserException">Thrown when the prefs file cannot be written.</exception>
        public void Save()
        {
            try
            {
                JsonObject map = [];
                foreach (var pair in _typeToDefault.OrderBy(entry => entry.Key, StringComparer.Ordinal))
                {
                    map[pair.Key] = JsonSerializer.SerializeToNode(pair.Value, PresetJsonOptions.Default);
                }

                ConfigStore.FilterDefaultsJson = map;
                ConfigStore.Save();
            }
            catch (Exception ex)
            {
                throw new UserException($"Failed to save filter defaults into config: {ex.Message}", ex);
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
        /// Stores a clone of <paramref name="filter"/> as the add default for its type and writes prefs.
        /// </summary>
        /// <param name="filter">Current applied filter configuration to remember.</param>
        public void SetDefault(BaseFilter filter)
        {
            ArgumentNullException.ThrowIfNull(filter);
            _typeToDefault[filter.Type] = _Clone(filter);
            Save();
        }

        /// <summary>
        /// Deep-clones <paramref name="filter"/> via JSON round-trip.
        /// </summary>
        /// <param name="filter">Filter to clone.</param>
        /// <returns>A new filter instance with the same options.</returns>
        private static BaseFilter _Clone(BaseFilter filter)
        {
            var json = JsonSerializer.Serialize(filter, PresetJsonOptions.Default);
            return JsonSerializer.Deserialize<BaseFilter>(json, PresetJsonOptions.Default)
                ?? throw new InvalidOperationException($"Failed to clone filter type '{filter.Type}'.");
        }
    }
}
