using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Mfr.Utils.Config
{
    /// <summary>
    /// Writes annotated config fields into a new JSON object using the same string-leaf schema as
    /// <see cref="ConfigJsonApplier"/>.
    /// </summary>
    public static class ConfigJsonWriter
    {
        private static readonly JsonNamingPolicy s_DefaultNaming = JsonNamingPolicy.CamelCase;

        /// <summary>
        /// Serializes <paramref name="configObject"/> to a new JSON object.
        /// </summary>
        /// <param name="configObject">Annotated config instance to serialize.</param>
        /// <param name="jsonPropertyNamingPolicy">
        /// Converts CLR field names to JSON property names. When <c>null</c>, <see cref="JsonNamingPolicy.CamelCase"/> is used.
        /// </param>
        /// <returns>A JSON object with nested sections and string-leaf values.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="configObject"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// A field has incompatible attributes or types, or more than one leaf attribute.
        /// </exception>
        public static JsonObject Write(object configObject, JsonNamingPolicy? jsonPropertyNamingPolicy = null)
        {
            ArgumentNullException.ThrowIfNull(configObject);

            JsonObject root = [];
            _WriteInto(root, configObject, jsonPropertyNamingPolicy);
            return root;
        }

        /// <summary>
        /// Writes annotated public instance fields of <paramref name="configObject"/> into <paramref name="root"/>.
        /// </summary>
        /// <param name="root">JSON object receiving the fields.</param>
        /// <param name="configObject">Instance whose fields are written.</param>
        /// <param name="jsonPropertyNamingPolicy">Optional naming policy forwarded to nested sections.</param>
        private static void _WriteInto(JsonObject root, object configObject, JsonNamingPolicy? jsonPropertyNamingPolicy)
        {
            var naming = jsonPropertyNamingPolicy ?? s_DefaultNaming;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            foreach (var field in configObject.GetType().GetFields(flags))
            {
                _WriteField(root, configObject, field, naming, jsonPropertyNamingPolicy);
            }
        }

        /// <summary>
        /// Classifies <paramref name="field"/> and writes a matching section or leaf value.
        /// </summary>
        /// <param name="root">JSON object receiving the field.</param>
        /// <param name="configObject">Instance that owns <paramref name="field"/>.</param>
        /// <param name="field">Public instance field to write.</param>
        /// <param name="naming">JSON property naming policy.</param>
        /// <param name="jsonPropertyNamingPolicy">Optional naming policy forwarded to nested sections.</param>
        private static void _WriteField(
            JsonObject root,
            object configObject,
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

                _WriteSection(root, configObject, field, sectionAttr, naming, jsonPropertyNamingPolicy);
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
                _WriteIntLeaf(root, configObject, field, jsonName, intRange);
                return;
            }

            if (strMax is not null)
            {
                _WriteStringLeaf(root, configObject, field, jsonName);
                return;
            }

            if (field.FieldType == typeof(bool))
            {
                _WriteBoolLeaf(root, configObject, field, jsonName);
                return;
            }

            if (field.FieldType.IsEnum)
            {
                _WriteEnumLeaf(root, configObject, field, jsonName, naming);
            }
        }

        /// <summary>
        /// Writes a nested JSON object for a <see cref="ConfigSectionAttribute"/> field.
        /// </summary>
        private static void _WriteSection(
            JsonObject root,
            object configObject,
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

            var nested = field.GetValue(configObject);
            if (nested is null)
            {
                return;
            }

            var sectionKey = sectionAttr.JsonName;
            if (string.IsNullOrEmpty(sectionKey))
            {
                sectionKey = naming.ConvertName(field.Name);
            }

            JsonObject sectionObject = [];
            root[sectionKey] = sectionObject;
            _WriteInto(sectionObject, nested, jsonPropertyNamingPolicy);
        }

        /// <summary>
        /// Writes an integer leaf as a JSON string.
        /// </summary>
        private static void _WriteIntLeaf(
            JsonObject root,
            object configObject,
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

            var value = (int)field.GetValue(configObject)!;
            if (value < intRange.MinInclusive || value > intRange.MaxInclusive)
            {
                throw new InvalidOperationException(
                    $"Field '{field.Name}' value {value} is outside [{intRange.MinInclusive}, {intRange.MaxInclusive}]."
                );
            }

            root[jsonName] = value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Writes a string leaf as a JSON string (including empty).
        /// </summary>
        private static void _WriteStringLeaf(JsonObject root, object configObject, FieldInfo field, string jsonName)
        {
            if (field.FieldType != typeof(string))
            {
                throw new InvalidOperationException(
                    $"Field '{field.Name}' has [{nameof(ConfigStringMaxLengthAttribute)}] but is not string."
                );
            }

            var value = (string?)field.GetValue(configObject) ?? string.Empty;
            root[jsonName] = value;
        }

        /// <summary>
        /// Writes an unannotated <c>bool</c> leaf as a JSON string.
        /// </summary>
        private static void _WriteBoolLeaf(JsonObject root, object configObject, FieldInfo field, string jsonName)
        {
            var value = (bool)field.GetValue(configObject)!;
            root[jsonName] = value ? "true" : "false";
        }

        /// <summary>
        /// Writes an unannotated enum leaf as a JSON string.
        /// </summary>
        private static void _WriteEnumLeaf(
            JsonObject root,
            object configObject,
            FieldInfo field,
            string jsonName,
            JsonNamingPolicy naming
        )
        {
            var value = field.GetValue(configObject)!;
            root[jsonName] = naming.ConvertName(value.ToString()!);
        }
    }
}
