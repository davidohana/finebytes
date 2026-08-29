using System.Text.Json;
using System.Text.Json.Nodes;

namespace Mfr.Utils.Config
{
    /// <summary>
    /// Applies CLI <c>--set</c> assignments to annotated config.
    /// <para>
    /// Builds merged JSON from <c>section.leaf=value</c> keys, validates paths, then uses <see cref="ConfigJsonApplier"/>.
    /// </para>
    /// </summary>
    public static class ConfigOverridesApplier
    {
        private static readonly JsonNamingPolicy s_Naming = JsonNamingPolicy.CamelCase;

        /// <summary>
        /// Applies parsed <c>--set</c> strings to <paramref name="config"/>.
        /// <para>
        /// Validates dotted paths against <typeparamref name="TConfig"/>, merges JSON, then <see cref="ConfigJsonApplier.Apply"/>.
        /// </para>
        /// </summary>
        /// <typeparam name="TConfig">Root config type with <see cref="ConfigSectionAttribute"/> sections and leaf attributes.</typeparam>
        /// <param name="assignments">Raw <c>key=value</c> strings; blank entries are skipped.</param>
        /// <param name="config">The object to update.</param>
        /// <exception cref="InvalidDataException">Assignment format, path, or value is invalid.</exception>
        public static void Apply<TConfig>(IReadOnlyList<string> assignments, TConfig config)
            where TConfig : class
        {
            ArgumentNullException.ThrowIfNull(assignments);
            ArgumentNullException.ThrowIfNull(config);

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
                    throw new InvalidDataException($"Invalid --set argument (expected key=value): '{raw}'.");
                }

                var dottedKey = trimmed[..equalsIndex].Trim();
                var value = trimmed[(equalsIndex + 1)..].Trim();
                if (dottedKey.IsBlank())
                {
                    throw new InvalidDataException($"Invalid --set argument (missing key before '='): '{raw}'.");
                }

                var segments = dottedKey.Split(
                    '.',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                );
                if (segments.Length < 2)
                {
                    throw new InvalidDataException(
                        $"Config path must include a section and a field (e.g. log.maxSessionFiles); got '{dottedKey}'."
                    );
                }

                _MergeValidated(merged, typeof(TConfig), segments, value);
            }

            if (merged.Count == 0)
            {
                return;
            }

            var utf8Json = JsonSerializer.SerializeToUtf8Bytes(merged);
            using var doc = JsonDocument.Parse(utf8Json);
            ConfigJsonApplier.Apply(doc.RootElement, config);
        }

        private static void _MergeValidated(JsonObject parent, Type containerType, string[] segments, string value)
        {
            if (segments.Length == 1)
            {
                var leaf =
                    ConfigFieldBindings.FindLeaf(containerType, s_Naming, segments[0])
                    ?? throw new InvalidDataException(
                        $"Unknown config field '{segments[0]}' under '{containerType.Name}'."
                    );

                parent[leaf.JsonName] = value;
                return;
            }

            var section =
                ConfigFieldBindings.FindSection(containerType, s_Naming, segments[0])
                ?? throw new InvalidDataException($"Unknown config section '{segments[0]}'.");

            JsonObject sectionObject;
            if (parent[section.JsonName] is JsonObject existingSection)
            {
                sectionObject = existingSection;
            }
            else
            {
                sectionObject = [];
                parent[section.JsonName] = sectionObject;
            }

            _MergeValidated(sectionObject, section.Field.FieldType, segments[1..], value);
        }
    }
}
