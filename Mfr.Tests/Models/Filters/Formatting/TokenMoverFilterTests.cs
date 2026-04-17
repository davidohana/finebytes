using Mfr.Filters.Formatting;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Formatting
{
    /// <summary>
    /// Tests for <see cref="TokenMoverFilter"/>.
    /// </summary>
    public class TokenMoverFilterTests
    {
        private static readonly FileNameTarget _target = new(FileNamePart.Prefix);

        /// <summary>
        /// Verifies moving the second token three places right (documented milk example).
        /// </summary>
        [Fact]
        public void Apply_MoveSecondTokenThreeRight_LongName()
        {
            var f = new TokenMoverFilter(
                _target,
                new TokenMoverOptions(Delimiter: ",", TokenNumber: 2, MoveBy: 3));
            var input = "milk,sugar,bread,potatoes,honey,salt,water";
            Assert.Equal(
                "milk,bread,potatoes,honey,sugar,salt,water",
                FilterTestHelpers.ApplyToPrefix(f, input));
        }

        /// <summary>
        /// Verifies large positive offset clamps the token to the last position.
        /// </summary>
        [Fact]
        public void Apply_MoveSecondTokenThreeRight_ShortName_ClampsToEnd()
        {
            var f = new TokenMoverFilter(
                _target,
                new TokenMoverOptions(Delimiter: ",", TokenNumber: 2, MoveBy: 3));
            Assert.Equal(
                "milk,bread,potatoes,sugar",
                FilterTestHelpers.ApplyToPrefix(f, "milk,sugar,bread,potatoes"));
        }

        /// <summary>
        /// Verifies a large negative offset clamps the token to the first position.
        /// </summary>
        [Fact]
        public void Apply_MoveLeftPastStart_ClampsToFirst()
        {
            var f = new TokenMoverFilter(
                _target,
                new TokenMoverOptions(Delimiter: ",", TokenNumber: 2, MoveBy: -99));
            Assert.Equal("b,a,c", FilterTestHelpers.ApplyToPrefix(f, "a,b,c"));
        }

        /// <summary>
        /// Verifies a single step left swaps with the preceding token.
        /// </summary>
        [Fact]
        public void Apply_MoveOneLeft_SwapsWithPredecessor()
        {
            var f = new TokenMoverFilter(
                _target,
                new TokenMoverOptions(Delimiter: "-", TokenNumber: 2, MoveBy: -1));
            Assert.Equal("b-a-c", FilterTestHelpers.ApplyToPrefix(f, "a-b-c"));
        }

        /// <summary>
        /// Verifies no change when token index equals clamped target.
        /// </summary>
        [Fact]
        public void Apply_NoEffectiveMove_ReturnsOriginal()
        {
            var f = new TokenMoverFilter(
                _target,
                new TokenMoverOptions(Delimiter: ",", TokenNumber: 2, MoveBy: 0));
            Assert.Equal("a,b", FilterTestHelpers.ApplyToPrefix(f, "a,b"));
        }

        /// <summary>
        /// Verifies a zero-length delimiter leaves the segment unchanged.
        /// </summary>
        [Fact]
        public void Apply_EmptyDelimiter_DoesNotChange()
        {
            var f = new TokenMoverFilter(
                _target,
                new TokenMoverOptions(Delimiter: "", TokenNumber: 1, MoveBy: 1));
            Assert.Equal("a,b", FilterTestHelpers.ApplyToPrefix(f, "a,b"));
        }

        /// <summary>
        /// Verifies out-of-range token number leaves the segment unchanged.
        /// </summary>
        [Fact]
        public void Apply_TokenNumberTooLarge_DoesNotChange()
        {
            var f = new TokenMoverFilter(
                _target,
                new TokenMoverOptions(Delimiter: ",", TokenNumber: 3, MoveBy: 0));
            Assert.Equal("a,b", FilterTestHelpers.ApplyToPrefix(f, "a,b"));
        }
    }
}
