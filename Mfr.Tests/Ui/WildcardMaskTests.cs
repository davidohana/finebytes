using Mfr.App.Ui.Services.FileList;

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
        /// Verifies exclude lists match any pattern in the collection.
        /// </summary>
        [Fact]
        public void MatchesAny_Uses_Pattern_List()
        {
            Assert.True(WildcardMask.MatchesAny("a.tmp", ["*.tmp", "*.bak"]));
            Assert.True(WildcardMask.MatchesAny("a.bak", ["*.tmp", "*.bak"]));
            Assert.False(WildcardMask.MatchesAny("a.txt", ["*.tmp", "*.bak"]));
        }

        /// <summary>
        /// Verifies editor formatting and storage normalization round-trip (one mask per line).
        /// </summary>
        [Fact]
        public void Format_And_Normalize_Round_Trip()
        {
            var formatted = WildcardMask.FormatForEditor(["*.exe", "*.dll", "*.sys"]);
            Assert.Equal($"*.exe{Environment.NewLine}*.dll{Environment.NewLine}*.sys", formatted);
            Assert.Equal(["*.exe", "*.dll", "*.sys"], WildcardMask.NormalizeForStorage(formatted));
            Assert.Equal(["*.tmp", "*.bak"], WildcardMask.NormalizeForStorage("*.tmp\n*.bak"));
        }
    }
}
