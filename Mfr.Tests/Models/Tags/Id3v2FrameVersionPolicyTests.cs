using Mfr.Models.Tags.Id3v2;

namespace Mfr.Tests.Models.Tags
{
    /// <summary>
    /// Tests <see cref="Id3v2FrameVersionPolicy"/> (v2.4-only frames vs tag version).
    /// </summary>
    public sealed class Id3v2FrameVersionPolicyTests
    {
        /// <summary>
        /// <c>TDRC</c> and other v2.4-only ids require version 4.
        /// </summary>
        [Theory]
        [InlineData("TDRC")]
        [InlineData("tdrc")]
        [InlineData("TMOO")]
        [InlineData("TSST")]
        public void RequiresId3v24_KnownV24Frames(string frameId)
        {
            Assert.True(Id3v2FrameVersionPolicy.RequiresId3v24(frameId));
        }

        /// <summary>
        /// Common v2.3 frames are allowed on any version.
        /// </summary>
        [Theory]
        [InlineData("TIT2")]
        [InlineData("TYER")]
        [InlineData("COMM")]
        public void RequiresId3v24_CommonFrames_False(string frameId)
        {
            Assert.False(Id3v2FrameVersionPolicy.RequiresId3v24(frameId));
        }

        /// <summary>
        /// Writing <c>TDRC</c> into a v2.3 tag throws (no silent upgrade).
        /// </summary>
        [Fact]
        public void EnsureCompatible_TdrcOnV23_Throws()
        {
            var ex = Assert.Throws<NotSupportedException>(() =>
                Id3v2FrameVersionPolicy.EnsureCompatible(tagVersion: 3, frameId: "TDRC"));
            Assert.Contains("TDRC", ex.Message, StringComparison.Ordinal);
            Assert.Contains("2.4", ex.Message, StringComparison.Ordinal);
            Assert.Contains("2.3", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Writing <c>TDRC</c> into a v2.4 tag is allowed.
        /// </summary>
        [Fact]
        public void EnsureCompatible_TdrcOnV24_Succeeds()
        {
            Id3v2FrameVersionPolicy.EnsureCompatible(tagVersion: 4, frameId: "TDRC");
        }

        /// <summary>
        /// Writing <c>TIT2</c> into a v2.3 tag is allowed.
        /// </summary>
        [Fact]
        public void EnsureCompatible_Tit2OnV23_Succeeds()
        {
            Id3v2FrameVersionPolicy.EnsureCompatible(tagVersion: 3, frameId: "TIT2");
        }
    }
}
