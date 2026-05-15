using Mfr.Filters.Formatting;

namespace Mfr.Tests.Models.Filters.Formatting
{
    /// <summary>
    /// Tests for <see cref="FormatStringCompiler"/>: parsing, dispatch, token discovery, and compile output.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per-token behavior lives in the matching <c>XxxTokenTests</c> files under
    /// <c>Mfr.Tests/Models/Filters/Formatting/Tokens/</c>. This file focuses on the
    /// compiler's balanced-bracket template scanning, multi-token templates, error messages, and the
    /// reflection-based token discovery wiring.
    /// </para>
    /// </remarks>
    public sealed class FormatStringCompilerTests
    {
        /// <summary>
        /// Verifies that multiple tokens in one template are each replaced.
        /// </summary>
        [Fact]
        public void ResolveTemplate_MultipleTokens_ReplacesAll()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "song",
                extension: ".mp3",
                directory: @"C:\Music\My Album");

            var compiled = FormatStringCompiler.Compile(
                template: "<file-name><ext>-<parent-folder>");
            var result = compiled(item);

            Assert.Equal("song.mp3-My Album", result);
        }

        /// <summary>
        /// Verifies literal text between tokens is preserved.
        /// </summary>
        [Fact]
        public void ResolveTemplate_TokensMixedWithLiterals_PreservesLiterals()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "song", extension: ".mp3");

            var compiled = FormatStringCompiler.Compile(
                template: "Track: <file-name> [<ext>]");
            var result = compiled(item);

            Assert.Equal("Track: song [.mp3]", result);
        }

        /// <summary>
        /// Verifies a template without any tokens passes through unchanged.
        /// </summary>
        [Fact]
        public void ResolveTemplate_NoTokens_PassesThroughVerbatim()
        {
            var item = FilterTestHelpers.CreateRenameItem();

            var compiled = FormatStringCompiler.Compile("just text");
            var result = compiled(item);

            Assert.Equal("just text", result);
        }

        /// <summary>
        /// Verifies an unknown token name throws with a message that names the offender.
        /// </summary>
        [Fact]
        public void ResolveTemplate_UnknownToken_Throws()
        {
            var ex = Assert.Throws<NotSupportedException>(() =>
                FormatStringCompiler.Compile(template: "<does-not-exist>"));

            Assert.Contains("Unknown formatter token", ex.Message);
            Assert.Contains("does-not-exist", ex.Message);
        }

        /// <summary>
        /// Verifies the reflection-based discovery picks up every shipped token name (canonical and aliases).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Acts as a smoke test for the wiring in <see cref="FormatStringCompiler"/>: each name listed
        /// here must compile and run without throwing. Add a new entry whenever a token is added or aliased.
        /// </para>
        /// </remarks>
        [Theory]
        [InlineData("file-name")]
        [InlineData("file-extension")]
        [InlineData("ext")]
        [InlineData("full-name")]
        [InlineData("full-path")]
        [InlineData("parent-folder")]
        [InlineData("file-date:dd-MM-yyyy,creation")]
        [InlineData("file-size")]
        [InlineData("drive-letter")]
        [InlineData("label")]
        [InlineData("file-count")]
        [InlineData("item-count")]
        [InlineData("random-char:A,Z")]
        [InlineData("random-char:0,9")]
        [InlineData("counter")]
        [InlineData("counter:length=0,initial=1,step=1,padding=none,resetScope=global")]
        [InlineData("now")]
        public void ResolveTemplate_ShippedToken_ResolvesWithoutThrowing(string tokenInner)
        {
            var item = FilterTestHelpers.CreateRenameItem(directory: @"C:\Music\Album");

            var compiled = FormatStringCompiler.Compile($"<{tokenInner}>");
            var ex = Record.Exception(() => compiled(item));

            Assert.Null(ex);
        }

        /// <summary>
        /// Verifies <see cref="FormatStringCompiler.ContainsLikelyFormatTokens"/> detects real token spans.
        /// </summary>
        [Theory]
        [InlineData("<file-name>", true)]
        [InlineData("Pre <file-name> post", true)]
        [InlineData("<substr:source=x>", true)]
        [InlineData("plain text", false)]
        [InlineData("a < b", false)]
        [InlineData("a < b > c", false)]
        [InlineData("<3>", false)]
        [InlineData("<>", false)]
        public void ContainsLikelyFormatTokens_Classifies(string text, bool expected)
        {
            Assert.Equal(expected, FormatStringCompiler.ContainsLikelyFormatTokens(text));
        }
    }
}
