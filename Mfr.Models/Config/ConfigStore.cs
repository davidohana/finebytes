using System.Text.Json;
using Mfr.Utils;
using Mfr.Utils.Config;

namespace Mfr.Models.Config
{
    /// <summary>
    /// Loads and saves process-wide config as JSON.
    /// <para>Default file: <see cref="_DefaultConfigFilePath"/>.</para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Hard-fail dialect: when a config file exists but is invalid (or an explicit path is missing),
    /// <see cref="Load"/> throws <see cref="InvalidDataException"/> and aborts load. A missing default
    /// AppData file is fine (in-memory defaults). Opposite of soft-load
    /// <see cref="SessionStore"/> and <c>FilterDefaultsStore</c> (Engine); same family as
    /// <c>PresetManager</c> (Engine). Do not unify these modes — intentional product dialect.
    /// </para>
    /// <para>
    /// When the default AppData file is missing, <see cref="EnsureDefaultFile"/> writes one with current
    /// defaults so the user can hand-edit filter and log settings. The Options dialog persists selected
    /// UI prefs via <see cref="Save"/> (overwrite); other leaves remain hand-edit / CLI <c>--set</c>.
    /// When a property is omitted, values still come from <see cref="MfrConfig"/> field initializers.
    /// </para>
    /// <para>
    /// The document root must be a JSON object with nested sections (e.g. <c>filters</c>, <c>log</c>, <c>ui</c>). Each section is a JSON object;
    /// <see cref="ConfigJsonApplier.Apply"/> maps annotated fields on <see cref="MfrConfig"/> and nested section types using
    /// <see cref="ConfigValueReader"/>; every leaf value is read from a JSON <strong>string</strong>
    /// (including integers, e.g. <c>"1000"</c>, and booleans, e.g. <c>"true"</c>).
    /// </para>
    /// <para>
    /// Config binding is covered by <see cref="ApplyCliOverrides"/> tests and
    /// <see cref="ConfigJsonApplier"/> unit tests rather than a dedicated <c>ConfigStore</c> fixture type.
    /// </para>
    /// </remarks>
    public static class ConfigStore
    {
        private static readonly JsonSerializerOptions s_WriteOptions = new() { WriteIndented = true };

        /// <summary>
        /// Gets the active config for this process.
        /// </summary>
        public static MfrConfig Config { get; private set; } = new();

        /// <summary>
        /// Default JSON config path (<see cref="AppDataPaths.RoamingRoot"/> + <c>config.json</c>).
        /// </summary>
        /// <returns>Absolute path to the default config JSON file.</returns>
        private static string _DefaultConfigFilePath()
        {
            return AppDataPaths.RoamingRoot().CombinePath("config.json");
        }

        /// <summary>
        /// Resolves <paramref name="configFilePath"/> to the default config path when omitted.
        /// </summary>
        /// <param name="configFilePath">Explicit path, or blank for the default AppData file.</param>
        /// <returns>Absolute path to the config JSON file.</returns>
        private static string _ResolvePath(string? configFilePath)
        {
            return configFilePath.IsBlank() ? _DefaultConfigFilePath() : configFilePath.Trim();
        }

        /// <summary>
        /// Loads config from a JSON file when it exists; otherwise uses defaults.
        /// <para>Schema: see <see cref="ConfigStore"/> remarks.</para>
        /// </summary>
        /// <param name="configFilePath">
        /// Path to JSON. When <c>null</c> or whitespace, the default AppData path from <see cref="_DefaultConfigFilePath"/> is used.
        /// </param>
        /// <exception cref="InvalidDataException">
        /// Thrown when a user-supplied file path does not exist, or when the file exists but JSON is invalid or values are out of range.
        /// </exception>
        public static void Load(string? configFilePath = null)
        {
            var config = new MfrConfig();
            Config = config;

            var useDefaultPath = configFilePath.IsBlank();
            var path = _ResolvePath(configFilePath);
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
                ConfigJsonApplier.Apply(doc.RootElement, config);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Config file '{path}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deletes the config JSON file when it exists (Reset Configuration).
        /// <para>Missing files are a no-op. Does not change the in-memory <see cref="Config"/>.</para>
        /// </summary>
        /// <param name="configFilePath">
        /// Path to JSON. When <c>null</c> or whitespace, <see cref="_DefaultConfigFilePath"/> is used.
        /// </param>
        /// <exception cref="IOException">Thrown when the file exists but cannot be deleted.</exception>
        public static void DeleteDefaultFile(string? configFilePath = null)
        {
            var path = _ResolvePath(configFilePath);
            AppDataFile.DeleteFileIfExists(path, "configuration file");
        }

        /// <summary>
        /// Writes <see cref="Config"/> to JSON, creating the directory when needed.
        /// <para>Always overwrites. Used by the Options dialog after OK.</para>
        /// </summary>
        /// <param name="configFilePath">
        /// Path to JSON. When <c>null</c> or whitespace, <see cref="_DefaultConfigFilePath"/> is used.
        /// </param>
        /// <exception cref="IOException">Thrown when the file cannot be written.</exception>
        public static void Save(string? configFilePath = null)
        {
            var path = _ResolvePath(configFilePath);
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = ConfigJsonWriter.Write(Config).ToJsonString(s_WriteOptions);
            File.WriteAllText(path, json);
        }

        /// <summary>
        /// Writes <see cref="Config"/> to JSON when the file is missing, so it can be hand-edited.
        /// <para>
        /// Existing files are left unchanged. Failures are swallowed so a missing AppData write does not
        /// crash the app.
        /// </para>
        /// </summary>
        /// <param name="configFilePath">
        /// Path to JSON. When <c>null</c> or whitespace, <see cref="_DefaultConfigFilePath"/> is used.
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
    }
}
