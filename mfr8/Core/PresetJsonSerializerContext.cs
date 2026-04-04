using System.Text.Json.Serialization;

namespace Mfr8.Core
{
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, UseStringEnumConverter = true)]
    [JsonSerializable(typeof(PresetsDocument))]
    internal sealed partial class PresetJsonSerializerContext : JsonSerializerContext
    {
    }
}
