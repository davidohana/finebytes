using Mfr.Filters.Case;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Case
{
    /// <summary>
    /// Tests for <see cref="UppercaseInitialsFilter"/>.
    /// </summary>
    public class UppercaseInitialsFilterTests
    {
        private static readonly FilterTarget _target = new FileNameTarget(FileNamePart.Prefix);

        /// <summary>
        /// Verifies lowercase initials patterns are uppercased.
        /// </summary>
        [Fact]
        public void Apply_UppercasesInitialsPattern()
        {
            var filter = new UppercaseInitialsFilter(true, _target);
            var input = "bruce springsteen - born in the u.s.a";

            var result = FilterTestHelpers.ApplyToPrefix(filter, input);

            Assert.Equal("bruce springsteen - born in the U.S.A", result);
        }

        /// <summary>
        /// Verifies initials pattern length can be any number of letters.
        /// </summary>
        [Fact]
        public void Apply_UppercasesVariableLengthInitialsPatterns()
        {
            var filter = new UppercaseInitialsFilter(true, _target);
            var input = "live in the u.k and the u.s.a and e.u";

            var result = FilterTestHelpers.ApplyToPrefix(filter, input);

            Assert.Equal("live in the U.K and the U.S.A and E.U", result);
        }

        /// <summary>
        /// Verifies non-initial words and single letters stay unchanged.
        /// </summary>
        [Fact]
        public void Apply_DoesNotChangeNonInitialWords()
        {
            var filter = new UppercaseInitialsFilter(true, _target);
            var input = "alpha beta c and ab.cd stay as-is";

            var result = FilterTestHelpers.ApplyToPrefix(filter, input);

            Assert.Equal("alpha beta c and ab.cd stay as-is", result);
        }
    }
}
