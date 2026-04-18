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
    /// The document root must be a JSON object with nested sections (e.g. <c>filters</c>, <c>log</c>). Each section is a JSON object;
    /// <see cref="ConfigApplier.Apply"/> maps annotated fields on <see cref="MfrSettings"/> and nested section types using
    /// <see cref="ConfigValueReader"/>; every leaf value is read from a JSON <strong>string</strong> (including integers, e.g. <c>"1000"</c>).
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
                ConfigApplier.Apply(doc.RootElement, settings);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Config file '{path}': {ex.Message}", ex);
            }
        }
    }
}
