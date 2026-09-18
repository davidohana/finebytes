using System.Globalization;
using Mfr.Filters.Formatting;
using Mfr.Filters.Formatting.Tokens.Pdf;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.Pdf
{
    /// <summary>
    /// Tests for <c>pdf-*</c> formatter tokens.
    /// </summary>
    public sealed class PdfDocumentTokenTests
    {
        private static PdfDocumentInfo _SamplePdf()
        {
            return new PdfDocumentInfo
            {
                Title = "Sample Title",
                Author = "Sample Author",
                Subject = "Sample Subject",
                Keywords = "alpha, beta",
                Creator = "Sample Creator App",
                Producer = "Sample Producer",
                Created = new DateTimeOffset(2024, 1, 15, 12, 30, 45, TimeSpan.Zero),
                Modified = new DateTimeOffset(2024, 2, 20, 14, 30, 0, TimeSpan.Zero),
                PageCount = 2,
            };
        }

        [Fact]
        public void Resolve_SeededFields_FormatPerRules()
        {
            var sample = _SamplePdf();
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m => m.Pdf = sample);

            Assert.Equal("Sample Title", new PdfTitleToken().Compile(string.Empty)(item));
            Assert.Equal("Sample Author", new PdfAuthorToken().Compile(string.Empty)(item));
            Assert.Equal("Sample Subject", new PdfSubjectToken().Compile(string.Empty)(item));
            Assert.Equal("alpha, beta", new PdfKeywordsToken().Compile(string.Empty)(item));
            Assert.Equal("Sample Creator App", new PdfCreatorToken().Compile(string.Empty)(item));
            Assert.Equal("Sample Producer", new PdfProducerToken().Compile(string.Empty)(item));
            Assert.Equal("2", new PdfPageCountToken().Compile(string.Empty)(item));
            Assert.Equal(
                sample.Created!.Value.ToString("G", CultureInfo.InvariantCulture),
                new PdfCreatedToken().Compile(string.Empty)(item)
            );
            Assert.Equal(
                sample.Modified!.Value.ToString("G", CultureInfo.InvariantCulture),
                new PdfModifiedToken().Compile(string.Empty)(item)
            );
        }

        [Fact]
        public void Resolve_MissingFields_YieldEmpty()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m => m.Pdf = new PdfDocumentInfo());

            Assert.Equal(string.Empty, new PdfTitleToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new PdfAuthorToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new PdfCreatedToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new PdfPageCountToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void Resolve_NullPdf_YieldsEmpty()
        {
            var item = FilterTestHelpers.CreateRenameItem();
            Assert.Null(item.Original.Pdf);

            Assert.Equal(string.Empty, new PdfTitleToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new PdfPageCountToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void Compile_WithAnyArgument_Throws()
        {
            var token = new PdfTitleToken();
            var item = FilterTestHelpers.CreateRenameItem();

            foreach (var bad in new[] { "0", "1", "x" })
            {
                var ex = Assert.Throws<ArgumentException>(() => token.Compile(tokenArgs: bad)(item));
                Assert.Contains("pdf-title", ex.Message, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void FormatterFilter_UsesSeededPdf()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
                m.Pdf = new PdfDocumentInfo { Title = "Report", PageCount = 3 }
            );

            var filter = new FormatterFilter(
                Target: new FileNameTarget(),
                Options: new FormatterOptions("<pdf-title>-<pdf-page-count>")
            );
            filter.Setup();
            filter.Apply(item);

            Assert.Equal("Report-3", item.Preview.FileName);
        }

        [Fact]
        public void EnsurePdfLoaded_ReadsFromDiskWhenNotMarked()
        {
            var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "tiny-info.pdf");
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
            Assert.False(item.PdfLoadAttempted);
            Assert.Null(item.Original.Pdf);

            var text = new PdfTitleToken().Compile(string.Empty)(item);

            Assert.True(item.PdfLoadAttempted);
            Assert.NotNull(item.Original.Pdf);
            Assert.Equal("Sample Title", text);
            Assert.Equal("2", new PdfPageCountToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void EnsurePdfLoaded_NonPdf_Throws()
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
            Assert.ThrowsAny<Exception>(() => new PdfTitleToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void EnsurePdfLoaded_Directory_Throws()
        {
            var item = FilterTestHelpers.CreateRenameItem(attributes: FileAttributes.Directory);

            var ex = Assert.Throws<InvalidOperationException>(() => new PdfTitleToken().Compile(string.Empty)(item));
            Assert.Contains("directory", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ClearPdfCache_ClearsSnapshot()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m => m.Pdf = _SamplePdf());

            Assert.NotNull(item.Original.Pdf);
            item.ClearPdfCache();

            Assert.False(item.PdfLoadAttempted);
            Assert.Null(item.Original.Pdf);
            Assert.Null(item.Preview.Pdf);
        }
    }
}
