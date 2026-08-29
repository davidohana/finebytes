using System.Text.Json;
using System.Text.Json.Nodes;
using Mfr.Utils;
using Mfr.Utils.Config;

namespace Mfr.Models.Config
{
    /// <summary>
    /// Loads optional process-wide config from JSON.
    /// <para>Default file: <see cref="_DefaultConfigFilePath"/>.</para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>config.json</c> is optional. When the file is missing, or a property is omitted, values come from
    /// <see cref="MfrConfig"/> field initializers. <see cref="Save"/> merge-writes the in-memory
    /// <see cref="Config"/> without removing unrelated keys.
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
        /// <summary>
        /// Gets the active config for this process.
        /// </summary>
        public static MfrConfig Config { get; private set; } = new();

        /// <summary>
        /// Default JSON config path (<see cref="AppDataPaths.RoamingRoot"/> + <c>config.json</c>).
        /// </summary>
        private static string _DefaultConfigFilePath()
        {
            return AppDataPaths.RoamingRoot().CombinePath("config.json");
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
            var path = useDefaultPath ? _DefaultConfigFilePath() : configFilePath!.Trim();
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
        /// Merge-writes <see cref="Config"/> to JSON.
        /// <para>
        /// When the file already exists, unrelated sections and keys are preserved. Failures are swallowed so UI
        /// saves do not crash the app.
        /// </para>
        /// </summary>
        /// <param name="configFilePath">
        /// Path to JSON. When <c>null</c> or whitespace, the default AppData path from <see cref="_DefaultConfigFilePath"/> is used.
        /// </param>
        public static void Save(string? configFilePath = null)
        {
            try
            {
                var path = configFilePath.IsBlank() ? _DefaultConfigFilePath() : configFilePath.Trim();
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                JsonObject root;
                if (File.Exists(path))
                {
                    var existingJson = File.ReadAllText(path);
                    root = JsonNode.Parse(existingJson)?.AsObject() ?? [];
                }
                else
                {
                    root = [];
                }

                ConfigJsonWriter.MergeInto(root, Config);
                var json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch
            {
                // Config save must not block UI or surface to the user.
            }
        }
    }
}
