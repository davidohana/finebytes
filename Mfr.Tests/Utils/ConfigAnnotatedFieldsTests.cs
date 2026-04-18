using System.Text.Json;
using Mfr.Utils;

namespace Mfr.Tests.Utils
{
    /// <summary>
    /// Tests for <see cref="ConfigAnnotatedFields"/>.
    /// </summary>
    public sealed class ConfigAnnotatedFieldsTests
    {
        /// <summary>Maps CLR names to uppercase (e.g. <c>Port</c> → <c>PORT</c>) for policy coverage.</summary>
        private sealed class UpperInvariantNamingPolicy : JsonNamingPolicy
        {
            public override string ConvertName(string name)
            {
                return name.ToUpperInvariant();
            }
        }

        private sealed class SampleOptions
        {
            [ConfigIntRange(1, 100)]
            public int Port = 10;

            [ConfigStringMaxLength(4)]
            public string Name = "ab";

            /// <summary>Not annotated; must stay at its default when matching JSON keys are present.</summary>
            public int UnmappedField = 5;
        }

        private sealed class BadDualAttribute
        {
            [ConfigIntRange(1, 10)]
            [ConfigStringMaxLength(5)]
            public int X = 1;
        }

        private sealed class BadIntRangeOnLong
        {
            [ConfigIntRange(1, 10)]
            public long X = 1;
        }

        private sealed class BadStringMaxOnInt
        {
            [ConfigStringMaxLength(5)]
            public int X = 1;
        }

        [Fact]
        public void ApplyFromJsonObject_sets_annotated_fields_uses_camel_case_names()
        {
            using var doc = JsonDocument.Parse(/*lang=json,strict*/ """{"port":"50","name":"xyz"}""");
            var o = new SampleOptions();
            ConfigAnnotatedFields.ApplyFromJsonObject(doc.RootElement, o);
            Assert.Equal(50, o.Port);
            Assert.Equal("xyz", o.Name);
        }

        [Fact]
        public void ApplyFromJsonObject_missing_properties_leave_defaults()
        {
            using var doc = JsonDocument.Parse(/*lang=json,strict*/ "{}");
            var o = new SampleOptions();
            ConfigAnnotatedFields.ApplyFromJsonObject(doc.RootElement, o);
            Assert.Equal(10, o.Port);
            Assert.Equal("ab", o.Name);
        }

        [Fact]
        public void ApplyFromJsonObject_unannotated_public_field_is_not_read_from_json()
        {
            using var doc = JsonDocument.Parse(/*lang=json,strict*/ """{"unmappedField":"999","port":"20"}""");
            var o = new SampleOptions();
            ConfigAnnotatedFields.ApplyFromJsonObject(doc.RootElement, o);
            Assert.Equal(20, o.Port);
            Assert.Equal(5, o.UnmappedField);
        }

        [Fact]
        public void ApplyFromJsonObject_int_out_of_range_throws_InvalidDataException()
        {
            using var doc = JsonDocument.Parse(/*lang=json,strict*/ """{"port":"0"}""");
            var o = new SampleOptions();
            Assert.Throws<InvalidDataException>(() => ConfigAnnotatedFields.ApplyFromJsonObject(doc.RootElement, o));
        }

        [Fact]
        public void ApplyFromJsonObject_string_exceeds_max_length_throws_InvalidDataException()
        {
            using var doc = JsonDocument.Parse(/*lang=json,strict*/ """{"name":"hello"}""");
            var o = new SampleOptions();
            Assert.Throws<InvalidDataException>(() => ConfigAnnotatedFields.ApplyFromJsonObject(doc.RootElement, o));
        }

        [Fact]
        public void ApplyFromJsonObject_both_range_and_string_attributes_throws_InvalidOperationException()
        {
            using var doc = JsonDocument.Parse(/*lang=json,strict*/ "{}");
            var o = new BadDualAttribute();
            Assert.Throws<InvalidOperationException>(() => ConfigAnnotatedFields.ApplyFromJsonObject(doc.RootElement, o));
        }

        [Fact]
        public void ApplyFromJsonObject_int_range_on_non_int_field_throws_InvalidOperationException()
        {
            using var doc = JsonDocument.Parse(/*lang=json,strict*/ "{}");
            var o = new BadIntRangeOnLong();
            Assert.Throws<InvalidOperationException>(() => ConfigAnnotatedFields.ApplyFromJsonObject(doc.RootElement, o));
        }

        [Fact]
        public void ApplyFromJsonObject_string_max_on_non_string_field_throws_InvalidOperationException()
        {
            using var doc = JsonDocument.Parse(/*lang=json,strict*/ "{}");
            var o = new BadStringMaxOnInt();
            Assert.Throws<InvalidOperationException>(() => ConfigAnnotatedFields.ApplyFromJsonObject(doc.RootElement, o));
        }

        [Fact]
        public void ApplyFromJsonObject_target_null_throws_ArgumentNullException()
        {
            using var doc = JsonDocument.Parse(/*lang=json,strict*/ "{}");
            object? target = null;
            Assert.Throws<ArgumentNullException>(() => ConfigAnnotatedFields.ApplyFromJsonObject(doc.RootElement, target!));
        }

        [Fact]
        public void ApplyFromJsonObject_custom_naming_policy_is_used()
        {
            using var doc = JsonDocument.Parse(/*lang=json,strict*/ """{"PORT":"77"}""");
            var o = new SampleOptions();
            ConfigAnnotatedFields.ApplyFromJsonObject(doc.RootElement, o, new UpperInvariantNamingPolicy());
            Assert.Equal(77, o.Port);
        }
    }
}
