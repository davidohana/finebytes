using System.Text.Json;
using Mfr.App.Ui.Views.DragAndDrop;
using Mfr.App.Ui.Views.RenameList;

namespace Mfr.Tests.Ui.DragAndDrop
{
    /// <summary>
    /// Tests for <see cref="JsonDragPayload"/> drag deserialize safety.
    /// </summary>
    public sealed class JsonDragPayloadTests
    {
        /// <summary>
        /// Verifies non-object JSON (e.g. Rename List reorder marker) does not throw.
        /// </summary>
        [Fact]
        public void Deserialize_invalid_json_returns_null()
        {
            Assert.Null(JsonDragPayload.Deserialize<RenameListSampleDragPayload>("1"));
            Assert.Null(JsonDragPayload.Deserialize<RenameListSampleDragPayload>("\"C:\\\\temp\\\\a.txt\""));
            Assert.Null(JsonDragPayload.Deserialize<RenameListSampleDragPayload>("[]"));
        }

        /// <summary>
        /// Verifies a well-formed sample payload round-trips.
        /// </summary>
        [Fact]
        public void Deserialize_sample_payload_round_trips()
        {
            var json = new RenameListSampleDragPayload(TestPaths.Absolute("a.txt")).Serialize();
            var payload = JsonDragPayload.Deserialize<RenameListSampleDragPayload>(json);
            Assert.NotNull(payload);
            Assert.Equal(TestPaths.Absolute("a.txt"), payload.FullPath);
        }

        /// <summary>
        /// Verifies JsonException is not propagated for malformed objects.
        /// </summary>
        [Fact]
        public void Deserialize_malformed_object_returns_null()
        {
            // Build incomplete JSON without a JSON001-flagged string literal.
            var incompleteJson = "{" + string.Empty;
            Assert.Null(JsonDragPayload.Deserialize<RenameListSampleDragPayload>(incompleteJson));
            // Ensure the catch path is JsonException-shaped (not a different failure mode).
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<RenameListSampleDragPayload>(incompleteJson));
        }
    }
}
