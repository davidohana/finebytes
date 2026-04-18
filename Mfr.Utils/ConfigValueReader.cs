using System.Globalization;
using System.Text.Json;

namespace Mfr.Utils
{
    /// <summary>
    /// Parses optional string fields from config JSON using defaults and constraints.
    /// </summary>
    public static class ConfigValueReader
    {
        /// <summary>
        /// When <paramref name="propertyName"/> is present as a JSON string, parses and assigns <paramref name="value"/>; when missing or JSON null, leaves <paramref name="value"/> unchanged.
        /// </summary>
        /// <param name="configObject">A JSON object (typically the document root).</param>
        /// <param name="propertyName">Object property name; matching is case-insensitive.</param>
        /// <param name="value">Field to update when the property is set.</param>
        /// <param name="minInclusive">Minimum allowed value when a value is present.</param>
        /// <param name="maxInclusive">Maximum allowed value when a value is present.</param>
        /// <exception cref="InvalidDataException">
        /// Thrown when <paramref name="configObject"/> is not an object, the property is not a JSON string or null, or the text fails integer / range checks.
        /// </exception>
        public static void ReadInt(
            JsonElement configObject,
            string propertyName,
            ref int value,
            int minInclusive,
            int maxInclusive)
        {
            var raw = _ReadOptionalStringProperty(configObject, propertyName);
            if (raw is null)
            {
                return;
            }

            if (raw.IsBlank())
            {
                throw new InvalidDataException(
                    $"'{propertyName}' must be an integer (got '{raw}').");
            }

            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                throw new InvalidDataException(
                    $"'{propertyName}' must be an integer (got '{raw}').");
            }

            if (parsed < minInclusive || parsed > maxInclusive)
            {
                throw new InvalidDataException(
                    $"'{propertyName}' must be between {minInclusive} and {maxInclusive} (got {parsed}).");
            }

            value = parsed;
        }

        /// <summary>
        /// When <paramref name="propertyName"/> is present as a JSON string, assigns that string to <paramref name="value"/>; when missing or JSON null, leaves <paramref name="value"/> unchanged.
        /// </summary>
        /// <param name="configObject">A JSON object (typically the document root).</param>
        /// <param name="propertyName">Object property name; matching is case-insensitive.</param>
        /// <param name="value">Field to update when the property is set.</param>
        /// <param name="maxLengthInclusive">When set, the value must not exceed this length.</param>
        /// <exception cref="InvalidDataException">
        /// Thrown when <paramref name="configObject"/> is not an object, the property is not a JSON string or null, the value is blank, or the length exceeds <paramref name="maxLengthInclusive"/>.
        /// </exception>
        public static void ReadString(
            JsonElement configObject,
            string propertyName,
            ref string value,
            int? maxLengthInclusive = null)
        {
            var raw = _ReadOptionalStringProperty(configObject, propertyName);
            if (raw is null)
            {
                return;
            }

            if (raw.IsBlank())
            {
                throw new InvalidDataException(
                    $"'{propertyName}' must be a non-empty string (got '{raw}').");
            }

            if (maxLengthInclusive is { } maxLen && raw.Length > maxLen)
            {
                throw new InvalidDataException(
                    $"'{propertyName}' must be at most {maxLen} characters (got {raw.Length}).");
            }

            value = raw;
        }

        /// <summary>
        /// Reads a case-insensitive object property whose value must be a JSON string or null.
        /// </summary>
        /// <param name="root">A JSON object (config root or nested object).</param>
        /// <param name="propertyName">Property name to find.</param>
        /// <returns>
        /// The string value when the property exists and is a JSON string; <c>null</c> when the property is missing, or exists with JSON null.
        /// </returns>
        /// <exception cref="InvalidDataException">
        /// Thrown when <paramref name="root"/> is not a JSON object, or the property exists but is neither a string nor null.
        /// </exception>
        private static string? _ReadOptionalStringProperty(JsonElement root, string propertyName)
        {
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException("Root must be a JSON object.");
            }

            foreach (var prop in root.EnumerateObject())
            {
                if (!string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var kind = prop.Value.ValueKind;
                if (kind == JsonValueKind.String)
                {
                    return prop.Value.GetString();
                }

                if (kind == JsonValueKind.Null)
                {
                    return null;
                }

                throw new InvalidDataException(
                    $"'{propertyName}' must be a JSON string or null.");
            }

            return null;
        }
    }
}
