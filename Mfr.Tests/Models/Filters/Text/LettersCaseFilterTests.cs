using Mfr.Models;
using Mfr.Models.Filters.Text;

namespace Mfr.Tests.Models.Filters.Text
{
    /// <summary>
    /// Tests for <see cref="LettersCaseFilter"/> transformations.
    /// </summary>
    public class LettersCaseFilterTests
    {
        private static readonly FileNameTarget _Target = new(FileNamePart.Prefix);

        /// <summary>
        /// Verifies upper-case mode.
        /// </summary>
        [Fact]
        public void Apply_UpperCase_ConvertsToUpperInvariant()
        {
            var f = new LettersCaseFilter(true, _Target, new LettersCaseOptions(LettersCaseMode.UpperCase, []));
            Assert.Equal("HELLO", FilterTestHelpers.ApplyToPrefix(f, "hello"));
        }

        /// <summary>
        /// Verifies lower-case mode.
        /// </summary>
        [Fact]
        public void Apply_LowerCase_ConvertsToLowerInvariant()
        {
            var f = new LettersCaseFilter(true, _Target, new LettersCaseOptions(LettersCaseMode.LowerCase, []));
            Assert.Equal("hello", FilterTestHelpers.ApplyToPrefix(f, "HELLO"));
        }

        /// <summary>
        /// Verifies title-case respects skip words.
        /// </summary>
        [Fact]
        public void Apply_TitleCase_SkipsConfiguredWords()
        {
            var f = new LettersCaseFilter(
                true,
                _Target,
                new LettersCaseOptions(LettersCaseMode.TitleCase, ["a", "the", "for"]));
            Assert.Equal("a Song for the World", FilterTestHelpers.ApplyToPrefix(f, "a song for the world"));
        }

        /// <summary>
        /// Verifies sentence-case capitalizes after sentence boundaries.
        /// </summary>
        [Fact]
        public void Apply_SentenceCase_CapitalizesAfterPunctuation()
        {
            var f = new LettersCaseFilter(true, _Target, new LettersCaseOptions(LettersCaseMode.SentenceCase, []));
            Assert.Equal("Hello world. Next line.", FilterTestHelpers.ApplyToPrefix(f, "hello world. next line."));
        }

        /// <summary>
        /// Verifies case inversion.
        /// </summary>
        [Fact]
        public void Apply_InvertCase_SwapsCasing()
        {
            var f = new LettersCaseFilter(true, _Target, new LettersCaseOptions(LettersCaseMode.InvertCase, []));
            Assert.Equal("hELLO", FilterTestHelpers.ApplyToPrefix(f, "Hello"));
        }
    }
}
