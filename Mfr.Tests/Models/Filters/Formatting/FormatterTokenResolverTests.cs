using System.Globalization;
using Mfr.Filters.Formatting;

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
            var item = FilterTestHelpers.CreateRenameItem(
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
            var item = FilterTestHelpers.CreateRenameItem(globalIndex: 2, inFolderIndex: 7);

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
            var item = FilterTestHelpers.CreateRenameItem(globalIndex: 2, inFolderIndex: 7);

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
            var item = FilterTestHelpers.CreateRenameItem();

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
            var item = FilterTestHelpers.CreateRenameItem();

            var ex = Assert.Throws<NotSupportedException>(() =>
                FormatterTokenResolver.ResolveTemplate(
                    template: "<does-not-exist>",
                    item: item));

            Assert.Contains("not supported", ex.Message);
        }

        /// <summary>
        /// Verifies <c>&lt;file-ext&gt;</c> matches documented <c>&lt;ext&gt;</c> behavior.
        /// </summary>
        [Fact]
        public void ResolveTemplate_FileExtToken_MatchesExtension()
        {
            var item = FilterTestHelpers.CreateRenameItem(extension: ".flac");

            var ext = FormatterTokenResolver.ResolveTemplate("<ext>", item);
            var fileExt = FormatterTokenResolver.ResolveTemplate("<file-ext>", item);

            Assert.Equal(".flac", ext);
            Assert.Equal(".flac", fileExt);
        }

        /// <summary>
        /// Verifies <c>&lt;full-name&gt;</c> is prefix plus extension.
        /// </summary>
        [Fact]
        public void ResolveTemplate_FullNameToken_ConcatenatesPrefixAndExtension()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "a", extension: ".b");

            var result = FormatterTokenResolver.ResolveTemplate("<full-name>", item);

            Assert.Equal("a.b", result);
        }

        /// <summary>
        /// Verifies <c>&lt;full-path&gt;</c> matches combined directory and file name.
        /// </summary>
        [Fact]
        public void ResolveTemplate_FullPathToken_MatchesFilePath()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "song",
                extension: ".mp3",
                directory: @"D:\Music\Album");

            var result = FormatterTokenResolver.ResolveTemplate("<full-path>", item);

            Assert.Equal(item.Original.FullPath, result);
        }

        /// <summary>
        /// Verifies <c>&lt;now&gt;</c> resolves to a round-trip ISO 8601 UTC string.
        /// </summary>
        [Fact]
        public void ResolveTemplate_NowToken_ProducesParseableUtcString()
        {
            var item = FilterTestHelpers.CreateRenameItem();

            var result = FormatterTokenResolver.ResolveTemplate("<now>", item);

            Assert.True(
                DateTimeOffset.TryParse(
                    result,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var parsed));
            Assert.Equal(DateTimeKind.Utc, parsed.UtcDateTime.Kind);
        }

        /// <summary>
        /// Verifies <c>&lt;now:format&gt;</c> uses the format segment after the first colon.
        /// </summary>
        [Fact]
        public void ResolveTemplate_NowWithFormat_UsesSuppliedFormat()
        {
            var item = FilterTestHelpers.CreateRenameItem();

            var result = FormatterTokenResolver.ResolveTemplate("<now:yyyy>", item);
            var expectedYear = DateTimeOffset.UtcNow.ToString("yyyy", CultureInfo.InvariantCulture);

            Assert.Equal(expectedYear, result);
        }

        /// <summary>
        /// Verifies counter token pad mode <c>1</c> uses spaces (matches formatter docs).
        /// </summary>
        [Fact]
        public void ResolveTemplate_CounterTokenPadModeSpace_PadsWithSpaces()
        {
            var item = FilterTestHelpers.CreateRenameItem(globalIndex: 0, inFolderIndex: 0);

            var result = FormatterTokenResolver.ResolveTemplate(
                template: "<counter:7,1,0,4,1>",
                item: item);

            Assert.Equal("   7", result);
        }
    }
}
