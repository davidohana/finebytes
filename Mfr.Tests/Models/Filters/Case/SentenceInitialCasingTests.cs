using Mfr.Filters.Case;

namespace Mfr.Tests.Models.Filters.Case
{
    /// <summary>
    /// Tests for <see cref="SentenceInitialCasing"/>.
    /// </summary>
    public sealed class SentenceInitialCasingTests
    {
        /// <summary>
        /// Verifies start and after-punctuation uppercasing with the default word separator.
        /// </summary>
        [Fact]
        public void UppercaseInitials_CapitalizesStartAndAfterPunctuation()
        {
            Assert.Equal(
                "Hello world. Next line.",
                SentenceInitialCasing.UppercaseInitials("hello world. next line.", ' ', ".!?")
            );
        }

        /// <summary>
        /// Verifies non-ASCII letters are uppercased at sentence starts.
        /// </summary>
        [Fact]
        public void UppercaseInitials_CapitalizesNonAsciiLetters()
        {
            Assert.Equal("École. Über next.", SentenceInitialCasing.UppercaseInitials("école. über next.", ' ', ".!?"));
        }

        /// <summary>
        /// Verifies leading non-letters are skipped when finding the first letter.
        /// </summary>
        [Fact]
        public void UppercaseInitials_SkipsLeadingNonLetters()
        {
            Assert.Equal(
                "03 - With or without",
                SentenceInitialCasing.UppercaseInitials("03 - with or without", ' ', "-.!")
            );
        }

        /// <summary>
        /// Verifies sentence ends without a following word separator do not trigger uppercasing.
        /// </summary>
        [Fact]
        public void UppercaseInitials_RequiresSeparatorAfterSentenceEnd()
        {
            Assert.Equal("Hello.world", SentenceInitialCasing.UppercaseInitials("hello.world", ' ', ".!?"));
        }

        /// <summary>
        /// Verifies empty sentence-end characters only capitalize at the start.
        /// </summary>
        [Fact]
        public void UppercaseInitials_EmptySentenceEnds_CapitalizesOnlyStart()
        {
            Assert.Equal("Hello. next line", SentenceInitialCasing.UppercaseInitials("hello. next line", ' ', ""));
        }
    }
}
