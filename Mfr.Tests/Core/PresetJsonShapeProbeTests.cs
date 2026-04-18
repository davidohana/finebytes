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
        public void AttributesSetter_roundtrips_without_target()
        {
            var json = /*lang=json,strict*/ """
            {
              "type": "AttributesSetter",
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
        }

        [Fact]
        public void DateSetter_roundtrips_with_timestamp_field()
        {
            var json = /*lang=json,strict*/ """
            {
              "type": "DateSetter",
              "options": {
                "timestampField": "lastWrite",
                "date": "2021-07-04"
              }
            }
            """;

            var filter = JsonSerializer.Deserialize<BaseFilter>(json, PresetJsonOptions.Default);
            Assert.NotNull(filter);
            var typed = Assert.IsType<DateSetterFilter>(filter);
            Assert.Equal(new DateOnly(2021, 7, 4), typed.Options.Date);
            Assert.Equal(TimestampField.LastWrite, typed.Options.TimestampField);
            typed.Setup();
        }

        [Fact]
        public void TimeSetter_roundtrips_with_timestamp_field()
        {
            var json = /*lang=json,strict*/ """
            {
              "type": "TimeSetter",
              "options": {
                "timestampField": "lastAccess",
                "time": "18:30:00"
              }
            }
            """;

            var filter = JsonSerializer.Deserialize<BaseFilter>(json, PresetJsonOptions.Default);
            Assert.NotNull(filter);
            var typed = Assert.IsType<TimeSetterFilter>(filter);
            Assert.Equal(new TimeOnly(18, 30, 0), typed.Options.Time);
            Assert.Equal(TimestampField.LastAccess, typed.Options.TimestampField);
            typed.Setup();
        }

        [Fact]
        public void TimeShifter_roundtrips_with_timestamp_field()
        {
            var json = /*lang=json,strict*/ """
            {
              "type": "TimeShifter",
              "options": {
                "timestampField": "lastWrite",
                "amount": -5,
                "unit": "days"
              }
            }
            """;

            var filter = JsonSerializer.Deserialize<BaseFilter>(json, PresetJsonOptions.Default);
            Assert.NotNull(filter);
            var typed = Assert.IsType<TimeShifterFilter>(filter);
            Assert.Equal(-5, typed.Options.Amount);
            Assert.Equal(TimeShiftUnit.Days, typed.Options.Unit);
            Assert.Equal(TimestampField.LastWrite, typed.Options.TimestampField);
            typed.Setup();
        }

        private sealed record PresetContainerWrapper(
            [property: JsonPropertyName("presets")] IReadOnlyList<FilterPreset> Presets);
    }
}
