using System.Text.Json;
using Mfr.Utils;

namespace Mfr.Models
{
    /// <summary>
    /// Process-wide settings optionally loaded from a user JSON file (see <see cref="DefaultConfigFilePath"/>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>mfr.config.json</c> is optional. When the file is missing, or a property is omitted, values come from
    /// <see cref="MfrSettings"/> field initializers.
    /// </para>
    /// <para>
    /// The document root must be a JSON object. <see cref="ConfigValueReader"/> reads every setting from a JSON
    /// <strong>string</strong> value (including integers, e.g. <c>"1000"</c>).
    /// </para>
    /// <para>
    /// Do not add a dedicated test type for this class; config loading is exercised indirectly (for example via CLI and
    /// consumers of <see cref="Settings"/>). This is intentional—keep it that way.
    /// </para>
    /// </remarks>
    public static class ConfigLoader
    {
        /// <summary>
        /// Gets the active settings for this process.
        /// </summary>
        public static MfrSettings Settings { get; private set; } = new();

        /// <summary>
        /// Gets the default path to the optional JSON config file:
        /// <c>%ApplicationData%/MagicFileRenamer/mfr.config.json</c>.
        /// </summary>
        /// <returns>An absolute file path.</returns>
        public static string DefaultConfigFilePath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return appData.CombinePath("MagicFileRenamer", "mfr.config.json");
        }

        /// <summary>
        /// Reads process settings from a JSON file when it exists; otherwise assigns a new <see cref="MfrSettings"/> instance
        /// with default field values. See remarks on <see cref="ConfigLoader"/> for the JSON schema.
        /// </summary>
        /// <param name="configFilePath">
        /// Path to JSON. When <c>null</c> or whitespace, <see cref="DefaultConfigFilePath"/> is used.
        /// </param>
        /// <exception cref="InvalidDataException">Thrown when the file exists but JSON is invalid or settings are out of range.</exception>
        public static void Load(string? configFilePath = null)
        {
            var settings = new MfrSettings();
            Settings = settings;

            var path = configFilePath.IsBlank() ? DefaultConfigFilePath() : configFilePath;
            if (!File.Exists(path))
            {
                return;
            }

            try
            {
                var json = File.ReadAllText(path);
                using var doc = JsonDocument.Parse(json);
                var camel = JsonNamingPolicy.CamelCase;
                ConfigValueReader.ReadInt(
                    configObject: doc.RootElement,
                    propertyName: camel.ConvertName(nameof(MfrSettings.MaxListFileLineLength)),
                    value: ref settings.MaxListFileLineLength,
                    minInclusive: 1,
                    maxInclusive: 60000);
                ConfigValueReader.ReadInt(
                    configObject: doc.RootElement,
                    propertyName: camel.ConvertName(nameof(MfrSettings.LogMaxSessionFiles)),
                    value: ref settings.LogMaxSessionFiles,
                    minInclusive: 1,
                    maxInclusive: 10000);
                ConfigValueReader.ReadString(
                    configObject: doc.RootElement,
                    propertyName: camel.ConvertName(nameof(MfrSettings.LogFilePrefix)),
                    value: ref settings.LogFilePrefix,
                    maxLengthInclusive: 200);
                ConfigValueReader.ReadString(
                    configObject: doc.RootElement,
                    propertyName: camel.ConvertName(nameof(MfrSettings.LogFileExtension)),
                    value: ref settings.LogFileExtension,
                    maxLengthInclusive: 32);
                ConfigValueReader.ReadString(
                    configObject: doc.RootElement,
                    propertyName: camel.ConvertName(nameof(MfrSettings.LogConsoleOutputTemplate)),
                    value: ref settings.LogConsoleOutputTemplate,
                    maxLengthInclusive: 4096);
                ConfigValueReader.ReadString(
                    configObject: doc.RootElement,
                    propertyName: camel.ConvertName(nameof(MfrSettings.LogFileOutputTemplate)),
                    value: ref settings.LogFileOutputTemplate,
                    maxLengthInclusive: 4096);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Config file '{path}': {ex.Message}", ex);
            }
        }
    }
}
