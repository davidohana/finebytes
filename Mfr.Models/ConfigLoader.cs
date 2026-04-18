using System.Text;
using System.Text.Json;
using Mfr.Utils;
using Mfr.Utils.Config;

namespace Mfr.Models
{
    /// <summary>
    /// Loads optional process-wide settings from JSON.
    /// <para>Default file: <see cref="DefaultConfigFilePath"/>.</para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>mfr.config.json</c> is optional. When the file is missing, or a property is omitted, values come from
    /// <see cref="MfrSettings"/> field initializers.
    /// </para>
    /// <para>
    /// The document root must be a JSON object with nested sections (e.g. <c>filters</c>, <c>log</c>). Each section is a JSON object;
    /// <see cref="ConfigJsonApplier.Apply"/> maps annotated fields on <see cref="MfrSettings"/> and nested section types using
    /// <see cref="ConfigValueReader"/>; every leaf value is read from a JSON <strong>string</strong> (including integers, e.g. <c>"1000"</c>).
    /// </para>
    /// <para>
    /// File I/O uses UTF-8. Config binding is covered by <see cref="ApplyCliOverrides"/> tests and
    /// <see cref="ConfigJsonApplier"/> unit tests rather than a dedicated <c>ConfigLoader</c> fixture type.
    /// </para>
    /// </remarks>
    public static class ConfigLoader
    {
        /// <summary>
        /// Gets the active settings for this process.
        /// </summary>
        public static MfrSettings Settings { get; private set; } = new();

        /// <summary>
        /// Gets the default JSON config file path.
        /// <para><c>%ApplicationData%/MagicFileRenamer/mfr.config.json</c>.</para>
        /// </summary>
        /// <returns>An absolute file path.</returns>
        public static string DefaultConfigFilePath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return appData.CombinePath("MagicFileRenamer", "mfr.config.json");
        }

        /// <summary>
        /// Loads settings from a JSON file when it exists; otherwise uses defaults.
        /// <para>Schema: see <see cref="ConfigLoader"/> remarks.</para>
        /// </summary>
        /// <param name="configFilePath">
        /// Path to JSON. When <c>null</c> or whitespace, <see cref="DefaultConfigFilePath"/> is used.
        /// </param>
        /// <exception cref="InvalidDataException">
        /// Thrown when a user-supplied file path does not exist, or when the file exists but JSON is invalid or settings are out of range.
        /// </exception>
        public static void Load(string? configFilePath = null)
        {
            var settings = new MfrSettings();
            Settings = settings;

            var useDefaultPath = configFilePath.IsBlank();
            var path = useDefaultPath ? DefaultConfigFilePath() : configFilePath!.Trim();
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
                var json = File.ReadAllText(path, Encoding.UTF8);
                using var doc = JsonDocument.Parse(json);
                ConfigJsonApplier.Apply(doc.RootElement, settings);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Config file '{path}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Applies CLI <c>--set</c> overrides to <see cref="Settings"/> (after <see cref="Load"/>).
        /// <para>Keys are dotted paths (e.g. <c>log.maxSessionFiles</c>) matching <c>mfr.config.json</c>.</para>
        /// </summary>
        /// <param name="assignments">Raw <c>key=value</c> strings from the CLI; blank entries are skipped.</param>
        /// <exception cref="InvalidDataException">Thrown when an assignment is malformed, the path is unknown, or a value is out of range.</exception>
        public static void ApplyCliOverrides(IEnumerable<string> assignments)
        {
            ArgumentNullException.ThrowIfNull(assignments);

            var list = assignments
                .Where(a => !a.IsBlank())
                .Select(a => a.Trim())
                .ToList();
            if (list.Count == 0)
            {
                return;
            }

            try
            {
                ConfigOverridesApplier.Apply(list, Settings);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"CLI config override: {ex.Message}", ex);
            }
        }
    }
}
