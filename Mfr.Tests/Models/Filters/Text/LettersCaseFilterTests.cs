using Mfr8.Models;
using Mfr8.Models.Filters.Text;

namespace Mfr8.Tests.Models.Filters.Text
{
    /// <summary>
    /// Tests for <see cref="LettersCaseFilter"/> transformations.
    /// </summary>
    public class LettersCaseFilterTests
    {
        private static readonly FileNameTarget _Target = new(FileNameTargetMode.Prefix);

        /// <summary>
        /// Verifies upper-case mode.
        /// </summary>
        [Fact]
        public void Apply_UpperCase_ConvertsToUpperInvariant()
        {
            var f = new LettersCaseFilter(true, _Target, new LettersCaseOptions(LettersCaseMode.UpperCase, []));
            var file = FilterTestHelpers.CreateFile();
            Assert.Equal("HELLO", f.Apply("hello", file));
        }

        /// <summary>
        /// Verifies lower-case mode.
        /// </summary>
        [Fact]
        public void Apply_LowerCase_ConvertsToLowerInvariant()
        {
            var f = new LettersCaseFilter(true, _Target, new LettersCaseOptions(LettersCaseMode.LowerCase, []));
            var file = FilterTestHelpers.CreateFile();
            Assert.Equal("hello", f.Apply("HELLO", file));
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
            var file = FilterTestHelpers.CreateFile();
            Assert.Equal("a Song for the World", f.Apply("a song for the world", file));
        }

        /// <summary>
        /// Verifies sentence-case capitalizes after sentence boundaries.
        /// </summary>
        [Fact]
        public void Apply_SentenceCase_CapitalizesAfterPunctuation()
        {
            var f = new LettersCaseFilter(true, _Target, new LettersCaseOptions(LettersCaseMode.SentenceCase, []));
            var file = FilterTestHelpers.CreateFile();
            Assert.Equal("Hello world. Next line.", f.Apply("hello world. next line.", file));
        }

        /// <summary>
        /// Verifies case inversion.
        /// </summary>
        [Fact]
        public void Apply_InvertCase_SwapsCasing()
        {
            var f = new LettersCaseFilter(true, _Target, new LettersCaseOptions(LettersCaseMode.InvertCase, []));
            var file = FilterTestHelpers.CreateFile();
            Assert.Equal("hELLO", f.Apply("Hello", file));
        }
    }
}
