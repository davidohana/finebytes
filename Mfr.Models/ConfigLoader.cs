using System.Reflection;
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
                _ApplyJsonToSettings(doc.RootElement, settings);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Config file '{path}': {ex.Message}", ex);
            }
        }

        private static void _ApplyJsonToSettings(JsonElement configRoot, MfrSettings settings)
        {
            var camel = JsonNamingPolicy.CamelCase;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            foreach (var field in typeof(MfrSettings).GetFields(flags))
            {
                var intRange = field.GetCustomAttribute<ConfigIntRangeAttribute>();
                var strMax = field.GetCustomAttribute<ConfigStringMaxLengthAttribute>();
                if (intRange is not null && strMax is not null)
                {
                    throw new InvalidOperationException(
                        $"Field '{field.Name}' cannot specify both [{nameof(ConfigIntRangeAttribute)}] and [{nameof(ConfigStringMaxLengthAttribute)}].");
                }

                var jsonName = camel.ConvertName(field.Name);

                if (intRange is not null)
                {
                    if (field.FieldType != typeof(int))
                    {
                        throw new InvalidOperationException(
                            $"Field '{field.Name}' has [{nameof(ConfigIntRangeAttribute)}] but is not int.");
                    }

                    var value = (int)field.GetValue(settings)!;
                    ConfigValueReader.ReadInt(
                        configRoot,
                        jsonName,
                        ref value,
                        minInclusive: intRange.MinInclusive,
                        maxInclusive: intRange.MaxInclusive);
                    field.SetValue(settings, value);
                    continue;
                }

                if (strMax is not null)
                {
                    if (field.FieldType != typeof(string))
                    {
                        throw new InvalidOperationException(
                            $"Field '{field.Name}' has [{nameof(ConfigStringMaxLengthAttribute)}] but is not string.");
                    }

                    var value = (string)field.GetValue(settings)!;
                    ConfigValueReader.ReadString(
                        configRoot,
                        jsonName,
                        ref value,
                        maxLengthInclusive: strMax.MaxLengthInclusive);
                    field.SetValue(settings, value);
                }
            }
        }
    }
}
