using System.Text.Json;
using System.Text.Json.Serialization;
using Mfr.Models.Config;

namespace Mfr.Engine.Presets
{
    /// <summary>
    /// Serializer options for <see cref="SessionStore"/> when the document may include
    /// <see cref="SessionState.AppliedFilters"/>.
    /// <para>
    /// Keeps session enum camelCase naming and soft-loads the working chain (unknown/invalid steps
    /// dropped; rest of session kept). Filter payloads inside the chain use the same serialization as
    /// presets via <see cref="SoftLoadFilterChainJsonConverter"/>.
    /// </para>
    /// </summary>
    public static class SessionJsonOptions
    {
        /// <summary>
        /// Options for load/save of <c>session.json</c> including the Applied Filters chain.
        /// </summary>
        public static JsonSerializerOptions Default { get; } = _CreateOptions();

        private static JsonSerializerOptions _CreateOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,

                PropertyNameCaseInsensitive = true,

                Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                    new SoftLoadFilterChainJsonConverter(),
                },
            };
        }
    }
}
