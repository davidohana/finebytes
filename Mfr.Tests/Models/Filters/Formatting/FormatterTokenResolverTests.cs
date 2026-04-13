using Mfr.Filters.Formatting;
using Mfr.Tests.Models.Filters;

namespace Mfr.Tests.Models.Filters.Formatting
{
    /// <summary>
    /// Tests for <see cref="FormatterTokenResolver"/>.
    /// </summary>
    public sealed class FormatterTokenResolverTests
    {
        /// <summary>
        /// Verifies that multiple known tokens are replaced.
        /// </summary>
        [Fact]
        public void ResolveTemplate_KnownTokens_ReplacesValues()
        {
            var item = FilterTestHelpers.CreateFile(
                prefix: "song",
                extension: ".mp3",
                directory: @"C:\Music\My Album");

            var result = FormatterTokenResolver.ResolveTemplate(
                template: "<file-name><ext>-<parent-folder>",
                item: item);

            Assert.Equal("song.mp3-My Album", result);
        }

        /// <summary>
        /// Verifies counter token uses global index when reset flag is 0.
        /// </summary>
        [Fact]
        public void ResolveTemplate_CounterTokenGlobalIndex_UsesGlobalIndex()
        {
            var item = FilterTestHelpers.CreateFile(globalIndex: 2, inFolderIndex: 7);

            var result = FormatterTokenResolver.ResolveTemplate(
                template: "<counter:10,1,0,2,0>",
                item: item);

            Assert.Equal("12", result);
        }

        /// <summary>
        /// Verifies counter token uses in-folder index when reset flag is 1.
        /// </summary>
        [Fact]
        public void ResolveTemplate_CounterTokenFolderIndex_UsesInFolderIndex()
        {
            var item = FilterTestHelpers.CreateFile(globalIndex: 2, inFolderIndex: 7);

            var result = FormatterTokenResolver.ResolveTemplate(
                template: "<counter:10,1,1,2,0>",
                item: item);

            Assert.Equal("17", result);
        }

        /// <summary>
        /// Verifies invalid counter args throw.
        /// </summary>
        [Fact]
        public void ResolveTemplate_InvalidCounterArgs_Throws()
        {
            var item = FilterTestHelpers.CreateFile();

            var ex = Assert.Throws<InvalidOperationException>(() =>
                FormatterTokenResolver.ResolveTemplate(
                    template: "<counter:1,2>",
                    item: item));

            Assert.Contains("Invalid counter token arg", ex.Message);
        }

        /// <summary>
        /// Verifies unknown tokens throw.
        /// </summary>
        [Fact]
        public void ResolveTemplate_UnknownToken_Throws()
        {
            var item = FilterTestHelpers.CreateFile();

            var ex = Assert.Throws<NotSupportedException>(() =>
                FormatterTokenResolver.ResolveTemplate(
                    template: "<does-not-exist>",
                    item: item));

            Assert.Contains("not supported", ex.Message);
        }
    }
}
