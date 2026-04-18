using System.Text.Json;
using System.Text.Json.Serialization;
using Mfr.Core;
using Mfr.Filters.Attributes;
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

        [Fact]
        public void AttributesSetter_with_FileName_target_fails_setup()
        {
            var json = /*lang=json,strict*/ """
            {
              "type": "AttributesSetter",
              "target": {
                "family": "FileName",
                "fileNamePart": "Prefix"
              },
              "options": {
                "readOnly": "Keep",
                "hidden": "Keep",
                "archive": "Keep",
                "system": "Keep"
              }
            }
            """;

            var filter = JsonSerializer.Deserialize<BaseFilter>(json, PresetJsonOptions.Default);
            Assert.NotNull(filter);
            var typed = Assert.IsType<AttributesSetterFilter>(filter);
            var ex = Assert.Throws<InvalidOperationException>(() => typed.Setup());
            Assert.Contains("Attributes", ex.Message);
        }

        [Fact]
        public void AttributesSetter_roundtrips_with_attributes_target()
        {
            var json = /*lang=json,strict*/ """
            {
              "type": "AttributesSetter",
              "target": { "family": "Attributes" },
              "options": {
                "readOnly": "Set",
                "hidden": "Clear",
                "archive": "Keep",
                "system": "Keep"
              }
            }
            """;

            var filter = JsonSerializer.Deserialize<BaseFilter>(json, PresetJsonOptions.Default);
            Assert.NotNull(filter);
            var typed = Assert.IsType<AttributesSetterFilter>(filter);
            Assert.Equal(AttributeTriState.Set, typed.Options.ReadOnly);
            Assert.Equal(AttributeTriState.Clear, typed.Options.Hidden);
            typed.Setup();
            Assert.IsType<AttributesTarget>(typed.Target);
        }

        [Fact]
        public void DateSetter_roundtrips_with_LastWriteDate_target()
        {
            var json = /*lang=json,strict*/ """
            {
              "type": "DateSetter",
              "target": { "family": "LastWriteDate" },
              "options": {
                "date": "2021-07-04"
              }
            }
            """;

            var filter = JsonSerializer.Deserialize<BaseFilter>(json, PresetJsonOptions.Default);
            Assert.NotNull(filter);
            var typed = Assert.IsType<DateSetterFilter>(filter);
            Assert.Equal(new DateOnly(2021, 7, 4), typed.Options.Date);
            typed.Setup();
            Assert.IsType<LastWriteDateTarget>(typed.Target);
        }

        private sealed record PresetContainerWrapper(
            [property: JsonPropertyName("presets")] IReadOnlyList<FilterPreset> Presets);
    }
}
