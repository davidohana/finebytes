using System.Reflection;
using System.Text.Json;

namespace Mfr.Utils.Config
{
    /// <summary>
    /// Applies JSON configuration to annotated public instance fields.
    /// <para>
    /// Uses <see cref="ConfigSectionAttribute"/> for nested objects,
    /// <see cref="ConfigIntRangeAttribute"/> / <see cref="ConfigStringMaxLengthAttribute"/> for constrained leaves,
    /// and unannotated <c>bool</c> fields as boolean leaves (via <see cref="ConfigValueReader"/>; leaf values are JSON strings,
    /// including integers and booleans).
    /// </para>
    /// </summary>
    public static class ConfigJsonApplier
    {
        /// <summary>
        /// Binds <paramref name="configObject"/> onto <paramref name="target"/>.
        /// <para>
        /// <see cref="ConfigSectionAttribute"/> fields recurse into nested JSON objects. Leaf attributes and public
        /// <c>bool</c> fields read matching properties as JSON strings. Omitted properties and JSON null leave fields unchanged.
        /// </para>
        /// </summary>
        /// <param name="configObject">A JSON object (typically the document root).</param>
        /// <param name="target">The object whose annotated fields are updated.</param>
        /// <param name="jsonPropertyNamingPolicy">
        /// Converts CLR field names to JSON property names. When <c>null</c>, <see cref="JsonNamingPolicy.CamelCase"/> is used.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="target"/> is null.</exception>
        /// <exception cref="InvalidDataException">JSON or values are invalid (see <see cref="ConfigValueReader"/>).</exception>
        /// <exception cref="InvalidOperationException">
        /// A field has incompatible attributes or types, or more than one leaf attribute.
        /// </exception>
        public static void Apply(
            JsonElement configObject,
            object target,
            JsonNamingPolicy? jsonPropertyNamingPolicy = null
        )
        {
            ArgumentNullException.ThrowIfNull(target);

            var naming = jsonPropertyNamingPolicy ?? JsonNamingPolicy.CamelCase;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            foreach (var field in target.GetType().GetFields(flags))
            {
                _ApplyField(configObject, target, field, naming, jsonPropertyNamingPolicy);
            }
        }

        /// <summary>
        /// Classifies <paramref name="field"/> and applies a matching section or leaf binding.
        /// </summary>
        private static void _ApplyField(
            JsonElement configObject,
            object target,
            FieldInfo field,
            JsonNamingPolicy naming,
            JsonNamingPolicy? jsonPropertyNamingPolicy
        )
        {
            var sectionAttr = field.GetCustomAttribute<ConfigSectionAttribute>();
            var intRange = field.GetCustomAttribute<ConfigIntRangeAttribute>();
            var strMax = field.GetCustomAttribute<ConfigStringMaxLengthAttribute>();

            if (sectionAttr is not null)
            {
                if (intRange is not null || strMax is not null)
                {
                    throw new InvalidOperationException(
                        $"Field '{field.Name}' cannot combine [{nameof(ConfigSectionAttribute)}] with leaf config attributes."
                    );
                }

                _ApplySection(configObject, target, field, sectionAttr, naming, jsonPropertyNamingPolicy);
                return;
            }

            if (intRange is not null && strMax is not null)
            {
                throw new InvalidOperationException(
                    $"Field '{field.Name}' cannot specify both [{nameof(ConfigIntRangeAttribute)}] and [{nameof(ConfigStringMaxLengthAttribute)}]."
                );
            }

            var jsonName = naming.ConvertName(field.Name);

            if (intRange is not null)
            {
                _ApplyIntLeaf(configObject, target, field, jsonName, intRange);
                return;
            }

            if (strMax is not null)
            {
                _ApplyStringLeaf(configObject, target, field, jsonName, strMax);
                return;
            }

            if (field.FieldType == typeof(bool))
            {
                _ApplyBoolLeaf(configObject, target, field, jsonName);
            }
        }

        /// <summary>
        /// Recurses into a nested JSON object for a <see cref="ConfigSectionAttribute"/> field.
        /// </summary>
        private static void _ApplySection(
            JsonElement configObject,
            object target,
            FieldInfo field,
            ConfigSectionAttribute sectionAttr,
            JsonNamingPolicy naming,
            JsonNamingPolicy? jsonPropertyNamingPolicy
        )
        {
            if (!field.FieldType.IsClass || field.FieldType == typeof(string))
            {
                throw new InvalidOperationException(
                    $"Field '{field.Name}' has [{nameof(ConfigSectionAttribute)}] but is not a reference class type."
                );
            }

            var nested = field.GetValue(target);
            if (nested is null)
            {
                return;
            }

            var sectionKey = sectionAttr.JsonName;
            if (string.IsNullOrEmpty(sectionKey))
            {
                sectionKey = naming.ConvertName(field.Name);
            }

            if (!_TryGetObjectProperty(configObject, sectionKey, out var nestedObject))
            {
                return;
            }

            Apply(nestedObject, nested, jsonPropertyNamingPolicy);
        }

        /// <summary>
        /// Reads and assigns an integer leaf using <see cref="ConfigIntRangeAttribute"/> bounds.
        /// </summary>
        private static void _ApplyIntLeaf(
            JsonElement configObject,
            object target,
            FieldInfo field,
            string jsonName,
            ConfigIntRangeAttribute intRange
        )
        {
            if (field.FieldType != typeof(int))
            {
                throw new InvalidOperationException(
                    $"Field '{field.Name}' has [{nameof(ConfigIntRangeAttribute)}] but is not int."
                );
            }

            var value = (int)field.GetValue(target)!;
            ConfigValueReader.ReadInt(
                configObject,
                jsonName,
                ref value,
                minInclusive: intRange.MinInclusive,
                maxInclusive: intRange.MaxInclusive
            );
            field.SetValue(target, value);
        }

        /// <summary>
        /// Reads and assigns a string leaf using <see cref="ConfigStringMaxLengthAttribute"/> limits.
        /// </summary>
        private static void _ApplyStringLeaf(
            JsonElement configObject,
            object target,
            FieldInfo field,
            string jsonName,
            ConfigStringMaxLengthAttribute strMax
        )
        {
            if (field.FieldType != typeof(string))
            {
                throw new InvalidOperationException(
                    $"Field '{field.Name}' has [{nameof(ConfigStringMaxLengthAttribute)}] but is not string."
                );
            }

            var value = (string)field.GetValue(target)!;
            ConfigValueReader.ReadString(
                configObject,
                jsonName,
                ref value,
                maxLengthInclusive: strMax.MaxLengthInclusive
            );
            field.SetValue(target, value);
        }

        /// <summary>
        /// Reads and assigns an unannotated <c>bool</c> leaf.
        /// </summary>
        private static void _ApplyBoolLeaf(JsonElement configObject, object target, FieldInfo field, string jsonName)
        {
            var value = (bool)field.GetValue(target)!;
            ConfigValueReader.ReadBool(configObject, jsonName, ref value);
            field.SetValue(target, value);
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
                    throw new InvalidDataException($"'{propertyName}' must be a JSON object or null.");
                }

                value = prop.Value;
                return true;
            }

            value = default;
            return false;
        }
    }
}
