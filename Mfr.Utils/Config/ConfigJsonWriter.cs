using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Mfr.Utils.Config
{
    /// <summary>
    /// Writes annotated config fields into a JSON object using the same string-leaf schema as
    /// <see cref="ConfigJsonApplier"/>.
    /// </summary>
    public static class ConfigJsonWriter
    {
        private static readonly JsonNamingPolicy s_DefaultNaming = JsonNamingPolicy.CamelCase;

        /// <summary>
        /// Merges <paramref name="configObject"/> into <paramref name="root"/>, updating known section and leaf
        /// properties while preserving unrelated keys already present in <paramref name="root"/>.
        /// </summary>
        /// <param name="root">Existing config document root or a new object.</param>
        /// <param name="configObject">Annotated config instance to serialize.</param>
        /// <param name="jsonPropertyNamingPolicy">
        /// Converts CLR field names to JSON property names. When <c>null</c>, <see cref="JsonNamingPolicy.CamelCase"/> is used.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="root"/> or <paramref name="configObject"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// A field has incompatible attributes or types, or more than one leaf attribute.
        /// </exception>
        public static void MergeInto(
            JsonObject root,
            object configObject,
            JsonNamingPolicy? jsonPropertyNamingPolicy = null
        )
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(configObject);

            var naming = jsonPropertyNamingPolicy ?? s_DefaultNaming;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            foreach (var field in configObject.GetType().GetFields(flags))
            {
                _MergeField(root, configObject, field, naming, jsonPropertyNamingPolicy);
            }
        }

        /// <summary>
        /// Classifies <paramref name="field"/> and merges a matching section or leaf value.
        /// </summary>
        private static void _MergeField(
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

                _MergeSection(root, configObject, field, sectionAttr, naming, jsonPropertyNamingPolicy);
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
                _MergeIntLeaf(root, configObject, field, jsonName, intRange);
                return;
            }

            if (strMax is not null)
            {
                _MergeStringLeaf(root, configObject, field, jsonName);
                return;
            }

            if (field.FieldType == typeof(bool))
            {
                _MergeBoolLeaf(root, configObject, field, jsonName);
                return;
            }

            if (field.FieldType.IsEnum)
            {
                _MergeEnumLeaf(root, configObject, field, jsonName, naming);
            }
        }

        /// <summary>
        /// Recurses into a nested JSON object for a <see cref="ConfigSectionAttribute"/> field.
        /// </summary>
        private static void _MergeSection(
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

            JsonObject sectionObject;
            if (root[sectionKey] is JsonObject existingSection)
            {
                sectionObject = existingSection;
            }
            else
            {
                sectionObject = [];
                root[sectionKey] = sectionObject;
            }

            MergeInto(sectionObject, nested, jsonPropertyNamingPolicy);
        }

        /// <summary>
        /// Writes an integer leaf as a JSON string.
        /// </summary>
        private static void _MergeIntLeaf(
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
        /// Writes a string leaf as a JSON string.
        /// </summary>
        private static void _MergeStringLeaf(JsonObject root, object configObject, FieldInfo field, string jsonName)
        {
            if (field.FieldType != typeof(string))
            {
                throw new InvalidOperationException(
                    $"Field '{field.Name}' has [{nameof(ConfigStringMaxLengthAttribute)}] but is not string."
                );
            }

            var value = (string)field.GetValue(configObject)!;
            if (value.IsBlank())
            {
                return;
            }

            root[jsonName] = value;
        }

        /// <summary>
        /// Writes an unannotated <c>bool</c> leaf as a JSON string.
        /// </summary>
        private static void _MergeBoolLeaf(JsonObject root, object configObject, FieldInfo field, string jsonName)
        {
            var value = (bool)field.GetValue(configObject)!;
            root[jsonName] = value ? "true" : "false";
        }

        /// <summary>
        /// Writes an unannotated enum leaf as a JSON string.
        /// </summary>
        private static void _MergeEnumLeaf(
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
