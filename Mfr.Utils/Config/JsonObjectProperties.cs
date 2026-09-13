using System.Text.Json;

namespace Mfr.Utils.Config
{
    /// <summary>
    /// Case-insensitive property lookup on JSON objects (config soft-load / apply).
    /// </summary>
    public static class JsonObjectProperties
    {
        /// <summary>
        /// Finds a property on <paramref name="root"/> by case-insensitive name.
        /// </summary>
        /// <param name="root">JSON value; non-objects yield <see langword="false"/>.</param>
        /// <param name="propertyName">Property name to match.</param>
        /// <param name="value">Matched element when found (any <see cref="JsonValueKind"/>).</param>
        /// <returns><see langword="true"/> when the property exists.</returns>
        public static bool TryGetPropertyIgnoreCase(JsonElement root, string propertyName, out JsonElement value)
        {
            if (root.ValueKind != JsonValueKind.Object)
            {
                value = default;
                return false;
            }

            foreach (var prop in root.EnumerateObject())
            {
                if (!string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                value = prop.Value;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Finds a property whose value must be a JSON object (or missing / JSON null).
        /// </summary>
        /// <param name="root">A JSON object (config root or nested object).</param>
        /// <param name="propertyName">Property name to match.</param>
        /// <param name="value">Object element when found and non-null.</param>
        /// <returns>
        /// <see langword="true"/> when the property exists and is an object; <see langword="false"/> when
        /// missing or JSON null.
        /// </returns>
        /// <exception cref="InvalidDataException">
        /// Thrown when <paramref name="root"/> is not an object, or the property exists but is neither an
        /// object nor null.
        /// </exception>
        public static bool TryGetObjectProperty(JsonElement root, string propertyName, out JsonElement value)
        {
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException("Root must be a JSON object.");
            }

            if (!TryGetPropertyIgnoreCase(root, propertyName, out var found))
            {
                value = default;
                return false;
            }

            var kind = found.ValueKind;
            if (kind == JsonValueKind.Null)
            {
                value = default;
                return false;
            }

            if (kind != JsonValueKind.Object)
            {
                throw new InvalidDataException($"'{propertyName}' must be a JSON object or null.");
            }

            value = found;
            return true;
        }
    }
}
