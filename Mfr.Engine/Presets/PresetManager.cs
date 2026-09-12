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
    /// <para>
    /// Display and save order is the <see cref="Presets"/> list (JSON array order). Use the mutation
    /// APIs to keep the list and <see cref="NameToPreset"/> lookup in sync; do not mutate the
    /// dictionary directly.
    /// </para>
    /// </remarks>
    /// <param name="presetsFilePath">Path to the JSON file containing all presets.</param>
    public sealed class PresetManager(string presetsFilePath)
    {
        private readonly List<FilterPreset> _presets = [];
        private readonly Dictionary<string, FilterPreset> _nameToPreset = [];

        /// <summary>
        /// Gets loaded presets in stored display/save order.
        /// </summary>
        public IReadOnlyList<FilterPreset> Presets => _presets;

        /// <summary>
        /// Gets loaded presets keyed by preset name (O(1) lookup).
        /// </summary>
        public IReadOnlyDictionary<string, FilterPreset> NameToPreset => _nameToPreset;

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

            _presets.Clear();
            _nameToPreset.Clear();
            foreach (var preset in container.Presets)
            {
                if (!_nameToPreset.TryAdd(preset.Name, preset))
                {
                    throw new UserException(
                        $"Duplicate preset names found in '{PresetsFilePath}'. Preset names must be unique."
                    );
                }

                _presets.Add(preset);
            }
        }

        /// <summary>
        /// Saves currently loaded presets to the configured presets file in stored order.
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

                var container = new PresetContainer(_presets);
                var json = JsonSerializer.Serialize(container, PresetJsonOptions.Default);
                File.WriteAllText(PresetsFilePath, json);
            }
            catch (Exception ex)
            {
                throw new UserException($"Failed to save presets file '{PresetsFilePath}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Inserts <paramref name="preset"/> or replaces the existing entry with the same name in place.
        /// </summary>
        /// <param name="preset">Preset to store (name is the lookup key).</param>
        public void Upsert(FilterPreset preset)
        {
            ArgumentNullException.ThrowIfNull(preset);

            if (_nameToPreset.ContainsKey(preset.Name))
            {
                var index = _IndexOfName(preset.Name);
                _presets[index] = preset;
                _nameToPreset[preset.Name] = preset;
                return;
            }

            _presets.Add(preset);
            _nameToPreset[preset.Name] = preset;
        }

        /// <summary>
        /// Removes the preset with the exact <paramref name="name"/> key.
        /// </summary>
        /// <param name="name">Exact preset name.</param>
        /// <returns><see langword="true"/> when a preset was removed.</returns>
        public bool Remove(string name)
        {
            ArgumentNullException.ThrowIfNull(name);

            if (!_nameToPreset.ContainsKey(name))
            {
                return false;
            }

            var index = _IndexOfName(name);
            _nameToPreset.Remove(name);
            _presets.RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Renames a preset in place (same list index); updates the lookup key.
        /// </summary>
        /// <param name="currentName">Exact current name key.</param>
        /// <param name="renamed">Preset with the new name (and any other updated fields).</param>
        /// <returns><see langword="false"/> when <paramref name="currentName"/> is missing.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="renamed"/>’s name is already used by another preset.
        /// </exception>
        public bool TryRename(string currentName, FilterPreset renamed)
        {
            ArgumentNullException.ThrowIfNull(currentName);
            ArgumentNullException.ThrowIfNull(renamed);

            if (!_nameToPreset.ContainsKey(currentName))
            {
                return false;
            }

            var nameChanged = !string.Equals(currentName, renamed.Name, StringComparison.Ordinal);
            if (nameChanged && _nameToPreset.ContainsKey(renamed.Name))
            {
                throw new InvalidOperationException($"A preset named '{renamed.Name}' already exists; rename refused.");
            }

            var index = _IndexOfName(currentName);
            _nameToPreset.Remove(currentName);
            _presets[index] = renamed;
            _nameToPreset[renamed.Name] = renamed;
            return true;
        }

        /// <summary>
        /// Moves selected presets one step toward <paramref name="offset"/> (Up/Down).
        /// </summary>
        /// <param name="selectedNames">Exact names of presets to move.</param>
        /// <param name="offset">Direction (-1 up, +1 down).</param>
        /// <returns><see langword="true"/> when at least one preset changed position.</returns>
        public bool TryMoveSelectedTowardNeighbor(IReadOnlyCollection<string> selectedNames, int offset)
        {
            ArgumentNullException.ThrowIfNull(selectedNames);

            return ListReorder.TryMoveSelectedTowardNeighbor(_presets, _PresetsMatchingNames(selectedNames), offset);
        }

        /// <summary>
        /// Whether any selected preset can move one step toward <paramref name="offset"/>.
        /// </summary>
        /// <param name="selectedNames">Exact names of presets to move.</param>
        /// <param name="offset">Direction (-1 up, +1 down).</param>
        /// <returns><see langword="true"/> when a neighbor swap is possible.</returns>
        public bool CanMoveSelectedTowardNeighbor(IReadOnlyCollection<string> selectedNames, int offset)
        {
            ArgumentNullException.ThrowIfNull(selectedNames);

            return ListReorder.CanMoveSelectedTowardNeighbor(_presets, _PresetsMatchingNames(selectedNames), offset);
        }

        /// <summary>
        /// Moves presets at <paramref name="sourceIndices"/> to <paramref name="targetIndex"/>.
        /// </summary>
        /// <param name="sourceIndices">Indices of presets to move.</param>
        /// <param name="targetIndex">Destination index before the move.</param>
        /// <param name="newIndices">Indices of the moved presets after a successful move.</param>
        /// <returns><see langword="false"/> when the move is not allowed or is a no-op.</returns>
        public bool TryMoveIndicesTo(
            IReadOnlyList<int> sourceIndices,
            int targetIndex,
            out IReadOnlyList<int> newIndices
        )
        {
            return ListReorder.TryMoveIndicesTo(_presets, sourceIndices, targetIndex, out newIndices);
        }

        /// <summary>
        /// Resolves list instances whose names are in <paramref name="selectedNames"/>.
        /// </summary>
        private HashSet<FilterPreset> _PresetsMatchingNames(IReadOnlyCollection<string> selectedNames)
        {
            if (selectedNames.Count == 0)
            {
                return [];
            }

            var nameToIsSelected = selectedNames.ToHashSet(StringComparer.Ordinal);
            return [.. _presets.Where(preset => nameToIsSelected.Contains(preset.Name))];
        }

        /// <summary>
        /// Finds the list index of the preset with <paramref name="name"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the name is in <see cref="NameToPreset"/> but missing from <see cref="Presets"/>
        /// (list/lookup invariant broken).
        /// </exception>
        private int _IndexOfName(string name)
        {
            for (var index = 0; index < _presets.Count; index++)
            {
                if (string.Equals(_presets[index].Name, name, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            throw new InvalidOperationException(
                $"Preset '{name}' is in the name lookup but missing from the ordered list."
            );
        }
    }

    internal sealed record PresetContainer([property: JsonPropertyName("presets")] IReadOnlyList<FilterPreset> Presets);
}
