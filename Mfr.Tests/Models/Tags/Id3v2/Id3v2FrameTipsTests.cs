using Mfr.Models.Tags;
using Mfr.Models.Tags.Id3v2;

namespace Mfr.Tests.Models.Tags.Id3v2
{
    /// <summary>
    /// Tests for <see cref="Id3v2FrameTips"/>.
    /// </summary>
    public sealed class Id3v2FrameTipsTests
    {
        /// <summary>
        /// Verifies every modeled frame has a non-empty tip.
        /// </summary>
        [Fact]
        public void For_returns_a_tip_for_every_modeled_frame()
        {
            foreach (var frameId in Id3v2ModeledFrame.AllModeledFrameIds)
            {
                Assert.False(string.IsNullOrWhiteSpace(Id3v2FrameTips.For(frameId)), frameId);
            }
        }

        /// <summary>
        /// Verifies 1:1 frames reuse the semantic meaning plus the frame id.
        /// </summary>
        [Fact]
        public void For_semantic_frames_reuse_semantic_tips()
        {
            Assert.Equal($"{SemanticAudioFieldTips.Title.TrimEnd('.')} (ID3v2 TIT2).", Id3v2FrameTips.For("TIT2"));
            Assert.Equal($"{SemanticAudioFieldTips.Artist.TrimEnd('.')} (ID3v2 TPE1).", Id3v2FrameTips.For("tpe1"));
            Assert.Equal($"{SemanticAudioFieldTips.Year.TrimEnd('.')} (ID3v2 TYER; v2.3).", Id3v2FrameTips.For("TYER"));
        }

        /// <summary>
        /// Verifies unmapped modeled frames fall back to the short-name catalog.
        /// </summary>
        [Fact]
        public void For_unmapped_frames_use_short_name()
        {
            Assert.Equal("Lyricist (ID3v2 TEXT).", Id3v2FrameTips.For("TEXT"));
            Assert.Equal("Remixer (ID3v2 TPE4).", Id3v2FrameTips.For("TPE4"));
        }
    }
}
