using Mfr.Metadata;

namespace Mfr.Tests.Metadata
{
    /// <summary>
    /// Tests for <see cref="OfficeFileReader"/>.
    /// </summary>
    public sealed class OfficeFileReaderTests
    {
        [Fact]
        public void Read_InfoFixture_FillsFields()
        {
            var path = FixturePaths.Require("tiny-info.docx");

            var office = OfficeFileReader.Read(path);

            Assert.Equal("Sample Office Title", office.Title);
            Assert.Equal("Sample Office Author", office.Author);
            Assert.Equal("Sample Office Subject", office.Subject);
            Assert.Equal("alpha, beta", office.Keywords);
            Assert.Equal("Sample Category", office.Category);
            Assert.Equal("Sample Office Description", office.Description);
            Assert.Equal("Sample Last Modified By", office.LastModifiedBy);
            Assert.NotNull(office.Created);
            Assert.NotNull(office.Modified);
            Assert.Equal(2024, office.Created.Value.Year);
            Assert.Equal(1, office.Created.Value.Month);
            Assert.Equal(15, office.Created.Value.Day);
            Assert.Equal(2024, office.Modified.Value.Year);
            Assert.Equal(2, office.Modified.Value.Month);
            Assert.Equal(20, office.Modified.Value.Day);
        }

        [Fact]
        public void Read_EmptyInfoFixture_LeavesFieldsEmpty()
        {
            var path = FixturePaths.Require("tiny-empty-info.docx");

            var office = OfficeFileReader.Read(path);

            Assert.Null(office.Title);
            Assert.Null(office.Author);
            Assert.Null(office.Subject);
            Assert.Null(office.Keywords);
            Assert.Null(office.Category);
            Assert.Null(office.Description);
            Assert.Null(office.LastModifiedBy);
            Assert.Null(office.Created);
            Assert.Null(office.Modified);
        }

        [Fact]
        public void Read_NonDocx_Throws()
        {
            var path = FixturePaths.Require("tiny.jpeg");

            Assert.Throws<InvalidDataException>(() => OfficeFileReader.Read(path));
        }

        [Fact]
        public void Read_CorruptDocx_Throws()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            try
            {
                var path = Path.Combine(tempDir, "corrupt.docx");
                File.WriteAllText(path, "not-a-zip-or-opc-package");

                var ex = Assert.ThrowsAny<Exception>(() => OfficeFileReader.Read(path));
                Assert.True(
                    ex.GetType().Name is "OpenXmlPackageException" or "FileFormatException",
                    $"Unexpected exception type: {ex.GetType().FullName}"
                );
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }

        [Fact]
        public void Read_MissingPath_ThrowsArgumentException()
        {
            var missing = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "missing.docx");
            var ex = Assert.Throws<ArgumentException>(() => OfficeFileReader.Read(missing));
            Assert.Contains("exist", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
