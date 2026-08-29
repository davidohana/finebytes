using System.Globalization;
using System.Text.Json;

namespace Mfr.Utils.Config
{
    /// <summary>
    /// Reads optional config JSON string properties with validation.
    /// </summary>
    public static class ConfigValueReader
    {
        /// <summary>
        /// Reads an optional integer from a JSON string property.
        /// <para>
        /// When <paramref name="propertyName"/> is missing or JSON null, <paramref name="value"/> is unchanged.
        /// </para>
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
            int maxInclusive
        )
        {
            var raw = _ReadOptionalStringProperty(configObject, propertyName);
            if (raw is null)
            {
                return;
            }

            if (raw.IsBlank())
            {
                throw new InvalidDataException($"'{propertyName}' must be an integer (got '{raw}').");
            }

            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                throw new InvalidDataException($"'{propertyName}' must be an integer (got '{raw}').");
            }

            if (parsed < minInclusive || parsed > maxInclusive)
            {
                throw new InvalidDataException(
                    $"'{propertyName}' must be between {minInclusive} and {maxInclusive} (got {parsed})."
                );
            }

            value = parsed;
        }

        /// <summary>
        /// Reads an optional string property.
        /// <para>
        /// When <paramref name="propertyName"/> is missing or JSON null, <paramref name="value"/> is unchanged.
        /// An empty string is accepted (optional values such as <c>log.directoryPath</c>).
        /// </para>
        /// </summary>
        /// <param name="configObject">A JSON object (typically the document root).</param>
        /// <param name="propertyName">Object property name; matching is case-insensitive.</param>
        /// <param name="value">Field to update when the property is set.</param>
        /// <param name="maxLengthInclusive">When set, the value must not exceed this length.</param>
        /// <exception cref="InvalidDataException">
        /// Thrown when <paramref name="configObject"/> is not an object, the property is not a JSON string or null, the value is whitespace-only, or the length exceeds <paramref name="maxLengthInclusive"/>.
        /// </exception>
        public static void ReadString(
            JsonElement configObject,
            string propertyName,
            ref string value,
            int? maxLengthInclusive = null
        )
        {
            var raw = _ReadOptionalStringProperty(configObject, propertyName);
            if (raw is null)
            {
                return;
            }

            var isWhitespaceOnly = raw.Length > 0 && raw.IsBlank();
            if (isWhitespaceOnly)
            {
                throw new InvalidDataException($"'{propertyName}' must not be whitespace-only (got '{raw}').");
            }

            if (maxLengthInclusive is { } maxLen && raw.Length > maxLen)
            {
                throw new InvalidDataException(
                    $"'{propertyName}' must be at most {maxLen} characters (got {raw.Length})."
                );
            }

            value = raw;
        }

        /// <summary>
        /// Reads an optional boolean from a JSON string property.
        /// <para>
        /// When <paramref name="propertyName"/> is missing or JSON null, <paramref name="value"/> is unchanged.
        /// Accepted text is case-insensitive <c>true</c> / <c>false</c>.
        /// </para>
        /// </summary>
        /// <param name="configObject">A JSON object (typically the document root).</param>
        /// <param name="propertyName">Object property name; matching is case-insensitive.</param>
        /// <param name="value">Field to update when the property is set.</param>
        /// <exception cref="InvalidDataException">
        /// Thrown when <paramref name="configObject"/> is not an object, the property is not a JSON string or null, or the text is not a boolean.
        /// </exception>
        public static void ReadBool(JsonElement configObject, string propertyName, ref bool value)
        {
            var raw = _ReadOptionalStringProperty(configObject, propertyName);
            if (raw is null)
            {
                return;
            }

            if (bool.TryParse(raw, out var parsed))
            {
                value = parsed;
                return;
            }

            throw new InvalidDataException($"'{propertyName}' must be a boolean (got '{raw}').");
        }

        /// <summary>
        /// Reads an optional enum from a JSON string property.
        /// <para>
        /// When <paramref name="propertyName"/> is missing or JSON null, <paramref name="value"/> is unchanged.
        /// Accepted text is a defined enum member name (case-insensitive).
        /// </para>
        /// </summary>
        /// <param name="configObject">A JSON object (typically the document root).</param>
        /// <param name="propertyName">Object property name; matching is case-insensitive.</param>
        /// <param name="enumType">Non-flags enum type to parse into.</param>
        /// <param name="value">Boxed enum field to update when the property is set.</param>
        /// <exception cref="ArgumentException"><paramref name="enumType"/> is not an enum.</exception>
        /// <exception cref="InvalidDataException">
        /// Thrown when <paramref name="configObject"/> is not an object, the property is not a JSON string or null, or the text is not a defined member name.
        /// </exception>
        public static void ReadEnum(JsonElement configObject, string propertyName, Type enumType, ref object value)
        {
            ArgumentNullException.ThrowIfNull(enumType);
            if (!enumType.IsEnum)
            {
                throw new ArgumentException($"Type '{enumType.FullName}' is not an enum.", nameof(enumType));
            }

            var raw = _ReadOptionalStringProperty(configObject, propertyName);
            if (raw is null)
            {
                return;
            }

            if (raw.IsBlank())
            {
                throw new InvalidDataException($"'{propertyName}' must be an enum member name (got '{raw}').");
            }

            if (!Enum.TryParse(enumType, raw, ignoreCase: true, out var parsed) || parsed is null)
            {
                throw new InvalidDataException($"'{propertyName}' must be an enum member name (got '{raw}').");
            }

            if (!Enum.IsDefined(enumType, parsed))
            {
                throw new InvalidDataException($"'{propertyName}' must be an enum member name (got '{raw}').");
            }

            value = parsed;
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

                throw new InvalidDataException($"'{propertyName}' must be a JSON string or null.");
            }

            return null;
        }
    }
}
