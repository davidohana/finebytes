using System.Text.Json;
using System.Text.Json.Serialization;
using Mfr.Utils;

namespace Mfr.Tests.Utils
{
    /// <summary>
    /// Tests for <see cref="ConfigValueReader"/>.
    /// </summary>
    public sealed class ConfigValueReaderTests
    {
        private const string IntSamplePropertyName = "intSetting";

        private sealed class SampleIntConfig
        {
            [JsonPropertyName(IntSamplePropertyName)]
            public required string AsString { get; init; }
        }

        private static string _IntPropertyJson(string value)
        {
            return JsonSerializer.Serialize(new SampleIntConfig { AsString = value });
        }

        [Fact]
        public void ReadInt_from_JsonElement_missing_property_leaves_ref_unchanged()
        {
            using var doc = JsonDocument.Parse(/*lang=json,strict*/ "{}");
            var x = 1000;
            ConfigValueReader.ReadInt(doc.RootElement, IntSamplePropertyName, ref x, 1, 10_000_000);
            Assert.Equal(1000, x);
        }

        [Fact]
        public void ReadInt_from_JsonElement_sets_ref_when_present()
        {
            using var doc = JsonDocument.Parse(_IntPropertyJson("99"));
            var x = 0;
            ConfigValueReader.ReadInt(doc.RootElement, IntSamplePropertyName, ref x, 1, 10_000_000);
            Assert.Equal(99, x);
        }

        [Fact]
        public void ReadInt_from_JsonElement_parses_integer_with_surrounding_whitespace()
        {
            using var doc = JsonDocument.Parse(/*lang=json,strict*/ """{"n":"  42 "}""");
            var x = 1;
            ConfigValueReader.ReadInt(doc.RootElement, "n", ref x, 1, 100);
            Assert.Equal(42, x);
        }

        [Fact]
        public void ReadInt_from_JsonElement_reads_large_value()
        {
            using var doc = JsonDocument.Parse(_IntPropertyJson("2048"));
            var x = 1000;
            ConfigValueReader.ReadInt(doc.RootElement, IntSamplePropertyName, ref x, 1, 10_000_000);
            Assert.Equal(2048, x);
        }

        [Fact]
        public void ReadInt_from_JsonElement_non_integer_throws()
        {
            using var doc = JsonDocument.Parse(_IntPropertyJson("x"));
            var x = 0;
            var ex = Assert.Throws<InvalidDataException>(() =>
                ConfigValueReader.ReadInt(doc.RootElement, IntSamplePropertyName, ref x, 1, 10_000_000));
            Assert.Contains(IntSamplePropertyName, ex.Message);
            Assert.Contains("integer", ex.Message);
        }

        [Fact]
        public void ReadInt_from_JsonElement_out_of_range_throws()
        {
            using var doc = JsonDocument.Parse(_IntPropertyJson("0"));
            var x = 1;
            Assert.Throws<InvalidDataException>(() =>
                ConfigValueReader.ReadInt(doc.RootElement, IntSamplePropertyName, ref x, 1, 10));
        }

        [Fact]
        public void ReadInt_from_JsonElement_non_object_throws()
        {
            using var doc = JsonDocument.Parse(/*lang=json,strict*/ "[]");
            var x = 1;
            Assert.Throws<InvalidDataException>(() =>
                ConfigValueReader.ReadInt(doc.RootElement, "x", ref x, 1, 10));
        }

        [Fact]
        public void ReadString_missing_property_leaves_ref_unchanged()
        {
            using var doc = JsonDocument.Parse(/*lang=json,strict*/ "{}");
            var s = "d";
            ConfigValueReader.ReadString(doc.RootElement, "p", ref s);
            Assert.Equal("d", s);
        }

        [Fact]
        public void ReadString_json_null_leaves_ref_unchanged()
        {
            using var doc = JsonDocument.Parse(/*lang=json,strict*/ """{"p":null}""");
            var s = "d";
            ConfigValueReader.ReadString(doc.RootElement, "p", ref s);
            Assert.Equal("d", s);
        }

        [Fact]
        public void ReadString_blank_in_json_throws()
        {
            using var doc = JsonDocument.Parse(/*lang=json,strict*/ """{"p":"   "}""");
            var s = "d";
            var ex = Assert.Throws<InvalidDataException>(() =>
                ConfigValueReader.ReadString(doc.RootElement, "p", ref s));
            Assert.Contains("p", ex.Message);
            Assert.Contains("non-empty", ex.Message);
        }

        [Fact]
        public void ReadString_preserves_surrounding_whitespace()
        {
            using var doc = JsonDocument.Parse(/*lang=json,strict*/ """{"p":"  ab  "}""");
            var s = "";
            ConfigValueReader.ReadString(doc.RootElement, "p", ref s);
            Assert.Equal("  ab  ", s);
        }

        [Fact]
        public void ReadString_exceeds_max_length_throws()
        {
            using var doc = JsonDocument.Parse(/*lang=json,strict*/ """{"p":"abcd"}""");
            var s = "";
            Assert.Throws<InvalidDataException>(() =>
                ConfigValueReader.ReadString(doc.RootElement, "p", ref s, maxLengthInclusive: 2));
        }
    }
}
