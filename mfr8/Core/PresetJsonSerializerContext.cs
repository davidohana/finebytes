using System.Text.Json.Serialization;

namespace Mfr8.Core
{
    // Source-generated serializer context avoids reflection-based JSON metadata at runtime.
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, UseStringEnumConverter = true)]
    [JsonSerializable(typeof(PresetContainer))]
    internal sealed partial class PresetJsonSerializerContext : JsonSerializerContext
    {
    }
}
