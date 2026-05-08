using Mfr.Filters.Formatting.Tokens.General;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.General
{
    /// <summary>
    /// Tests for <see cref="RandomCharToken"/>.
    /// </summary>
    public sealed class RandomCharTokenTests
    {
        /// <summary>
        /// Verifies digit ranges yield ASCII digits only.
        /// </summary>
        [Fact]
        public void Resolve_DigitRange_StaysWithinBounds()
        {
            var token = new RandomCharToken();
            var item = FilterTestHelpers.CreateRenameItem();

            for (var i = 0; i < 64; i++)
            {
                var s = token.Resolve(arg: "0,9", item: item);
                Assert.Single(s);
                var c = s[0];
                Assert.True(c is >= '0' and <= '9', $"Got '{c}' outside 0–9.");
            }
        }

        /// <summary>
        /// Verifies outputs stay within the inclusive ASCII letter range.
        /// </summary>
        [Fact]
        public void Resolve_UppercaseRange_StaysWithinBounds()
        {
            var token = new RandomCharToken();
            var item = FilterTestHelpers.CreateRenameItem();

            for (var i = 0; i < 64; i++)
            {
                var s = token.Resolve(arg: "A,Z", item: item);
                Assert.Single(s);
                var c = s[0];
                Assert.True(c is >= 'A' and <= 'Z', $"Got '{c}' outside A–Z.");
            }
        }

        /// <summary>
        /// Verifies reversed endpoints are accepted.
        /// </summary>
        [Fact]
        public void Resolve_ReversedEndpoints_NormalizesRange()
        {
            var token = new RandomCharToken();
            var item = FilterTestHelpers.CreateRenameItem();

            for (var i = 0; i < 32; i++)
            {
                var s = token.Resolve(arg: "Z,A", item: item);
                Assert.Single(s);
                var c = s[0];
                Assert.True(c is >= 'A' and <= 'Z');
            }
        }

        /// <summary>
        /// Verifies a collapsed range returns that character.
        /// </summary>
        [Fact]
        public void Resolve_SinglePointRange_ReturnsThatChar()
        {
            var token = new RandomCharToken();
            var item = FilterTestHelpers.CreateRenameItem();

            Assert.Equal("M", token.Resolve(arg: "M,M", item: item));
        }

        /// <summary>
        /// Verifies malformed arguments throw.
        /// </summary>
        [Fact]
        public void Resolve_MissingEndpoint_Throws()
        {
            var token = new RandomCharToken();
            var item = FilterTestHelpers.CreateRenameItem();

            var ex = Assert.Throws<InvalidOperationException>(() => token.Resolve(arg: "A", item: item));
            Assert.Contains("random-char", ex.Message);
        }
    }
}
