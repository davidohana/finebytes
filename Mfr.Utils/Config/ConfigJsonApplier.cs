using System.Text.Json;

namespace Mfr.Utils.Config
{
    /// <summary>
    /// Applies JSON configuration to annotated public instance fields.
    /// <para>
    /// Uses <see cref="ConfigSectionAttribute"/> for nested objects,
    /// <see cref="ConfigIntRangeAttribute"/> / <see cref="ConfigStringMaxLengthAttribute"/> for constrained leaves,
    /// and unannotated <c>bool</c> / enum fields as leaves (via <see cref="ConfigValueReader"/>; leaf values are JSON strings,
    /// including integers, booleans, and enum member names).
    /// </para>
    /// </summary>
    public static class ConfigJsonApplier
    {
        /// <summary>
        /// Binds <paramref name="configObject"/> onto <paramref name="target"/>.
        /// <para>
        /// <see cref="ConfigSectionAttribute"/> fields recurse into nested JSON objects. Leaf attributes and public
        /// <c>bool</c> / enum fields read matching properties as JSON strings. Omitted properties and JSON null leave fields unchanged.
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

            var naming = ConfigFieldBindings.ResolveNaming(jsonPropertyNamingPolicy);
            foreach (var binding in ConfigFieldBindings.Enumerate(target.GetType(), naming))
            {
                _ApplyBinding(configObject, target, binding, naming);
            }
        }

        /// <summary>
        /// Applies one classified field from <paramref name="configObject"/> onto <paramref name="target"/>.
        /// </summary>
        private static void _ApplyBinding(
            JsonElement configObject,
            object target,
            ConfigFieldBinding binding,
            JsonNamingPolicy naming
        )
        {
            switch (binding.Kind)
            {
                case ConfigFieldKind.Section:
                    _ApplySection(configObject, target, binding, naming);
                    return;
                case ConfigFieldKind.Int:
                    _ApplyIntLeaf(configObject, target, binding);
                    return;
                case ConfigFieldKind.String:
                    _ApplyStringLeaf(configObject, target, binding);
                    return;
                case ConfigFieldKind.Bool:
                    _ApplyBoolLeaf(configObject, target, binding);
                    return;
                case ConfigFieldKind.Enum:
                    _ApplyEnumLeaf(configObject, target, binding);
                    return;
                default:
                    throw new InvalidOperationException($"Unhandled config field kind '{binding.Kind}'.");
            }
        }

        /// <summary>
        /// Recurses into a nested JSON object for a section field.
        /// </summary>
        private static void _ApplySection(
            JsonElement configObject,
            object target,
            ConfigFieldBinding binding,
            JsonNamingPolicy naming
        )
        {
            var nested = binding.Field.GetValue(target);
            if (nested is null)
            {
                return;
            }

            if (!_TryGetObjectProperty(configObject, binding.JsonName, out var nestedObject))
            {
                return;
            }

            Apply(nestedObject, nested, naming);
        }

        /// <summary>
        /// Reads and assigns an integer leaf using <see cref="ConfigIntRangeAttribute"/> bounds.
        /// </summary>
        private static void _ApplyIntLeaf(JsonElement configObject, object target, ConfigFieldBinding binding)
        {
            var value = (int)binding.Field.GetValue(target)!;
            var intRange = binding.IntRange!;
            ConfigValueReader.ReadInt(
                configObject,
                binding.JsonName,
                ref value,
                minInclusive: intRange.MinInclusive,
                maxInclusive: intRange.MaxInclusive
            );
            binding.Field.SetValue(target, value);
        }

        /// <summary>
        /// Reads and assigns a string leaf using <see cref="ConfigStringMaxLengthAttribute"/> limits.
        /// </summary>
        private static void _ApplyStringLeaf(JsonElement configObject, object target, ConfigFieldBinding binding)
        {
            var value = (string)binding.Field.GetValue(target)!;
            ConfigValueReader.ReadString(
                configObject,
                binding.JsonName,
                ref value,
                maxLengthInclusive: binding.StringMax!.MaxLengthInclusive
            );
            binding.Field.SetValue(target, value);
        }

        /// <summary>
        /// Reads and assigns an unannotated <c>bool</c> leaf.
        /// </summary>
        private static void _ApplyBoolLeaf(JsonElement configObject, object target, ConfigFieldBinding binding)
        {
            var value = (bool)binding.Field.GetValue(target)!;
            ConfigValueReader.ReadBool(configObject, binding.JsonName, ref value);
            binding.Field.SetValue(target, value);
        }

        /// <summary>
        /// Reads and assigns an unannotated enum leaf.
        /// </summary>
        private static void _ApplyEnumLeaf(JsonElement configObject, object target, ConfigFieldBinding binding)
        {
            var value = binding.Field.GetValue(target)!;
            ConfigValueReader.ReadEnum(configObject, binding.JsonName, binding.Field.FieldType, ref value);
            binding.Field.SetValue(target, value);
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
