using System.Text.Json;
using System.Text.Json.Serialization;
using Mfr.Core;
using Mfr.Filters.Space;
using Mfr.Models;

namespace Mfr.Tests.Core
{
    public class PresetJsonShapeProbeTests
    {
        [Fact]
        public void CamelCase_preset_json_roundtrips()
        {
            var json = /*lang=json,strict*/ """
            {
              "presets": [
                {
                  "id": "11111111-1111-1111-1111-111111111111",
                  "name": "Probe",
                  "chain": {
                    "steps": [
                      {
                        "enabled": true,
                        "filter": {
                          "type": "SeparateCapitalizedText",
                          "target": {
                            "family": "FileName",
                            "fileNamePart": "Prefix"
                          }
                        }
                      }
                    ]
                  }
                }
              ]
            }
            """;

            var container = JsonSerializer.Deserialize<PresetContainerWrapper>(json, PresetJsonOptions.Default);
            Assert.NotNull(container);
            Assert.Single(container.Presets);
            var step = container.Presets[0].Chain.Steps[0];
            Assert.True(step.Enabled);
            Assert.IsType<SeparateCapitalizedTextFilter>(step.Filter);
        }

        private sealed record PresetContainerWrapper(
            [property: JsonPropertyName("presets")] IReadOnlyList<FilterPreset> Presets);
    }
}
