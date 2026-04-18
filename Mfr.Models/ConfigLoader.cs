using System.Text.Json;
using Mfr.Utils;

namespace Mfr.Models
{
    /// <summary>
    /// Process-wide settings optionally loaded from a user JSON file (see <see cref="DefaultConfigFilePath"/>).
    /// </summary>
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
        /// Reads list settings from a JSON file when it exists; otherwise leaves <see cref="Settings"/> at defaults.
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
                ConfigValueReader.ReadInt(
                    configObject: doc.RootElement,
                    propertyName: "maxListFileLineLength",
                    value: ref settings.MaxListFileLineLength,
                    minInclusive: 1,
                    maxInclusive: 60000);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Config file '{path}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Restores defaults (primarily for tests that call <see cref="Load"/>).
        /// </summary>
        internal static void ResetToDefaultsForTests()
        {
            _ResetToDefaults();
        }

        private static void _ResetToDefaults()
        {
            Settings = new MfrSettings();
        }
    }
}
