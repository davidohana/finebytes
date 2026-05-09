using Mfr.Filters.Formatting;
using Mfr.Filters.Formatting.Tokens.Meta;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.Meta
{
    /// <summary>
    /// Tests for <see cref="TokenExtractToken"/> and its argument parsing.
    /// </summary>
    public sealed class TokenExtractTokenTests
    {
        private static readonly TokenExtractToken _token = new();

        private static string _Named(
            int tokenNumber,
            string separator,
            bool includeNext,
            bool includePrev,
            string source)
        {
            return $"tokenNumber={tokenNumber},separator={separator},includeNext={includeNext.ToString().ToLowerInvariant()}," +
            $"includePrev={includePrev.ToString().ToLowerInvariant()},source={source}";
        }

        // ── Basic extraction ──────────────────────────────────────────────────

        /// <summary>
        /// Verifies extracting the first part when split by a single-character separator.
        /// </summary>
        [Fact]
        public void Resolve_Token1_SingleCharSeparator_ReturnsFirstPart()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "13_-_Smog_-_Cold_Blooded_Old_Times",
                extension: ".mp3");
            Assert.Equal(
                "13_",
                _token.Compile(_Named(1, "-", false, false, "13_-_Smog_-_Cold_Blooded_Old_Times.mp3"))(item));
        }

        /// <summary>
        /// Verifies extracting an inner part by multi-character separator.
        /// </summary>
        [Fact]
        public void Resolve_Token2_MultiCharSeparator_ReturnsMiddlePart()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "13_-_Smog_-_Cold_Blooded_Old_Times",
                extension: ".mp3");
            Assert.Equal(
                "Smog",
                _token.Compile(_Named(2, "_-_", false, false, "13_-_Smog_-_Cold_Blooded_Old_Times.mp3"))(item));
        }

        /// <summary>
        /// Verifies extracting the last part by 1-based index.
        /// </summary>
        [Fact]
        public void Resolve_LastToken_ReturnsLastPart()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "a_b_c", extension: ".txt");
            Assert.Equal("c.txt", _token.Compile(_Named(3, "_", false, false, "a_b_c.txt"))(item));
        }

        // ── include-next ──────────────────────────────────────────────────────

        /// <summary>
        /// Verifies include-next returns all parts from the specified token to end, rejoined with separator.
        /// </summary>
        [Fact]
        public void Resolve_IncludeNext_ReturnsRightRemainder()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "13_-_Smog_-_Cold_Blooded_Old_Times",
                extension: ".mp3");
            Assert.Equal(
                "Smog_-_Cold_Blooded_Old_Times.mp3",
                _token.Compile(_Named(2, "_-_", true, false, "13_-_Smog_-_Cold_Blooded_Old_Times.mp3"))(item));
        }

        /// <summary>
        /// Verifies include-next on the first token returns the full source.
        /// </summary>
        [Fact]
        public void Resolve_IncludeNext_Token1_ReturnsFullSource()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "a_b_c", extension: ".txt");
            Assert.Equal("a_b_c.txt", _token.Compile(_Named(1, "_", true, false, "a_b_c.txt"))(item));
        }

        // ── include-prev ──────────────────────────────────────────────────────

        /// <summary>
        /// Verifies include-prev returns all parts from the start up to and including the token, rejoined.
        /// </summary>
        [Fact]
        public void Resolve_IncludePrev_ReturnsLeftPortion()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "a_b_c", extension: ".txt");
            Assert.Equal("a_b", _token.Compile(_Named(2, "_", false, true, "a_b_c.txt"))(item));
        }

        /// <summary>
        /// Verifies include-prev on the last token returns the full source.
        /// </summary>
        [Fact]
        public void Resolve_IncludePrev_LastToken_ReturnsFullSource()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "a_b_c", extension: ".txt");
            Assert.Equal("a_b_c.txt", _token.Compile(_Named(3, "_", false, true, "a_b_c.txt"))(item));
        }

        // ── both include flags ────────────────────────────────────────────────

        /// <summary>
        /// Verifies that setting both include flags returns the full source string.
        /// </summary>
        [Fact]
        public void Resolve_BothIncludeFlags_ReturnsFullSource()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "a_b_c", extension: ".txt");
            Assert.Equal("a_b_c.txt", _token.Compile(_Named(2, "_", true, true, "a_b_c.txt"))(item));
        }

        /// <summary>
        /// Verifies named options can appear in any order.
        /// </summary>
        [Fact]
        public void Resolve_NamedOptions_OrderIndependent()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "a_b_c", extension: ".txt");
            Assert.Equal(
                "b",
                _token.Compile(
                    "source=a_b_c.txt,separator=_,tokenNumber=2,includePrev=false,includeNext=false")(item));
        }

        // ── nested format string via FormatStringCompiler ─────────────────────

        /// <summary>
        /// Verifies the full nested token form resolves the inner <c>&lt;full-name&gt;</c> token first.
        /// </summary>
        [Fact]
        public void ResolveTemplate_NestedFullName_ResolvesInnerTokenFirst()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "13_-_Smog_-_Cold_Blooded_Old_Times",
                extension: ".mp3");
            var compiled = FormatStringCompiler.Compile(
                "<token:tokenNumber=1,separator=-,includeNext=false,includePrev=false,source=<full-name>>");
            Assert.Equal("13_", compiled(item));
        }

        /// <summary>
        /// Spec example: multi-char separator and nested full-name.
        /// </summary>
        [Fact]
        public void ResolveTemplate_Token2MultiSep_ExtractsArtist()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "13_-_Smog_-_Cold_Blooded_Old_Times",
                extension: ".mp3");
            var compiled = FormatStringCompiler.Compile(
                "<token:tokenNumber=2,separator=_-_,includeNext=false,includePrev=false,source=<full-name>>");
            Assert.Equal("Smog", compiled(item));
        }

        /// <summary>
        /// Spec example: include-next with nested full-name.
        /// </summary>
        [Fact]
        public void ResolveTemplate_Token2MultiSepIncludeNext_ExtractsRemainder()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "13_-_Smog_-_Cold_Blooded_Old_Times",
                extension: ".mp3");
            var compiled = FormatStringCompiler.Compile(
                "<token:tokenNumber=2,separator=_-_,includeNext=true,includePrev=false,source=<full-name>>");
            Assert.Equal(
                "Smog_-_Cold_Blooded_Old_Times.mp3",
                compiled(item));
        }

        // ── error cases ───────────────────────────────────────────────────────

        /// <summary>
        /// Verifies a missing argument string throws with a descriptive message.
        /// </summary>
        [Fact]
        public void Resolve_EmptyArg_Throws()
        {
            var item = FilterTestHelpers.CreateRenameItem();
            var ex = Assert.Throws<ArgumentException>(() => _token.Compile("")(item));
            Assert.Contains("<token>", ex.Message);
        }

        /// <summary>
        /// Verifies incomplete named arguments throw.
        /// </summary>
        [Fact]
        public void Resolve_IncompleteNamedArgs_Throws()
        {
            var item = FilterTestHelpers.CreateRenameItem();
            var ex = Assert.Throws<ArgumentException>(() => _token.Compile("tokenNumber=1,separator=-")(item));
            Assert.Contains("missing required option", ex.Message);
        }

        /// <summary>
        /// Verifies a token number of zero throws.
        /// </summary>
        [Fact]
        public void Resolve_TokenNumberZero_Throws()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "a_b", extension: ".txt");
            var ex = Assert.Throws<ArgumentException>(() =>
                _token.Compile(_Named(0, "_", false, false, "a_b.txt"))(item));
            Assert.Contains("1 or greater", ex.Message);
        }

        /// <summary>
        /// Verifies a token number that exceeds the part count throws.
        /// </summary>
        [Fact]
        public void Resolve_TokenNumberOutOfRange_Throws()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "a_b", extension: ".txt");
            var ex = Assert.Throws<ArgumentException>(() =>
                _token.Compile(_Named(5, "_", false, false, "a_b.txt"))(item));
            Assert.Contains("exceeds the number of parts", ex.Message);
        }

        /// <summary>
        /// Verifies numeric include flags are rejected.
        /// </summary>
        [Fact]
        public void Resolve_NumericIncludeFlag_Throws()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "a_b", extension: ".txt");
            var ex = Assert.Throws<ArgumentException>(() =>
                _token.Compile(
                    "tokenNumber=1,separator=_,includeNext=1,includePrev=false,source=a_b.txt")(item));
            Assert.Contains("includeNext", ex.Message);
        }

        [Fact]
        public void Resolve_EmptySeparator_Throws()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "abc", extension: ".txt");
            var ex = Assert.Throws<ArgumentException>(() =>
                _token.Compile(
                    "tokenNumber=1,separator=,includeNext=false,includePrev=false,source=abc.txt")(item));
            Assert.Contains("separator", ex.Message);
        }
    }
}
