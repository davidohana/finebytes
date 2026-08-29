using System.Reflection;
using System.Text.Json;

namespace Mfr.Utils.Config
{
    /// <summary>
    /// Classifies annotated public instance fields for config JSON apply, write, and CLI path lookup.
    /// </summary>
    internal static class ConfigFieldBindings
    {
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        /// <summary>
        /// Returns <paramref name="jsonPropertyNamingPolicy"/>, or camelCase when it is null.
        /// </summary>
        /// <param name="jsonPropertyNamingPolicy">Optional naming policy from a public API.</param>
        /// <returns>The policy used to convert CLR field names to JSON names.</returns>
        internal static JsonNamingPolicy ResolveNaming(JsonNamingPolicy? jsonPropertyNamingPolicy)
        {
            return jsonPropertyNamingPolicy ?? JsonNamingPolicy.CamelCase;
        }

        /// <summary>
        /// Enumerates mapped fields on <paramref name="type"/> in declaration order.
        /// </summary>
        /// <param name="type">Config type whose public instance fields are classified.</param>
        /// <param name="naming">JSON property naming policy.</param>
        /// <returns>Mapped field bindings; unannotated non-bool/non-enum fields are omitted.</returns>
        /// <exception cref="InvalidOperationException">A field has incompatible attributes or types.</exception>
        internal static IEnumerable<ConfigFieldBinding> Enumerate(Type type, JsonNamingPolicy naming)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(naming);

            foreach (var field in type.GetFields(Flags))
            {
                if (_TryCreate(field, naming, out var binding))
                {
                    yield return binding;
                }
            }
        }

        /// <summary>
        /// Finds a section field whose JSON name matches <paramref name="jsonName"/>.
        /// </summary>
        /// <param name="containerType">Type that owns the section field.</param>
        /// <param name="naming">JSON property naming policy.</param>
        /// <param name="jsonName">JSON object property name (case-insensitive).</param>
        /// <returns>The matching section binding, or <see langword="null"/> when none matches.</returns>
        internal static ConfigFieldBinding? FindSection(Type containerType, JsonNamingPolicy naming, string jsonName)
        {
            return _Find(containerType, naming, jsonName, sectionsOnly: true);
        }

        /// <summary>
        /// Finds a leaf field whose JSON name matches <paramref name="jsonName"/>.
        /// </summary>
        /// <param name="containerType">Type that owns the leaf field.</param>
        /// <param name="naming">JSON property naming policy.</param>
        /// <param name="jsonName">JSON string property name (case-insensitive).</param>
        /// <returns>The matching leaf binding, or <see langword="null"/> when none matches.</returns>
        internal static ConfigFieldBinding? FindLeaf(Type containerType, JsonNamingPolicy naming, string jsonName)
        {
            return _Find(containerType, naming, jsonName, sectionsOnly: false);
        }

        private static ConfigFieldBinding? _Find(
            Type containerType,
            JsonNamingPolicy naming,
            string jsonName,
            bool sectionsOnly
        )
        {
            foreach (var binding in Enumerate(containerType, naming))
            {
                var isSection = binding.Kind == ConfigFieldKind.Section;
                if (isSection != sectionsOnly)
                {
                    continue;
                }

                if (string.Equals(binding.JsonName, jsonName, StringComparison.OrdinalIgnoreCase))
                {
                    return binding;
                }
            }

            return null;
        }

        private static bool _TryCreate(FieldInfo field, JsonNamingPolicy naming, out ConfigFieldBinding binding)
        {
            binding = default;
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

                if (!field.FieldType.IsClass || field.FieldType == typeof(string))
                {
                    throw new InvalidOperationException(
                        $"Field '{field.Name}' has [{nameof(ConfigSectionAttribute)}] but is not a reference class type."
                    );
                }

                var sectionKey = sectionAttr.JsonName;
                if (string.IsNullOrEmpty(sectionKey))
                {
                    sectionKey = naming.ConvertName(field.Name);
                }

                binding = new ConfigFieldBinding(
                    field,
                    ConfigFieldKind.Section,
                    sectionKey,
                    IntRange: null,
                    StringMax: null
                );
                return true;
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
                if (field.FieldType != typeof(int))
                {
                    throw new InvalidOperationException(
                        $"Field '{field.Name}' has [{nameof(ConfigIntRangeAttribute)}] but is not int."
                    );
                }

                binding = new ConfigFieldBinding(field, ConfigFieldKind.Int, jsonName, intRange, StringMax: null);
                return true;
            }

            if (strMax is not null)
            {
                if (field.FieldType != typeof(string))
                {
                    throw new InvalidOperationException(
                        $"Field '{field.Name}' has [{nameof(ConfigStringMaxLengthAttribute)}] but is not string."
                    );
                }

                binding = new ConfigFieldBinding(field, ConfigFieldKind.String, jsonName, IntRange: null, strMax);
                return true;
            }

            if (field.FieldType == typeof(bool))
            {
                binding = new ConfigFieldBinding(
                    field,
                    ConfigFieldKind.Bool,
                    jsonName,
                    IntRange: null,
                    StringMax: null
                );
                return true;
            }

            if (field.FieldType.IsEnum)
            {
                binding = new ConfigFieldBinding(
                    field,
                    ConfigFieldKind.Enum,
                    jsonName,
                    IntRange: null,
                    StringMax: null
                );
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Kind of mapped config field.
    /// </summary>
    internal enum ConfigFieldKind
    {
        /// <summary>Nested object marked with <see cref="ConfigSectionAttribute"/>.</summary>
        Section,

        /// <summary>Integer leaf with <see cref="ConfigIntRangeAttribute"/>.</summary>
        Int,

        /// <summary>String leaf with <see cref="ConfigStringMaxLengthAttribute"/>.</summary>
        String,

        /// <summary>Unannotated <c>bool</c> leaf.</summary>
        Bool,

        /// <summary>Unannotated enum leaf.</summary>
        Enum,
    }

    /// <summary>
    /// One classified public instance field on a config type.
    /// </summary>
    /// <param name="Field">CLR field.</param>
    /// <param name="Kind">Mapped kind.</param>
    /// <param name="JsonName">JSON property name after naming policy (or section <see cref="ConfigSectionAttribute.JsonName"/>).</param>
    /// <param name="IntRange">Integer bounds when <paramref name="Kind"/> is <see cref="ConfigFieldKind.Int"/>.</param>
    /// <param name="StringMax">String length limit when <paramref name="Kind"/> is <see cref="ConfigFieldKind.String"/>.</param>
    internal readonly record struct ConfigFieldBinding(
        FieldInfo Field,
        ConfigFieldKind Kind,
        string JsonName,
        ConfigIntRangeAttribute? IntRange,
        ConfigStringMaxLengthAttribute? StringMax
    );
}
