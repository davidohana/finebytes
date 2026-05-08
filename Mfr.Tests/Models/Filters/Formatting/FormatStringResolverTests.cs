using Mfr.Filters.Formatting;

namespace Mfr.Tests.Models.Filters.Formatting
{
    /// <summary>
    /// Resolver-level tests for <see cref="FormatStringResolver"/>: parsing, dispatch, and discovery.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per-token behavior lives in the matching <c>XxxTokenTests</c> files under
    /// <c>Mfr.Tests/Models/Filters/Formatting/Tokens/</c>. This file focuses on the
    /// resolver's regex parsing, multi-token templates, error messages, and the
    /// reflection-based token discovery wiring.
    /// </para>
    /// </remarks>
    public sealed class FormatStringResolverTests
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

            var result = FormatStringResolver.ResolveTemplate(
                template: "<file-name><ext>-<parent-folder>",
                item: item);

            Assert.Equal("song.mp3-My Album", result);
        }

        /// <summary>
        /// Verifies literal text between tokens is preserved.
        /// </summary>
        [Fact]
        public void ResolveTemplate_TokensMixedWithLiterals_PreservesLiterals()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "song", extension: ".mp3");

            var result = FormatStringResolver.ResolveTemplate(
                template: "Track: <file-name> [<ext>]",
                item: item);

            Assert.Equal("Track: song [.mp3]", result);
        }

        /// <summary>
        /// Verifies a template without any tokens passes through unchanged.
        /// </summary>
        [Fact]
        public void ResolveTemplate_NoTokens_PassesThroughVerbatim()
        {
            var item = FilterTestHelpers.CreateRenameItem();

            var result = FormatStringResolver.ResolveTemplate("just text", item);

            Assert.Equal("just text", result);
        }

        /// <summary>
        /// Verifies an unknown token name throws with a message that names the offender.
        /// </summary>
        [Fact]
        public void ResolveTemplate_UnknownToken_Throws()
        {
            var item = FilterTestHelpers.CreateRenameItem();

            var ex = Assert.Throws<NotSupportedException>(() =>
                FormatStringResolver.ResolveTemplate(
                    template: "<does-not-exist>",
                    item: item));

            Assert.Contains("Unknown formatter token", ex.Message);
            Assert.Contains("does-not-exist", ex.Message);
        }

        /// <summary>
        /// Verifies the reflection-based discovery picks up every shipped token name (canonical and aliases).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Acts as a smoke test for the wiring in <see cref="FormatStringResolver"/>: each name listed
        /// here must resolve without throwing. Add a new entry whenever a token is added or aliased.
        /// </para>
        /// </remarks>
        [Theory]
        [InlineData("file-name")]
        [InlineData("file-extension")]
        [InlineData("ext")]
        [InlineData("full-name")]
        [InlineData("full-path")]
        [InlineData("parent-folder")]
        [InlineData("file-date")]
        [InlineData("file-size")]
        [InlineData("drive-letter")]
        [InlineData("label")]
        [InlineData("file-count")]
        [InlineData("item-count")]
        [InlineData("counter")]
        [InlineData("counter:1,1,0,0,0")]
        [InlineData("now")]
        public void ResolveTemplate_ShippedToken_ResolvesWithoutThrowing(string tokenInner)
        {
            var item = FilterTestHelpers.CreateRenameItem(directory: @"C:\Music\Album");

            var ex = Record.Exception(
                () => FormatStringResolver.ResolveTemplate($"<{tokenInner}>", item));

            Assert.Null(ex);
        }
    }
}
