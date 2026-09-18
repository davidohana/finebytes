using Mfr.Metadata;

namespace Mfr.Tests.Metadata
{
    /// <summary>
    /// Tests for <see cref="PdfFileReader"/>.
    /// </summary>
    public sealed class PdfFileReaderTests
    {
        [Fact]
        public void Read_InfoFixture_FillsFieldsAndPageCount()
        {
            var path = _RequireFixture("tiny-info.pdf");

            var pdf = PdfFileReader.Read(path);

            Assert.Equal("Sample Title", pdf.Title);
            Assert.Equal("Sample Author", pdf.Author);
            Assert.Equal("Sample Subject", pdf.Subject);
            Assert.Equal("alpha, beta", pdf.Keywords);
            Assert.Equal("Sample Creator App", pdf.Creator);
            Assert.Equal("Sample Producer", pdf.Producer);
            Assert.Equal(2, pdf.PageCount);
            Assert.NotNull(pdf.Created);
            Assert.NotNull(pdf.Modified);
            Assert.Equal(2024, pdf.Created.Value.Year);
            Assert.Equal(1, pdf.Created.Value.Month);
            Assert.Equal(15, pdf.Created.Value.Day);
            Assert.Equal(2024, pdf.Modified.Value.Year);
            Assert.Equal(2, pdf.Modified.Value.Month);
            Assert.Equal(20, pdf.Modified.Value.Day);
        }

        [Fact]
        public void Read_EmptyInfoFixture_LeavesFieldsEmpty()
        {
            var path = _RequireFixture("tiny-empty-info.pdf");

            var pdf = PdfFileReader.Read(path);

            Assert.Null(pdf.Title);
            Assert.Null(pdf.Author);
            Assert.Null(pdf.Subject);
            Assert.Null(pdf.Keywords);
            Assert.Null(pdf.Creator);
            Assert.Null(pdf.Producer);
            Assert.Null(pdf.Created);
            Assert.Null(pdf.Modified);
            Assert.Equal(1, pdf.PageCount);
        }

        [Fact]
        public void Read_NonPdf_Throws()
        {
            var path = _RequireFixture("tiny.jpeg");

            var ex = Assert.ThrowsAny<Exception>(() => PdfFileReader.Read(path));
            Assert.Equal("PdfDocumentFormatException", ex.GetType().Name);
        }

        [Fact]
        public void Read_MissingPath_ThrowsArgumentException()
        {
            var missing = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "missing.pdf");
            var ex = Assert.Throws<ArgumentException>(() => PdfFileReader.Read(missing));
            Assert.Contains("exist", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        private static string _RequireFixture(string fileName)
        {
            var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
            if (!File.Exists(fixturePath))
            {
                throw new InvalidOperationException(
                    $"Missing fixture '{fixturePath}'. Run build so Fixtures copy to output."
                );
            }

            return Path.GetFullPath(fixturePath);
        }
    }
}
