using System.Reflection;
using System.Text.Json;

namespace Mfr.Utils.Config
{
    /// <summary>
    /// Applies JSON config values to public instance fields using <see cref="ConfigIntRangeAttribute"/> and
    /// <see cref="ConfigStringMaxLengthAttribute"/> together with <see cref="ConfigValueReader"/>, and nested objects
    /// marked with <see cref="ConfigSectionAttribute"/>.
    /// </summary>
    public static class ConfigApplier
    {
        /// <summary>
        /// For each public instance field declared on the runtime type of <paramref name="target"/>:
        /// fields with <see cref="ConfigSectionAttribute"/> load a nested JSON object and apply recursively;
        /// fields with <see cref="ConfigIntRangeAttribute"/> or <see cref="ConfigStringMaxLengthAttribute"/> read the matching
        /// JSON object property (values must be JSON strings, including integer settings) and update the field.
        /// Omitted properties and JSON null leave the current field value unchanged.
        /// </summary>
        /// <param name="configObject">A JSON object (typically the document root).</param>
        /// <param name="target">The object whose annotated fields are updated.</param>
        /// <param name="jsonPropertyNamingPolicy">
        /// Converts CLR field names to JSON property names. When <c>null</c>, <see cref="JsonNamingPolicy.CamelCase"/> is used.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="target"/> is null.</exception>
        /// <exception cref="InvalidDataException">JSON or values are invalid (see <see cref="ConfigValueReader"/>).</exception>
        /// <exception cref="InvalidOperationException">
        /// A field has incompatible attributes or types, or both range and string-length attributes.
        /// </exception>
        public static void Apply(
            JsonElement configObject,
            object target,
            JsonNamingPolicy? jsonPropertyNamingPolicy = null)
        {
            ArgumentNullException.ThrowIfNull(target);

            var naming = jsonPropertyNamingPolicy ?? JsonNamingPolicy.CamelCase;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            foreach (var field in target.GetType().GetFields(flags))
            {
                var sectionAttr = field.GetCustomAttribute<ConfigSectionAttribute>();
                var intRange = field.GetCustomAttribute<ConfigIntRangeAttribute>();
                var strMax = field.GetCustomAttribute<ConfigStringMaxLengthAttribute>();

                if (sectionAttr is not null)
                {
                    if (intRange is not null || strMax is not null)
                    {
                        throw new InvalidOperationException(
                            $"Field '{field.Name}' cannot combine [{nameof(ConfigSectionAttribute)}] with [{nameof(ConfigIntRangeAttribute)}] or [{nameof(ConfigStringMaxLengthAttribute)}].");
                    }

                    if (!field.FieldType.IsClass || field.FieldType == typeof(string))
                    {
                        throw new InvalidOperationException(
                            $"Field '{field.Name}' has [{nameof(ConfigSectionAttribute)}] but is not a reference class type.");
                    }

                    var nested = field.GetValue(target);
                    if (nested is null)
                    {
                        continue;
                    }

                    var sectionKey = sectionAttr.JsonName;
                    if (string.IsNullOrEmpty(sectionKey))
                    {
                        sectionKey = naming.ConvertName(field.Name);
                    }

                    if (!_TryGetObjectProperty(configObject, sectionKey, out var nestedObject))
                    {
                        continue;
                    }

                    Apply(nestedObject, nested, jsonPropertyNamingPolicy);
                    continue;
                }

                if (intRange is not null && strMax is not null)
                {
                    throw new InvalidOperationException(
                        $"Field '{field.Name}' cannot specify both [{nameof(ConfigIntRangeAttribute)}] and [{nameof(ConfigStringMaxLengthAttribute)}].");
                }

                var jsonName = naming.ConvertName(field.Name);

                if (intRange is not null)
                {
                    if (field.FieldType != typeof(int))
                    {
                        throw new InvalidOperationException(
                            $"Field '{field.Name}' has [{nameof(ConfigIntRangeAttribute)}] but is not int.");
                    }

                    var value = (int)field.GetValue(target)!;
                    ConfigValueReader.ReadInt(
                        configObject,
                        jsonName,
                        ref value,
                        minInclusive: intRange.MinInclusive,
                        maxInclusive: intRange.MaxInclusive);
                    field.SetValue(target, value);
                    continue;
                }

                if (strMax is not null)
                {
                    if (field.FieldType != typeof(string))
                    {
                        throw new InvalidOperationException(
                            $"Field '{field.Name}' has [{nameof(ConfigStringMaxLengthAttribute)}] but is not string.");
                    }

                    var value = (string)field.GetValue(target)!;
                    ConfigValueReader.ReadString(
                        configObject,
                        jsonName,
                        ref value,
                        maxLengthInclusive: strMax.MaxLengthInclusive);
                    field.SetValue(target, value);
                }
            }
        }

        /// <summary>
        /// When <paramref name="propertyName"/> matches a property on <paramref name="root"/>, returns true and sets
        /// <paramref name="value"/> to that property's element. Missing properties and JSON null return false.
        /// When the property exists but is not an object or null, throws <see cref="InvalidDataException"/>.
        /// </summary>
        private static bool _TryGetObjectProperty(JsonElement root, string propertyName, out JsonElement value)
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
                if (kind == JsonValueKind.Null)
                {
                    value = default;
                    return false;
                }

                if (kind != JsonValueKind.Object)
                {
                    throw new InvalidDataException(
                        $"'{propertyName}' must be a JSON object or null.");
                }

                value = prop.Value;
                return true;
            }

            value = default;
            return false;
        }
    }
}
