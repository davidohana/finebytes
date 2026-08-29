using System.Globalization;
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
            _WriteInto(root, configObject, ConfigFieldBindings.ResolveNaming(jsonPropertyNamingPolicy));
            return root;
        }

        /// <summary>
        /// Writes annotated public instance fields of <paramref name="configObject"/> into <paramref name="root"/>.
        /// </summary>
        /// <param name="root">JSON object receiving the fields.</param>
        /// <param name="configObject">Instance whose fields are written.</param>
        /// <param name="naming">JSON property naming policy.</param>
        private static void _WriteInto(JsonObject root, object configObject, JsonNamingPolicy naming)
        {
            foreach (var binding in ConfigFieldBindings.Enumerate(configObject.GetType(), naming))
            {
                _WriteBinding(root, configObject, binding, naming);
            }
        }

        /// <summary>
        /// Writes one classified field from <paramref name="configObject"/> into <paramref name="root"/>.
        /// </summary>
        private static void _WriteBinding(
            JsonObject root,
            object configObject,
            ConfigFieldBinding binding,
            JsonNamingPolicy naming
        )
        {
            switch (binding.Kind)
            {
                case ConfigFieldKind.Section:
                    _WriteSection(root, configObject, binding, naming);
                    return;
                case ConfigFieldKind.Int:
                    _WriteIntLeaf(root, configObject, binding);
                    return;
                case ConfigFieldKind.String:
                    _WriteStringLeaf(root, configObject, binding);
                    return;
                case ConfigFieldKind.Bool:
                    _WriteBoolLeaf(root, configObject, binding);
                    return;
                case ConfigFieldKind.Enum:
                    _WriteEnumLeaf(root, configObject, binding, naming);
                    return;
                default:
                    throw new InvalidOperationException($"Unhandled config field kind '{binding.Kind}'.");
            }
        }

        /// <summary>
        /// Writes a nested JSON object for a section field.
        /// </summary>
        private static void _WriteSection(
            JsonObject root,
            object configObject,
            ConfigFieldBinding binding,
            JsonNamingPolicy naming
        )
        {
            var nested = binding.Field.GetValue(configObject);
            if (nested is null)
            {
                return;
            }

            JsonObject sectionObject = [];
            root[binding.JsonName] = sectionObject;
            _WriteInto(sectionObject, nested, naming);
        }

        /// <summary>
        /// Writes an integer leaf as a JSON string.
        /// </summary>
        private static void _WriteIntLeaf(JsonObject root, object configObject, ConfigFieldBinding binding)
        {
            var value = (int)binding.Field.GetValue(configObject)!;
            var intRange = binding.IntRange!;
            if (value < intRange.MinInclusive || value > intRange.MaxInclusive)
            {
                throw new InvalidOperationException(
                    $"Field '{binding.Field.Name}' value {value} is outside [{intRange.MinInclusive}, {intRange.MaxInclusive}]."
                );
            }

            root[binding.JsonName] = value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Writes a string leaf as a JSON string (including empty).
        /// </summary>
        private static void _WriteStringLeaf(JsonObject root, object configObject, ConfigFieldBinding binding)
        {
            var value = (string?)binding.Field.GetValue(configObject) ?? string.Empty;
            root[binding.JsonName] = value;
        }

        /// <summary>
        /// Writes an unannotated <c>bool</c> leaf as a JSON string.
        /// </summary>
        private static void _WriteBoolLeaf(JsonObject root, object configObject, ConfigFieldBinding binding)
        {
            var value = (bool)binding.Field.GetValue(configObject)!;
            root[binding.JsonName] = value ? "true" : "false";
        }

        /// <summary>
        /// Writes an unannotated enum leaf as a JSON string.
        /// </summary>
        private static void _WriteEnumLeaf(
            JsonObject root,
            object configObject,
            ConfigFieldBinding binding,
            JsonNamingPolicy naming
        )
        {
            var value = binding.Field.GetValue(configObject)!;
            root[binding.JsonName] = naming.ConvertName(value.ToString()!);
        }
    }
}
