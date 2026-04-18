using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Mfr.Utils.Config
{
    /// <summary>
    /// Applies CLI <c>--set</c> assignments to annotated settings.
    /// <para>
    /// Builds merged JSON from <c>section.leaf=value</c> keys, validates paths, then uses <see cref="ConfigJsonApplier"/>.
    /// </para>
    /// </summary>
    public static class ConfigOverridesApplier
    {
        private static readonly JsonNamingPolicy s_Naming = JsonNamingPolicy.CamelCase;

        /// <summary>
        /// Applies parsed <c>--set</c> strings to <paramref name="settings"/>.
        /// <para>
        /// Validates dotted paths against <typeparamref name="TSettings"/>, merges JSON, then <see cref="ConfigJsonApplier.Apply"/>.
        /// </para>
        /// </summary>
        /// <typeparam name="TSettings">Root settings type with <see cref="ConfigSectionAttribute"/> sections and leaf attributes.</typeparam>
        /// <param name="assignments">Raw <c>key=value</c> strings; blank entries are skipped.</param>
        /// <param name="settings">The object to update.</param>
        /// <exception cref="InvalidDataException">Assignment format, path, or value is invalid.</exception>
        public static void Apply<TSettings>(IReadOnlyList<string> assignments, TSettings settings)
            where TSettings : class
        {
            ArgumentNullException.ThrowIfNull(assignments);
            ArgumentNullException.ThrowIfNull(settings);

            var merged = new JsonObject();
            foreach (var raw in assignments)
            {
                if (raw.IsBlank())
                {
                    continue;
                }

                var trimmed = raw.Trim();
                var equalsIndex = trimmed.IndexOf('=');
                if (equalsIndex <= 0)
                {
                    throw new InvalidDataException(
                        $"Invalid --set argument (expected key=value): '{raw}'.");
                }

                var dottedKey = trimmed[..equalsIndex].Trim();
                var value = trimmed[(equalsIndex + 1)..].Trim();
                if (dottedKey.IsBlank())
                {
                    throw new InvalidDataException(
                        $"Invalid --set argument (missing key before '='): '{raw}'.");
                }

                var segments = dottedKey
                    .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (segments.Length < 2)
                {
                    throw new InvalidDataException(
                        $"Config path must include a section and a field (e.g. log.maxSessionFiles); got '{dottedKey}'.");
                }

                _MergeValidated(
                    merged,
                    typeof(TSettings),
                    segments,
                    value);
            }

            if (merged.Count == 0)
            {
                return;
            }

            var utf8Json = JsonSerializer.SerializeToUtf8Bytes(merged);
            using var doc = JsonDocument.Parse(utf8Json);
            ConfigJsonApplier.Apply(doc.RootElement, settings);
        }

        private static void _MergeValidated(JsonObject parent, Type containerType, string[] segments, string value)
        {
            if (segments.Length == 1)
            {
                var leaf = _FindLeafField(containerType, segments[0])
                    ?? throw new InvalidDataException(
                        $"Unknown config field '{segments[0]}' under '{containerType.Name}'.");

                var jsonName = s_Naming.ConvertName(leaf.Name);
                parent[jsonName] = value;
                return;
            }

            var section = _FindSectionField(containerType, segments[0])
                ?? throw new InvalidDataException(
                    $"Unknown config section '{segments[0]}'.");

            var sectionKey = _GetSectionJsonKey(section);
            JsonObject sectionObject;
            if (parent[sectionKey] is JsonObject existingSection)
            {
                sectionObject = existingSection;
            }
            else
            {
                sectionObject = [];
                parent[sectionKey] = sectionObject;
            }

            _MergeValidated(sectionObject, section.FieldType, segments[1..], value);
        }

        private static FieldInfo? _FindSectionField(Type containerType, string segment)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            foreach (var field in containerType.GetFields(flags))
            {
                var attr = field.GetCustomAttribute<ConfigSectionAttribute>();
                if (attr is null)
                {
                    continue;
                }

                var jsonKey = string.IsNullOrEmpty(attr.JsonName)
                    ? s_Naming.ConvertName(field.Name)
                    : attr.JsonName;
                if (string.Equals(jsonKey, segment, StringComparison.OrdinalIgnoreCase))
                {
                    return field;
                }
            }

            return null;
        }

        private static FieldInfo? _FindLeafField(Type containerType, string segment)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            foreach (var field in containerType.GetFields(flags))
            {
                if (field.GetCustomAttribute<ConfigSectionAttribute>() is not null)
                {
                    continue;
                }

                var intRange = field.GetCustomAttribute<ConfigIntRangeAttribute>();
                var strMax = field.GetCustomAttribute<ConfigStringMaxLengthAttribute>();
                if (intRange is null && strMax is null)
                {
                    continue;
                }

                var jsonName = s_Naming.ConvertName(field.Name);
                if (string.Equals(jsonName, segment, StringComparison.OrdinalIgnoreCase))
                {
                    return field;
                }
            }

            return null;
        }

        private static string _GetSectionJsonKey(FieldInfo sectionField)
        {
            var attr = sectionField.GetCustomAttribute<ConfigSectionAttribute>()!;
            return string.IsNullOrEmpty(attr.JsonName)
                ? s_Naming.ConvertName(sectionField.Name)
                : attr.JsonName;
        }
    }
}
