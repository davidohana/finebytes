using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Mfr.Utils;
using Mfr.Utils.Config;

namespace Mfr.Models.Config
{
    /// <summary>
    /// Loads and saves process-wide preferences as a single <c>config.json</c>
    /// (<c>log</c>/<c>ui</c> string leaves, <c>session</c>, and opaque <c>filterDefaults</c>).
    /// <para>Default file: <see cref="_DefaultConfigFilePath"/>.</para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Soft-load dialect for the whole prefs file: missing default AppData file → in-memory defaults.
    /// Corrupt / unreadable → defaults (app continues). Missing keys → field initializers / empty
    /// <see cref="Session"/> / empty <see cref="FilterDefaultsJson"/>. Invalid <c>log</c>/<c>ui</c>
    /// leaf → that leaf is skipped. Bad <c>filterDefaults</c> entries are skipped later by
    /// <c>FilterDefaultsStore</c> (Engine). Explicit <c>--config PATH</c> missing → hard-fail
    /// (CLI typo). Explicit path corrupt → soft to defaults. Opposite of hard-fail
    /// <c>PresetManager</c> (Engine) and CLI <c>--set</c> (<see cref="ApplyCliOverrides"/>).
    /// </para>
    /// <para>
    /// When the default AppData file is missing, <see cref="EnsureDefaultFile"/> writes one with current
    /// defaults so the user can hand-edit log settings. Empty <c>session</c> / <c>filterDefaults</c> are
    /// omitted (same as first launch). Options, session close-save, and filter-default pin all persist via
    /// <see cref="Save"/> (whole document overwrite). Null session properties are omitted on write.
    /// When a property is omitted, values still come from <see cref="MfrConfig"/> field initializers.
    /// </para>
    /// <para>
    /// Document shape: root object with <c>log</c>/<c>ui</c> (string leaves via
    /// <see cref="ConfigJsonApplier"/> / <see cref="ConfigJsonWriter"/>), <c>session</c>
    /// (<see cref="SessionState"/> via STJ), and <c>filterDefaults</c> (opaque map of type → filter JSON;
    /// not nested under <c>defaults</c>).
    /// </para>
    /// </remarks>
    public static class ConfigStore
    {
        private static readonly JsonSerializerOptions s_WriteOptions = new() { WriteIndented = true };

        private static readonly JsonSerializerOptions s_SessionJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        };

        private static string? s_ActiveConfigFilePath;

        /// <summary>
        /// Gets the active config for this process.
        /// </summary>
        public static MfrConfig Config { get; private set; } = new();

        /// <summary>
        /// Gets or sets the active UI session document for this process.
        /// </summary>
        public static SessionState Session { get; set; } = new();

        /// <summary>
        /// Gets or sets the opaque <c>filterDefaults</c> map (type discriminator → filter JSON object).
        /// <para>Empty when omitted or unset. Typed deserialization lives in Engine <c>FilterDefaultsStore</c>.</para>
        /// </summary>
        public static JsonObject FilterDefaultsJson { get; set; } = [];

        /// <summary>
        /// Default JSON config path (<see cref="AppDataPaths.RoamingRoot"/> + <c>config.json</c>).
        /// </summary>
        /// <returns>Absolute path to the default config JSON file.</returns>
        private static string _DefaultConfigFilePath()
        {
            return AppDataPaths.RoamingRoot().CombinePath("config.json");
        }

        /// <summary>
        /// Resolves <paramref name="configFilePath"/> to the active or default config path when omitted.
        /// </summary>
        /// <param name="configFilePath">Explicit path, or blank for the active / default AppData file.</param>
        /// <returns>Absolute path to the config JSON file.</returns>
        private static string _ResolvePath(string? configFilePath)
        {
            if (!configFilePath.IsBlank())
            {
                return configFilePath.Trim();
            }

            return s_ActiveConfigFilePath.IsBlank() ? _DefaultConfigFilePath() : s_ActiveConfigFilePath.Trim();
        }

        /// <summary>
        /// Loads preferences from a JSON file when it exists; otherwise uses defaults.
        /// <para>Schema: see <see cref="ConfigStore"/> remarks.</para>
        /// </summary>
        /// <param name="configFilePath">
        /// Path to JSON. When <c>null</c> or whitespace, the default AppData path from <see cref="_DefaultConfigFilePath"/> is used.
        /// </param>
        /// <exception cref="InvalidDataException">
        /// Thrown when a user-supplied file path does not exist.
        /// </exception>
        public static void Load(string? configFilePath = null)
        {
            _ResetToDefaults();

            var useDefaultPath = configFilePath.IsBlank();
            var path = useDefaultPath ? _DefaultConfigFilePath() : configFilePath!.Trim();
            s_ActiveConfigFilePath = path;

            if (!File.Exists(path))
            {
                if (!useDefaultPath)
                {
                    throw new InvalidDataException($"Config file not found: '{path}'.");
                }

                return;
            }

            try
            {
                var json = File.ReadAllText(path);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    _ResetToDefaultsKeepingActivePath(path);
                    return;
                }

                ConfigJsonApplier.ApplySoft(doc.RootElement, Config);
                Session = _ReadSession(doc.RootElement);
                FilterDefaultsJson = _ReadFilterDefaults(doc.RootElement);
            }
            catch
            {
                // Soft-load: corrupt / unreadable prefs → defaults; app continues.
                _ResetToDefaultsKeepingActivePath(path);
            }
        }

        /// <summary>
        /// Deletes the config JSON file when it exists (Reset Configuration).
        /// <para>Missing files are a no-op. Does not change in-memory prefs (see <see cref="ClearSessionAndFilterDefaults"/>).</para>
        /// </summary>
        /// <param name="configFilePath">
        /// Path to JSON. When <c>null</c> or whitespace, <see cref="_ResolvePath"/> is used.
        /// </param>
        /// <exception cref="IOException">Thrown when the file exists but cannot be deleted.</exception>
        public static void DeleteDefaultFile(string? configFilePath = null)
        {
            var path = _ResolvePath(configFilePath);
            AppDataFile.DeleteFileIfExists(path, "configuration file");
        }

        /// <summary>
        /// Clears in-memory <see cref="Session"/> and <see cref="FilterDefaultsJson"/> after Reset Configuration.
        /// <para>Does not change <see cref="Config"/> or the active file path (app restarts after reset).</para>
        /// </summary>
        public static void ClearSessionAndFilterDefaults()
        {
            Session = new SessionState();
            FilterDefaultsJson = [];
        }

        /// <summary>
        /// Writes <see cref="Config"/>, <see cref="Session"/>, and <see cref="FilterDefaultsJson"/> to JSON,
        /// creating the directory when needed.
        /// <para>Always overwrites. Used by Options OK, session close-save, and filter-default pin.</para>
        /// </summary>
        /// <param name="configFilePath">
        /// Path to JSON. When <c>null</c> or whitespace, <see cref="_ResolvePath"/> is used.
        /// </param>
        /// <exception cref="IOException">Thrown when the file cannot be written.</exception>
        public static void Save(string? configFilePath = null)
        {
            var path = _ResolvePath(configFilePath);
            s_ActiveConfigFilePath = path;

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var root = ConfigJsonWriter.Write(Config);
            var sessionNode = JsonSerializer.SerializeToNode(Session, s_SessionJsonOptions);
            if (sessionNode is JsonObject { Count: > 0 } sessionObject)
            {
                root["session"] = sessionObject;
            }

            if (FilterDefaultsJson is { Count: > 0 })
            {
                root["filterDefaults"] = FilterDefaultsJson;
            }

            File.WriteAllText(path, root.ToJsonString(s_WriteOptions));
        }

        /// <summary>
        /// Writes the prefs document.
        /// <para>Failures are swallowed so preference saves do not crash the app.</para>
        /// </summary>
        /// <param name="configFilePath">
        /// Path to JSON. When <c>null</c> or whitespace, <see cref="_ResolvePath"/> is used.
        /// </param>
        public static void TrySave(string? configFilePath = null)
        {
            try
            {
                Save(configFilePath);
            }
            catch
            {
                // Preference save must not block the UI or surface to the user.
            }
        }

        /// <summary>
        /// Writes defaults to JSON when the file is missing, so it can be hand-edited.
        /// <para>
        /// Existing files are left unchanged. Failures are swallowed so a missing AppData write does not
        /// crash the app. Empty <c>session</c> / <c>filterDefaults</c> are omitted until first real save.
        /// </para>
        /// </summary>
        /// <param name="configFilePath">
        /// Path to JSON. When <c>null</c> or whitespace, <see cref="_ResolvePath"/> is used.
        /// </param>
        public static void EnsureDefaultFile(string? configFilePath = null)
        {
            try
            {
                var path = _ResolvePath(configFilePath);
                if (File.Exists(path))
                {
                    return;
                }

                Save(path);
            }
            catch
            {
                // Creating the hand-edit file must not block startup.
            }
        }

        /// <summary>
        /// Applies CLI <c>--set</c> overrides to <see cref="Config"/> (after <see cref="Load"/>).
        /// <para>Keys are dotted paths (e.g. <c>log.maxSessionFiles</c>) matching <c>config.json</c>.</para>
        /// </summary>
        /// <param name="assignments">Raw <c>key=value</c> strings from the CLI; blank entries are skipped.</param>
        /// <exception cref="InvalidDataException">Thrown when an assignment is malformed, the path is unknown, or a value is out of range.</exception>
        public static void ApplyCliOverrides(IEnumerable<string> assignments)
        {
            ArgumentNullException.ThrowIfNull(assignments);

            var list = assignments.Where(a => !a.IsBlank()).Select(a => a.Trim()).ToList();
            if (list.Count == 0)
            {
                return;
            }

            try
            {
                ConfigOverridesApplier.Apply(list, Config);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"CLI config override: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Resets in-memory prefs to factory defaults without changing the active file path.
        /// </summary>
        private static void _ResetToDefaults()
        {
            Config = new MfrConfig();
            Session = new SessionState();
            FilterDefaultsJson = [];
        }

        /// <summary>
        /// Resets in-memory prefs after a soft load failure while keeping <paramref name="path"/> active.
        /// </summary>
        /// <param name="path">Resolved config path that failed to load.</param>
        private static void _ResetToDefaultsKeepingActivePath(string path)
        {
            _ResetToDefaults();
            s_ActiveConfigFilePath = path;
        }

        /// <summary>
        /// Reads the <c>session</c> object, or an empty session when missing or unreadable.
        /// </summary>
        /// <param name="root">Document root object.</param>
        /// <returns>Deserialized session, or a new empty session.</returns>
        private static SessionState _ReadSession(JsonElement root)
        {
            if (!_TryGetPropertyIgnoreCase(root, "session", out var sessionElement))
            {
                return new SessionState();
            }

            if (sessionElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return new SessionState();
            }

            if (sessionElement.ValueKind != JsonValueKind.Object)
            {
                return new SessionState();
            }

            try
            {
                return JsonSerializer.Deserialize<SessionState>(sessionElement.GetRawText(), s_SessionJsonOptions)
                    ?? new SessionState();
            }
            catch
            {
                return new SessionState();
            }
        }

        /// <summary>
        /// Reads the opaque <c>filterDefaults</c> map, or an empty object when missing or unreadable.
        /// </summary>
        /// <param name="root">Document root object.</param>
        /// <returns>Filter-defaults JSON object (never null).</returns>
        private static JsonObject _ReadFilterDefaults(JsonElement root)
        {
            if (!_TryGetPropertyIgnoreCase(root, "filterDefaults", out var defaultsElement))
            {
                return [];
            }

            if (defaultsElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return [];
            }

            if (defaultsElement.ValueKind != JsonValueKind.Object)
            {
                return [];
            }

            try
            {
                return JsonNode.Parse(defaultsElement.GetRawText()) as JsonObject ?? [];
            }
            catch
            {
                return [];
            }
        }

        /// <summary>
        /// Finds a property on <paramref name="root"/> by case-insensitive name.
        /// </summary>
        /// <param name="root">JSON object.</param>
        /// <param name="propertyName">Property name to match.</param>
        /// <param name="value">Matched element when found.</param>
        /// <returns><see langword="true"/> when the property exists.</returns>
        private static bool _TryGetPropertyIgnoreCase(JsonElement root, string propertyName, out JsonElement value)
        {
            foreach (var prop in root.EnumerateObject())
            {
                if (!string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                value = prop.Value;
                return true;
            }

            value = default;
            return false;
        }
    }
}
