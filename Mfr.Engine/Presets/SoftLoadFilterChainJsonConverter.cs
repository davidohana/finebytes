using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mfr.Engine.Presets
{
    /// <summary>
    /// Soft-loads <see cref="FilterChain"/> for session JSON: unknown or invalid steps are dropped.
    /// <para>
    /// Opposite of preset hard-fail: a bad step must not wipe the rest of <c>session.json</c>.
    /// Missing <c>steps</c> or a non-object value yields an empty chain (not null), except JSON null → null.
    /// Filter payloads use <see cref="PresetJsonOptions"/> so option enums match preset <c>chain</c> steps.
    /// </para>
    /// </summary>
    internal sealed class SoftLoadFilterChainJsonConverter : JsonConverter<FilterChain?>
    {
        /// <inheritdoc />
        public override FilterChain? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                reader.Skip();
                return new FilterChain { Steps = [] };
            }

            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;
            if (!_TryGetPropertyIgnoreCase(root, "steps", out var stepsElement))
            {
                return new FilterChain { Steps = [] };
            }

            if (stepsElement.ValueKind != JsonValueKind.Array)
            {
                return new FilterChain { Steps = [] };
            }

            var steps = new List<FilterChainStep>();
            foreach (var stepElement in stepsElement.EnumerateArray())
            {
                if (stepElement.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                if (!_TryReadStep(stepElement, out var step))
                {
                    continue;
                }

                steps.Add(step);
            }

            return new FilterChain { Steps = steps };
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, FilterChain? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName("steps");
            writer.WriteStartArray();
            foreach (var step in value.Steps)
            {
                writer.WriteStartObject();
                writer.WriteBoolean("enabled", step.Enabled);
                writer.WritePropertyName("filter");
                JsonSerializer.Serialize(writer, step.Filter, typeof(BaseFilter), PresetJsonOptions.Default);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        /// <summary>
        /// Tries to read one step; returns false when <c>filter</c> is missing or fails to deserialize.
        /// </summary>
        private static bool _TryReadStep(JsonElement stepElement, out FilterChainStep step)
        {
            step = default!;
            var enabled = true;
            BaseFilter? filter = null;

            foreach (var property in stepElement.EnumerateObject())
            {
                if (property.Name.Equals("enabled", StringComparison.OrdinalIgnoreCase))
                {
                    if (property.Value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    {
                        enabled = property.Value.GetBoolean();
                    }

                    continue;
                }

                if (!property.Name.Equals("filter", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    filter = JsonSerializer.Deserialize<BaseFilter>(
                        property.Value.GetRawText(),
                        PresetJsonOptions.Default
                    );
                }
                catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
                {
                    // Unknown type discriminator or bad payload — drop this step only.
                    return false;
                }
            }

            if (filter is null)
            {
                return false;
            }

            step = new FilterChainStep(enabled, filter);
            return true;
        }

        /// <summary>
        /// Looks up a JSON object property by name, ignoring ASCII case.
        /// </summary>
        private static bool _TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (!property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                value = property.Value;
                return true;
            }

            value = default;
            return false;
        }
    }
}
