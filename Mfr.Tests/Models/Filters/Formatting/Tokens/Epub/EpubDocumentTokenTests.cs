using Mfr.Filters.Formatting;
using Mfr.Filters.Formatting.FormatString;
using Mfr.Filters.Formatting.Tokens.Epub;
using Mfr.Models.RenameList.Fields.Epub;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.Epub
{
    /// <summary>
    /// Tests for <c>epub-*</c> formatter tokens.
    /// </summary>
    public sealed class EpubDocumentTokenTests
    {
        private static EpubDocumentInfo _SampleEpub()
        {
            return new EpubDocumentInfo
            {
                Title = "Sample EPUB Title",
                Creator = "Sample EPUB Creator",
                Publisher = "Sample EPUB Publisher",
                Language = "en",
                Date = "2024-03-15",
                Identifier = "urn:uuid:sample-unique-id",
                Subject = "Sample EPUB Subject",
                Description = "Sample EPUB Description",
            };
        }

        /// <summary>
        /// Verifies catalog rows nest under <c>Document\Epub</c>.
        /// </summary>
        [Fact]
        public void Catalog_EpubTokens_UseDocumentEpubGroupPath()
        {
            var epubEntries = FormatTokenCatalog
                .Entries.Where(e => e.CanonicalName.StartsWith("epub-", StringComparison.Ordinal))
                .ToList();
            Assert.Equal(Enum.GetValues<EpubDocumentField>().Length, epubEntries.Count);
            Assert.All(epubEntries, e => Assert.Equal("Document\\Epub", e.GroupPath));
        }

        [Fact]
        public void Resolve_SeededFields_FormatPerRules()
        {
            var sample = _SampleEpub();
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m => m.Epub = sample);

            Assert.Equal("Sample EPUB Title", new EpubTitleToken().Compile(string.Empty)(item));
            Assert.Equal("Sample EPUB Creator", new EpubCreatorToken().Compile(string.Empty)(item));
            Assert.Equal("Sample EPUB Publisher", new EpubPublisherToken().Compile(string.Empty)(item));
            Assert.Equal("en", new EpubLanguageToken().Compile(string.Empty)(item));
            Assert.Equal("2024-03-15", new EpubDateToken().Compile(string.Empty)(item));
            Assert.Equal("urn:uuid:sample-unique-id", new EpubIdentifierToken().Compile(string.Empty)(item));
            Assert.Equal("Sample EPUB Subject", new EpubSubjectToken().Compile(string.Empty)(item));
            Assert.Equal("Sample EPUB Description", new EpubDescriptionToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void Format_AllFields_TokenAndGridMatch_NoDateFork()
        {
            var sample = _SampleEpub();

            foreach (var field in Enum.GetValues<EpubDocumentField>())
            {
                var tokenText = EpubDocumentInfoFormatting.Format(sample, field, PropertyDisplayContext.Token);
                var gridText = EpubDocumentInfoFormatting.Format(sample, field, PropertyDisplayContext.Grid);
                Assert.Equal(tokenText, gridText);
                Assert.False(string.IsNullOrEmpty(tokenText));
            }
        }

        [Fact]
        public void Resolve_MissingFields_YieldEmpty()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m => m.Epub = new EpubDocumentInfo());

            Assert.Equal(string.Empty, new EpubTitleToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new EpubCreatorToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new EpubDateToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new EpubIdentifierToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void Resolve_NullEpub_YieldsEmpty()
        {
            var item = FilterTestHelpers.CreateRenameItem();
            Assert.Null(item.Original.Epub);

            Assert.Equal(string.Empty, new EpubTitleToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new EpubDescriptionToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void Compile_WithAnyArgument_Throws()
        {
            var token = new EpubTitleToken();
            var item = FilterTestHelpers.CreateRenameItem();

            foreach (var bad in new[] { "0", "1", "x" })
            {
                var ex = Assert.Throws<ArgumentException>(() => token.Compile(tokenArgs: bad)(item));
                Assert.Contains("epub-title", ex.Message, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void FormatterFilter_UsesSeededEpub()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
                m.Epub = new EpubDocumentInfo { Title = "Report", Creator = "Author" }
            );

            var filter = new FormatterFilter(
                Target: new FileNameTarget(),
                Options: new FormatterOptions("<epub-title>-<epub-creator>")
            );
            filter.Setup();
            filter.Apply(item);

            Assert.Equal("Report-Author", item.Preview.FileName);
        }

        [Fact]
        public void EnsureEpubLoaded_ReadsFromDiskWhenNotMarked()
        {
            var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "tiny-info.epub");
            Assert.True(File.Exists(fixturePath), $"Missing fixture '{fixturePath}'.");

            var fullPath = Path.GetFullPath(fixturePath);
            var directory = Path.GetDirectoryName(fullPath)!;
            var prefix = Path.GetFileNameWithoutExtension(fullPath);
            var extension = FileMeta.ExtensionWithoutDot(fullPath);

            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: directory,
                fileName: prefix,
                extension: extension,
                fileSize: new FileInfo(fullPath).Length
            );

            var item = new RenameItem(meta);
            Assert.False(item.EpubLoadAttempted);
            Assert.Null(item.Original.Epub);

            var text = new EpubTitleToken().Compile(string.Empty)(item);

            Assert.True(item.EpubLoadAttempted);
            Assert.NotNull(item.Original.Epub);
            Assert.Equal("Sample EPUB Title", text);
            Assert.Equal("Sample EPUB Creator", new EpubCreatorToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void EnsureEpubLoaded_NonEpub_Throws()
        {
            var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "tiny.jpeg");
            Assert.True(File.Exists(fixturePath), $"Missing fixture '{fixturePath}'.");

            var fullPath = Path.GetFullPath(fixturePath);
            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: Path.GetDirectoryName(fullPath)!,
                fileName: Path.GetFileNameWithoutExtension(fullPath),
                extension: FileMeta.ExtensionWithoutDot(fullPath),
                fileSize: new FileInfo(fullPath).Length
            );

            var item = new RenameItem(meta);
            Assert.ThrowsAny<Exception>(() => new EpubTitleToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void EnsureEpubLoaded_Directory_Throws()
        {
            var item = FilterTestHelpers.CreateRenameItem(attributes: FileAttributes.Directory);

            var ex = Assert.Throws<InvalidOperationException>(() => new EpubTitleToken().Compile(string.Empty)(item));
            Assert.Contains("directory", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ClearEpubCache_ClearsSnapshot()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m => m.Epub = _SampleEpub());

            Assert.NotNull(item.Original.Epub);
            item.ClearEpubCache();

            Assert.False(item.EpubLoadAttempted);
            Assert.Null(item.Original.Epub);
            Assert.Null(item.Preview.Epub);
        }
    }
}
