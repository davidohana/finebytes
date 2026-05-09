using Mfr.Filters.Formatting;
using Mfr.Filters.Formatting.Tokens.General;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.General
{
    /// <summary>
    /// Tests for <see cref="TokenExtractToken"/> and its argument parsing.
    /// </summary>
    public sealed class TokenExtractTokenTests
    {
        private static readonly TokenExtractToken _token = new();

        // ── Basic extraction ──────────────────────────────────────────────────

        /// <summary>
        /// Verifies extracting the first part when split by a single-character separator.
        /// </summary>
        [Fact]
        public void Resolve_Token1_SingleCharSeparator_ReturnsFirstPart()
        {
            // "13_-_Smog_-_Cold_Blooded_Old_Times.mp3" split by "-" → ["13_", "_Smog_", "_Cold_Blooded_Old_Times.mp3"]
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "13_-_Smog_-_Cold_Blooded_Old_Times",
                extension: ".mp3");
            Assert.Equal("13_", _token.Compile("1,-,0,0,13_-_Smog_-_Cold_Blooded_Old_Times.mp3")(item));
        }

        /// <summary>
        /// Verifies extracting an inner part by multi-character separator.
        /// </summary>
        [Fact]
        public void Resolve_Token2_MultiCharSeparator_ReturnsMiddlePart()
        {
            // "13_-_Smog_-_Cold_Blooded_Old_Times.mp3" split by "_-_" → ["13", "Smog", "Cold_Blooded_Old_Times.mp3"]
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "13_-_Smog_-_Cold_Blooded_Old_Times",
                extension: ".mp3");
            Assert.Equal("Smog", _token.Compile("2,_-_,0,0,13_-_Smog_-_Cold_Blooded_Old_Times.mp3")(item));
        }

        /// <summary>
        /// Verifies extracting the last part by 1-based index.
        /// </summary>
        [Fact]
        public void Resolve_LastToken_ReturnsLastPart()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "a_b_c", extension: ".txt");
            Assert.Equal("c.txt", _token.Compile("3,_,0,0,a_b_c.txt")(item));
        }

        // ── include-next ──────────────────────────────────────────────────────

        /// <summary>
        /// Verifies include-next returns all parts from the specified token to end, rejoined with separator.
        /// </summary>
        [Fact]
        public void Resolve_IncludeNext_ReturnsRightRemainder()
        {
            // split by "_-_" → ["13", "Smog", "Cold_Blooded_Old_Times.mp3"]; from token 2 → rejoin
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "13_-_Smog_-_Cold_Blooded_Old_Times",
                extension: ".mp3");
            Assert.Equal(
                "Smog_-_Cold_Blooded_Old_Times.mp3",
                _token.Compile("2,_-_,1,0,13_-_Smog_-_Cold_Blooded_Old_Times.mp3")(item));
        }

        /// <summary>
        /// Verifies include-next on the first token returns the full source.
        /// </summary>
        [Fact]
        public void Resolve_IncludeNext_Token1_ReturnsFullSource()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "a_b_c", extension: ".txt");
            Assert.Equal("a_b_c.txt", _token.Compile("1,_,1,0,a_b_c.txt")(item));
        }

        // ── include-prev ──────────────────────────────────────────────────────

        /// <summary>
        /// Verifies include-prev returns all parts from the start up to and including the token, rejoined.
        /// </summary>
        [Fact]
        public void Resolve_IncludePrev_ReturnsLeftPortion()
        {
            // split by "_" → ["a", "b", "c.txt"]; up to token 2 → "a_b"
            var item = FilterTestHelpers.CreateRenameItem(prefix: "a_b_c", extension: ".txt");
            Assert.Equal("a_b", _token.Compile("2,_,0,1,a_b_c.txt")(item));
        }

        /// <summary>
        /// Verifies include-prev on the last token returns the full source.
        /// </summary>
        [Fact]
        public void Resolve_IncludePrev_LastToken_ReturnsFullSource()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "a_b_c", extension: ".txt");
            Assert.Equal("a_b_c.txt", _token.Compile("3,_,0,1,a_b_c.txt")(item));
        }

        // ── both include flags ────────────────────────────────────────────────

        /// <summary>
        /// Verifies that setting both include flags returns the full source string.
        /// </summary>
        [Fact]
        public void Resolve_BothIncludeFlags_ReturnsFullSource()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "a_b_c", extension: ".txt");
            Assert.Equal("a_b_c.txt", _token.Compile("2,_,1,1,a_b_c.txt")(item));
        }

        // ── nested format string via FormatStringResolver ─────────────────────

        /// <summary>
        /// Verifies the full nested token form resolves the inner <c>&lt;full-name&gt;</c> token first.
        /// Matches spec example: <c>&lt;token:1,-,0,0,&lt;full-name&gt;&gt;</c> → <c>13_</c>.
        /// </summary>
        [Fact]
        public void ResolveTemplate_NestedFullName_ResolvesInnerTokenFirst()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "13_-_Smog_-_Cold_Blooded_Old_Times",
                extension: ".mp3");
            Assert.Equal("13_", FormatStringResolver.ResolveTemplate("<token:1,-,0,0,<full-name>>", item));
        }

        /// <summary>
        /// Spec example 2: <c>&lt;token:2,_-_,0,0,&lt;full-name&gt;&gt;</c> → <c>Smog</c>.
        /// </summary>
        [Fact]
        public void ResolveTemplate_Token2MultiSep_ExtractsArtist()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "13_-_Smog_-_Cold_Blooded_Old_Times",
                extension: ".mp3");
            Assert.Equal("Smog", FormatStringResolver.ResolveTemplate("<token:2,_-_,0,0,<full-name>>", item));
        }

        /// <summary>
        /// Spec example 3: <c>&lt;token:2,_-_,1,0,&lt;full-name&gt;&gt;</c> → right remainder.
        /// </summary>
        [Fact]
        public void ResolveTemplate_Token2MultiSepIncludeNext_ExtractsRemainder()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "13_-_Smog_-_Cold_Blooded_Old_Times",
                extension: ".mp3");
            Assert.Equal(
                "Smog_-_Cold_Blooded_Old_Times.mp3",
                FormatStringResolver.ResolveTemplate("<token:2,_-_,1,0,<full-name>>", item));
        }

        // ── error cases ───────────────────────────────────────────────────────

        /// <summary>
        /// Verifies a missing argument string throws with a descriptive message.
        /// </summary>
        [Fact]
        public void Resolve_EmptyArg_Throws()
        {
            var item = FilterTestHelpers.CreateRenameItem();
            var ex = Assert.Throws<InvalidOperationException>(() => _token.Compile("")(item));
            Assert.Contains("<token>", ex.Message);
        }

        /// <summary>
        /// Verifies fewer than five comma-separated arguments throws.
        /// </summary>
        [Fact]
        public void Resolve_TooFewArgs_Throws()
        {
            var item = FilterTestHelpers.CreateRenameItem();
            var ex = Assert.Throws<InvalidOperationException>(() => _token.Compile("1,-,0,0")(item));
            Assert.Contains("5 comma-separated", ex.Message);
        }

        /// <summary>
        /// Verifies a token number of zero throws.
        /// </summary>
        [Fact]
        public void Resolve_TokenNumberZero_Throws()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "a_b", extension: ".txt");
            var ex = Assert.Throws<InvalidOperationException>(() => _token.Compile("0,_,0,0,a_b.txt")(item));
            Assert.Contains("1 or greater", ex.Message);
        }

        /// <summary>
        /// Verifies a token number that exceeds the part count throws.
        /// </summary>
        [Fact]
        public void Resolve_TokenNumberOutOfRange_Throws()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "a_b", extension: ".txt");
            var ex = Assert.Throws<InvalidOperationException>(() => _token.Compile("5,_,0,0,a_b.txt")(item));
            Assert.Contains("exceeds the number of parts", ex.Message);
        }

        /// <summary>
        /// Verifies an empty separator throws.
        /// </summary>
        [Fact]
        public void Resolve_EmptySeparator_Throws()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "abc", extension: ".txt");
            var ex = Assert.Throws<InvalidOperationException>(() => _token.Compile("1,,0,0,abc.txt")(item));
            Assert.Contains("separator", ex.Message);
        }
    }
}
