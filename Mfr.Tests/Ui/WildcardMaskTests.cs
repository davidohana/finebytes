using Mfr.App.Ui.Services.FileExplorer;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Tests Explorer wildcard mask matching.
    /// </summary>
    public sealed class WildcardMaskTests
    {
        /// <summary>
        /// Verifies a blank pattern matches every file name.
        /// </summary>
        [Fact]
        public void Blank_Pattern_Matches_All()
        {
            Assert.True(WildcardMask.IsMatch("a.txt", null));
            Assert.True(WildcardMask.IsMatch("a.txt", string.Empty));
        }

        /// <summary>
        /// Verifies <c>*</c> and extension masks match case-insensitively.
        /// </summary>
        [Fact]
        public void Star_And_Extension_Masks_Match()
        {
            Assert.True(WildcardMask.IsMatch("Song.MP3", "*"));
            Assert.True(WildcardMask.IsMatch("Song.MP3", "*.mp3"));
            Assert.False(WildcardMask.IsMatch("Song.MP3", "*.txt"));
            Assert.True(WildcardMask.IsMatch("index.html", "*.htm*"));
        }

        /// <summary>
        /// Verifies exclude lists split on colon and semicolon.
        /// </summary>
        [Fact]
        public void MatchesAny_Splits_Colon_And_Semicolon()
        {
            Assert.True(WildcardMask.MatchesAny("a.tmp", "*.tmp;*.bak"));
            Assert.True(WildcardMask.MatchesAny("a.bak", "*.tmp:*.bak"));
            Assert.False(WildcardMask.MatchesAny("a.txt", "*.tmp;*.bak"));
        }
    }
}
