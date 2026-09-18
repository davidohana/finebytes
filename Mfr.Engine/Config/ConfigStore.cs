using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Mfr.Utils;
using Mfr.Utils.Config;

namespace Mfr.Engine.Config
{
    /// <summary>
    /// Loads and saves process-wide preferences as a single <c>config.json</c>:
    /// app config sections (<c>log</c>/<c>options</c>/<c>renameLog</c> string leaves), UI session sections
    /// (<c>mainWindow</c>/<c>fileList</c>/<c>renameList</c>/<c>filterEditor</c>/<c>dialogs</c>), and opaque
    /// <c>filterDefaults</c>.
    /// <para>Default file: <see cref="_DefaultConfigFilePath"/>.</para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Soft-load dialect for the whole prefs file: missing default AppData file → in-memory defaults.
    /// Corrupt / unreadable → defaults (app continues). Missing keys → field initializers / null UI session
    /// sections / empty <see cref="FilterDefaultsJson"/>. Invalid <c>log</c>/<c>options</c>/<c>renameLog</c>
    /// leaf → that leaf is skipped. Unknown members inside <c>options.suppressedConfirmations</c> are skipped;
    /// obsolete <c>options.confirmationPrompts</c> / root <c>ui</c> / Options fields formerly under
    /// <c>fileList</c> / <c>renameList</c> are ignored (no migration). Bad <c>filterDefaults</c> entries
    /// are skipped later by <c>FilterDefaultsStore</c> (Engine). Explicit <c>--config PATH</c> missing →
    /// hard-fail (CLI typo). Explicit path corrupt → soft to defaults. Opposite of hard-fail
    /// <c>PresetManager</c> (Engine) and CLI <c>--set</c> (<see cref="ApplyCliOverrides"/>).
    /// </para>
    /// <para>
    /// When the default AppData file is missing, <see cref="EnsureDefaultFile"/> writes one with current
    /// defaults so the user can hand-edit log settings. Empty UI session sections / <c>filterDefaults</c> are
    /// omitted (same as first launch). Options, session close-save, and filter-default pin all persist via
    /// <see cref="Save"/> (whole document overwrite). Null UI session section properties are omitted on write.
    /// When a property is omitted, values still come from <see cref="LogConfig"/> / <see cref="OptionsConfig"/> /
    /// <see cref="RenameLogConfig"/> field initializers.
    /// </para>
    /// <para>
    /// Document shape: root object with app config sections <c>log</c>/<c>options</c>/<c>renameLog</c>
    /// (string leaves and enum-list arrays via <see cref="ConfigJsonApplier"/> / <see cref="ConfigJsonWriter"/>),
    /// sibling UI session sections (<c>mainWindow</c>, <c>fileList</c>, <c>renameList</c>, <c>filterEditor</c>,
    /// <c>dialogs</c> via STJ), and <c>filterDefaults</c> (opaque map of type → filter JSON; not nested under
    /// <c>defaults</c>). Nested <c>session</c> is not read (no migration).
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
        private static PrefsRoot s_Prefs = new();

        /// <summary>
        /// Gets the diagnostic session-log options for this process.
        /// </summary>
        public static LogConfig Log => s_Prefs.Log;

        /// <summary>
        /// Gets the Options prefs (<c>options</c> app config section: confirms, remember flags, add policy).
        /// </summary>
        public static OptionsConfig Options => s_Prefs.Options;

        /// <summary>
        /// Gets rename-commit undo log retention (<c>renameLog.limit</c>).
        /// <para>Options dialog Undo &amp; Rename Log section; also read by rename-log capture/trim.</para>
        /// </summary>
        public static RenameLogConfig RenameLog => s_Prefs.RenameLog;

        /// <summary>
        /// Gets or sets the last main-window geometry and pane splitters, when remembered.
        /// </summary>
        public static MainWindowPrefs? MainWindow { get; set; }

        /// <summary>
        /// Gets or sets the last File List folder, masks, and view chrome.
        /// </summary>
        public static FileListPrefs? FileList { get; set; }

        /// <summary>
        /// Gets or sets the last Rename List Auto-Sort and related UI session fields.
        /// </summary>
        public static RenameListPrefs? RenameList { get; set; }

        /// <summary>
        /// Gets or sets Filter Configuration chrome (e.g. format-token picker collapse).
        /// </summary>
        public static FilterEditorPrefs? FilterEditor { get; set; }

        /// <summary>
        /// Gets or sets size/position per resizable modal dialog id, when remembered.
        /// <para>Root <c>dialogs</c> map; gated by <see cref="OptionsConfig.RememberWindowState"/>.</para>
        /// </summary>
        public static Dictionary<string, WindowGeometryPrefs>? Dialogs { get; set; }

        /// <summary>
        /// Gets or sets the opaque <c>filterDefaults</c> map (type discriminator → filter JSON object).
        /// <para>Empty when omitted or unset. Typed deserialization lives in Engine <c>FilterDefaultsStore</c>.</para>
        /// </summary>
        public static JsonObject FilterDefaultsJson { get; set; } = [];

        /// <summary>
        /// Returns <see cref="MainWindow"/>, creating it when missing.
        /// </summary>
        /// <returns>The main-window session object.</returns>
        public static MainWindowPrefs EnsureMainWindow()
        {
            return MainWindow ??= new MainWindowPrefs();
        }

        /// <summary>
        /// Returns <see cref="Dialogs"/>, creating it when missing.
        /// </summary>
        /// <returns>The root dialog-geometry map.</returns>
        public static Dictionary<string, WindowGeometryPrefs> EnsureDialogs()
        {
            return Dialogs ??= new Dictionary<string, WindowGeometryPrefs>(StringComparer.Ordinal);
        }

        /// <summary>
        /// Returns <see cref="FileList"/>, creating it when missing.
        /// </summary>
        /// <returns>The File List session object.</returns>
        public static FileListPrefs EnsureFileList()
        {
            return FileList ??= new FileListPrefs();
        }

        /// <summary>
        /// Returns <see cref="RenameList"/>, creating it when missing.
        /// </summary>
        /// <returns>The Rename List session object.</returns>
        public static RenameListPrefs EnsureRenameList()
        {
            return RenameList ??= new RenameListPrefs();
        }

        /// <summary>
        /// Returns <see cref="FilterEditor"/>, creating it when missing.
        /// </summary>
        /// <returns>The Filter Configuration session object.</returns>
        public static FilterEditorPrefs EnsureFilterEditor()
        {
            return FilterEditor ??= new FilterEditorPrefs();
        }

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

                ConfigJsonApplier.ApplySoft(doc.RootElement, s_Prefs);
                MainWindow = _ReadSection<MainWindowPrefs>(doc.RootElement, "mainWindow");
                FileList = _ReadSection<FileListPrefs>(doc.RootElement, "fileList");
                RenameList = _ReadSection<RenameListPrefs>(doc.RootElement, "renameList");
                FilterEditor = _ReadSection<FilterEditorPrefs>(doc.RootElement, "filterEditor");
                Dialogs = _ReadSection<Dictionary<string, WindowGeometryPrefs>>(doc.RootElement, "dialogs");
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
        /// <para>
        /// Removes <c>config.json</c> only (log/options/renameLog + UI session sections + filterDefaults).
        /// Does not touch <c>presets.json</c> or local logs. Missing files are a no-op. Does not change
        /// in-memory prefs — the UI exits after delete (with session save suppressed) so the next process
        /// soft-loads defaults.
        /// </para>
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
        /// Writes prefs and UI session sections to JSON, creating the directory when needed.
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

            var root = ConfigJsonWriter.Write(s_Prefs);
            _WriteSection(root, "mainWindow", MainWindow);
            _WriteSection(root, "fileList", FileList);
            _WriteSection(root, "renameList", RenameList);
            _WriteSection(root, "filterEditor", FilterEditor);
            _WriteSection(root, "dialogs", Dialogs);

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
        /// crash the app. Empty UI session sections / <c>filterDefaults</c> are omitted until first real save.
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
        /// Applies CLI <c>--set</c> overrides to <see cref="Log"/> / <see cref="Options"/> / <see cref="RenameLog"/> (after <see cref="Load"/>).
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
                ConfigOverridesApplier.Apply(list, s_Prefs);
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
            s_Prefs = new PrefsRoot();
            MainWindow = null;
            FileList = null;
            RenameList = null;
            FilterEditor = null;
            Dialogs = null;
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
        /// Reads a root UI session section, or <see langword="null"/> when missing or unreadable.
        /// </summary>
        /// <typeparam name="T">UI session section type.</typeparam>
        /// <param name="root">Document root object.</param>
        /// <param name="propertyName">Root property name.</param>
        /// <returns>Deserialized section, or <see langword="null"/>.</returns>
        private static T? _ReadSection<T>(JsonElement root, string propertyName)
            where T : class
        {
            if (!JsonObjectProperties.TryGetPropertyIgnoreCase(root, propertyName, out var sectionElement))
            {
                return null;
            }

            if (sectionElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return null;
            }

            if (sectionElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<T>(sectionElement.GetRawText(), s_SessionJsonOptions);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Writes a UI session section when non-null and non-empty after null-ignore serialization.
        /// </summary>
        /// <typeparam name="T">UI session section type.</typeparam>
        /// <param name="root">Document root object.</param>
        /// <param name="propertyName">Root property name.</param>
        /// <param name="section">Section to write, or <see langword="null"/> to omit.</param>
        private static void _WriteSection<T>(JsonObject root, string propertyName, T? section)
            where T : class
        {
            if (section is null)
            {
                return;
            }

            var sectionNode = JsonSerializer.SerializeToNode(section, s_SessionJsonOptions);
            if (sectionNode is JsonObject { Count: > 0 } sectionObject)
            {
                root[propertyName] = sectionObject;
            }
        }

        /// <summary>
        /// Reads the opaque <c>filterDefaults</c> map, or an empty object when missing or unreadable.
        /// </summary>
        /// <param name="root">Document root object.</param>
        /// <returns>Filter-defaults JSON object (never null).</returns>
        private static JsonObject _ReadFilterDefaults(JsonElement root)
        {
            if (!JsonObjectProperties.TryGetPropertyIgnoreCase(root, "filterDefaults", out var defaultsElement))
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
        /// Private root for string-leaf <c>log</c>/<c>options</c>/<c>renameLog</c> app config binding via
        /// <see cref="ConfigJsonApplier"/>.
        /// </summary>
        private sealed class PrefsRoot
        {
            /// <summary>
            /// Diagnostic session-log options.
            /// </summary>
            [ConfigSection]
            public LogConfig Log = new();

            /// <summary>
            /// Options dialog prefs (confirms, remember flags, File List double-click, Rename List add policy).
            /// </summary>
            [ConfigSection("options")]
            public OptionsConfig Options = new();

            /// <summary>
            /// Rename-commit undo log retention (<c>limit</c>).
            /// </summary>
            [ConfigSection]
            public RenameLogConfig RenameLog = new();
        }
    }
}
