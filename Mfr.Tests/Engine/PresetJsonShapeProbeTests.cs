using System.Text.Json;
using System.Text.Json.Serialization;
using Mfr.Engine;
using Mfr.Filters.Attributes;
using Mfr.Filters.Audio;
using Mfr.Filters.Case;
using Mfr.Filters.Formatting;
using Mfr.Filters.Replace;
using Mfr.Filters.Space;
using Mfr.Models.Tags;
using Mfr.Models.Tags.Id3v1;

namespace Mfr.Tests.Engine
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
                            "targetType": "FilePrefix"
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

        [Fact]
        public void TagRemover_roundtrips_block_names()
        {
            var json = /*lang=json,strict*/ """
            {
              "type": "TagRemover",
              "options": {
                "blocks": ["id3v1", "riffInfo"]
              }
            }
            """;

            var filter = JsonSerializer.Deserialize<BaseFilter>(json, PresetJsonOptions.Default);
            Assert.NotNull(filter);
            var typed = Assert.IsType<TagRemoverFilter>(filter);
            AudioTagBlockKind[] expectedBlocks = [AudioTagBlockKind.Id3v1, AudioTagBlockKind.RiffInfo];
            Assert.Equal(expectedBlocks, typed.Options.Blocks);
            Assert.False(typed.Options.All);
            typed.Setup();
        }

        [Fact]
        public void TagRemover_roundtrips_all_flag()
        {
            var json = /*lang=json,strict*/ """
            {
              "type": "TagRemover",
              "options": {
                "all": true
              }
            }
            """;

            var filter = JsonSerializer.Deserialize<BaseFilter>(json, PresetJsonOptions.Default);
            Assert.NotNull(filter);
            var typed = Assert.IsType<TagRemoverFilter>(filter);
            Assert.True(typed.Options.All);
            typed.Setup();
        }

        [Fact]
        public void Replacer_JSON_round_trips_AncestorFolder_target()
        {
            var json = /*lang=json,strict*/ """
            {
              "type": "Replacer",
              "target": {
                "targetType": "AncestorFolder",
                "level": 2
              },
              "options": {
                "find": "a",
                "replacement": "b",
                "mode": "Literal",
                "caseSensitive": true,
                "replaceAll": true,
                "wholeWord": false
              }
            }
            """;

            var filter = JsonSerializer.Deserialize<BaseFilter>(json, PresetJsonOptions.Default);
            Assert.NotNull(filter);
            var typed = Assert.IsType<ReplacerFilter>(filter);
            var target = Assert.IsType<AncestorFolderTarget>(typed.Target);
            Assert.Equal(2, target.Level);

            typed.Setup();
        }

        [Fact]
        public void Formatter_JSON_round_trips_FullPath_target()
        {
            var json = /*lang=json,strict*/ """
            {
              "type": "Formatter",
              "target": {
                "targetType": "FullPath"
              },
              "options": {
                "template": "D:\\Staging\\<full-name>"
              }
            }
            """;

            var filter = JsonSerializer.Deserialize<BaseFilter>(json, PresetJsonOptions.Default);
            Assert.NotNull(filter);
            var typed = Assert.IsType<FormatterFilter>(filter);
            Assert.IsType<FullPathTarget>(typed.Target);
            Assert.Equal("D:\\Staging\\<full-name>", typed.Options.Template);
            typed.Setup();
        }

        [Fact]
        public void Formatter_JSON_round_trips_ParentDirectory_target()
        {
            var json = /*lang=json,strict*/ """
            {
              "type": "Formatter",
              "target": {
                "targetType": "ParentDirectory"
              },
              "options": {
                "template": "D:\\Archived"
              }
            }
            """;

            var filter = JsonSerializer.Deserialize<BaseFilter>(json, PresetJsonOptions.Default);
            Assert.NotNull(filter);
            var typed = Assert.IsType<FormatterFilter>(filter);
            Assert.IsType<ParentDirectoryTarget>(typed.Target);
            typed.Setup();
        }

        [Fact]
        public void Formatter_JSON_round_trips_AudioField_target()
        {
            var json = /*lang=json,strict*/ """
            {
              "type": "Formatter",
              "target": {
                "targetType": "SemanticAudioField",
                "field": "title"
              },
              "options": {
                "template": "<file-name>"
              }
            }
            """;

            var filter = JsonSerializer.Deserialize<BaseFilter>(json, PresetJsonOptions.Default);
            Assert.NotNull(filter);
            var typed = Assert.IsType<FormatterFilter>(filter);
            var target = Assert.IsType<SemanticAudioFieldTarget>(typed.Target);
            Assert.Equal(SemanticAudioField.Title, target.Field);
            typed.Setup();
        }

        [Fact]
        public void Formatter_JSON_round_trips_Id3v2Frame_target()
        {
            var json = /*lang=json,strict*/ """
            {
              "type": "Formatter",
              "target": {
                "targetType": "Id3v2Frame",
                "frameId": "COMM",
                "language": "eng",
                "description": ""
              },
              "options": {
                "template": "hi"
              }
            }
            """;

            var filter = JsonSerializer.Deserialize<BaseFilter>(json, PresetJsonOptions.Default);
            Assert.NotNull(filter);
            var typed = Assert.IsType<FormatterFilter>(filter);
            var target = Assert.IsType<Id3v2FrameTarget>(typed.Target);
            Assert.Equal("COMM", target.FrameId);
            Assert.Equal("eng", target.Language);
            Assert.Equal(string.Empty, target.Description);
            typed.Setup();
        }

        [Fact]
        public void Formatter_JSON_round_trips_XiphField_and_Id3v1Field_targets()
        {
            var xiphJson = /*lang=json,strict*/ """
            {
              "type": "Formatter",
              "target": {
                "targetType": "XiphField",
                "key": "TITLE"
              },
              "options": {
                "template": "x"
              }
            }
            """;
            var xiphFilter = Assert.IsType<FormatterFilter>(
                JsonSerializer.Deserialize<BaseFilter>(xiphJson, PresetJsonOptions.Default));
            Assert.Equal("TITLE", Assert.IsType<XiphFieldTarget>(xiphFilter.Target).Key);

            var id3v1Json = /*lang=json,strict*/ """
            {
              "type": "Formatter",
              "target": {
                "targetType": "Id3v1Field",
                "field": "artist"
              },
              "options": {
                "template": "x"
              }
            }
            """;
            var id3v1Filter = Assert.IsType<FormatterFilter>(
                JsonSerializer.Deserialize<BaseFilter>(id3v1Json, PresetJsonOptions.Default));
            Assert.Equal(Id3v1Field.Artist, Assert.IsType<Id3v1FieldTarget>(id3v1Filter.Target).Field);
        }

        [Fact]
        public void LettersCase_JSON_round_trips_apply_scope_token()
        {
            var expected = new LettersCaseFilter(
                new FilePrefixTarget(),
                new LettersCaseOptions(LettersCaseMode.UpperCase, []),
                new TokenApplyScope(Separator: "-", TokenNumber: 2));
            var canonicalJson = JsonSerializer.Serialize<BaseFilter>(expected, PresetJsonOptions.Default);
            var fromObject = JsonSerializer.Deserialize<BaseFilter>(canonicalJson, PresetJsonOptions.Default);
            Assert.NotNull(fromObject);
            var typedFromObject = Assert.IsType<LettersCaseFilter>(fromObject);
            Assert.IsType<TokenApplyScope>(typedFromObject.ApplyScope);

            var json = /*lang=json,strict*/ """
            {
              "type": "LettersCase",
              "target": {
                "targetType": "FilePrefix"
              },
              "options": {
                "mode": "UpperCase",
                "skipWords": [],
                "weirdUppercaseChancePercent": 50,
                "weirdFixedPlaces": false
              },
              "applyScope": {
                "scopeType": "Token",
                "separator": "-",
                "tokenNumber": 2
              }
            }
            """;

            var filter = JsonSerializer.Deserialize<BaseFilter>(json, PresetJsonOptions.Default);
            Assert.NotNull(filter);
            var typed = Assert.IsType<LettersCaseFilter>(filter);
            var tokenScope = Assert.IsType<TokenApplyScope>(typed.ApplyScope);
            Assert.Equal("-", tokenScope.Separator);
            Assert.Equal(2, tokenScope.TokenNumber);
            typed.Setup();

            var serialized = JsonSerializer.Serialize(typed, PresetJsonOptions.Default);
            Assert.Contains("LettersCase", serialized, StringComparison.Ordinal);
            Assert.Contains("applyScope", serialized, StringComparison.Ordinal);
            Assert.Contains("Token", serialized, StringComparison.Ordinal);
        }

        private sealed record PresetContainerWrapper(
            [property: JsonPropertyName("presets")] IReadOnlyList<FilterPreset> Presets);
    }
}
