using System.Text.Json;
using Mfr.Utils.Config;

namespace Mfr.Tests.Utils.Config
{
    /// <summary>
    /// Tests for <see cref="JsonObjectProperties"/>.
    /// </summary>
    public sealed class JsonObjectPropertiesTests
    {
        /// <summary>
        /// Verifies case-insensitive any-value lookup.
        /// </summary>
        [Fact]
        public void TryGetPropertyIgnoreCase_matches_ignoring_case()
        {
            using var doc = JsonDocument.Parse("""{"FileList":{"fileMask":"*.mp3"}}""");

            Assert.True(JsonObjectProperties.TryGetPropertyIgnoreCase(doc.RootElement, "filelist", out var value));
            Assert.Equal(JsonValueKind.Object, value.ValueKind);
            Assert.False(JsonObjectProperties.TryGetPropertyIgnoreCase(doc.RootElement, "missing", out _));
            Assert.False(JsonObjectProperties.TryGetPropertyIgnoreCase(value.GetProperty("fileMask"), "x", out _));
        }

        /// <summary>
        /// Verifies object-only lookup accepts objects, skips null, and rejects other kinds.
        /// </summary>
        [Fact]
        public void TryGetObjectProperty_requires_object_or_null()
        {
            using var doc = JsonDocument.Parse("""{"nested":{"a":1},"gone":null,"leaf":"x"}""");

            Assert.True(JsonObjectProperties.TryGetObjectProperty(doc.RootElement, "Nested", out var nested));
            Assert.Equal(JsonValueKind.Object, nested.ValueKind);
            Assert.False(JsonObjectProperties.TryGetObjectProperty(doc.RootElement, "gone", out _));
            Assert.False(JsonObjectProperties.TryGetObjectProperty(doc.RootElement, "missing", out _));
            Assert.Throws<InvalidDataException>(() =>
                JsonObjectProperties.TryGetObjectProperty(doc.RootElement, "leaf", out _)
            );
        }
    }
}
