using System.Globalization;
using Mfr.Filters.Formatting;
using Mfr.Filters.Formatting.FormatString;
using Mfr.Filters.Formatting.Tokens.Office;
using Mfr.Models.RenameList.Fields.Office;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.Office
{
    /// <summary>
    /// Tests for <c>office-*</c> formatter tokens.
    /// </summary>
    public sealed class OfficeDocumentTokenTests
    {
        [Fact]
        public void Catalog_OfficeTokens_UseDocumentOfficeGroupPath()
        {
            var officeEntries = FormatTokenCatalog
                .Entries.Where(e => e.CanonicalName.StartsWith("office-", StringComparison.Ordinal))
                .ToList();
            Assert.Equal(Enum.GetValues<OfficeDocumentField>().Length, officeEntries.Count);
            Assert.All(officeEntries, e => Assert.Equal("Document\\Office", e.GroupPath));
        }

        private static OfficeDocumentInfo _SampleOffice()
        {
            return new OfficeDocumentInfo
            {
                Title = "Sample Office Title",
                Author = "Sample Office Author",
                Subject = "Sample Office Subject",
                Keywords = "alpha, beta",
                Category = "Sample Category",
                Description = "Sample Office Description",
                LastModifiedBy = "Sample Last Modified By",
                Created = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero),
                Modified = new DateTimeOffset(2024, 2, 20, 14, 45, 0, TimeSpan.Zero),
            };
        }

        [Fact]
        public void Resolve_SeededFields_FormatPerRules()
        {
            var sample = _SampleOffice();
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m => m.Office = sample);

            Assert.Equal("Sample Office Title", new OfficeTitleToken().Compile(string.Empty)(item));
            Assert.Equal("Sample Office Author", new OfficeAuthorToken().Compile(string.Empty)(item));
            Assert.Equal("Sample Office Subject", new OfficeSubjectToken().Compile(string.Empty)(item));
            Assert.Equal("alpha, beta", new OfficeKeywordsToken().Compile(string.Empty)(item));
            Assert.Equal("Sample Category", new OfficeCategoryToken().Compile(string.Empty)(item));
            Assert.Equal("Sample Office Description", new OfficeDescriptionToken().Compile(string.Empty)(item));
            Assert.Equal("Sample Last Modified By", new OfficeLastModifiedByToken().Compile(string.Empty)(item));
            Assert.Equal(
                sample.Created!.Value.ToString("G", CultureInfo.InvariantCulture),
                new OfficeCreatedToken().Compile(string.Empty)(item)
            );
            Assert.Equal(
                sample.Modified!.Value.ToString("G", CultureInfo.InvariantCulture),
                new OfficeModifiedToken().Compile(string.Empty)(item)
            );
        }

        [Fact]
        public void Format_DateFields_TokenUsesInvariantDateTimeOffset_GridUsesFormatFileDateLocal()
        {
            var sample = _SampleOffice();
            foreach (
                var (field, value) in new (OfficeDocumentField Field, DateTimeOffset Value)[]
                {
                    (OfficeDocumentField.Created, sample.Created!.Value),
                    (OfficeDocumentField.Modified, sample.Modified!.Value),
                }
            )
            {
                var tokenText = OfficeDocumentInfoFormatting.Format(sample, field, PropertyDisplayContext.Token);
                var gridText = OfficeDocumentInfoFormatting.Format(sample, field, PropertyDisplayContext.Grid);

                Assert.Equal(value.ToString("G", CultureInfo.InvariantCulture), tokenText);
                Assert.Equal(RenameListFieldDisplay.FormatFileDate(value.LocalDateTime), gridText);
            }
        }

        [Fact]
        public void Resolve_MissingFields_YieldEmpty()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m => m.Office = new OfficeDocumentInfo());

            Assert.Equal(string.Empty, new OfficeTitleToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new OfficeAuthorToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new OfficeCreatedToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new OfficeLastModifiedByToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void Resolve_NullOffice_YieldsEmpty()
        {
            var item = FilterTestHelpers.CreateRenameItem();
            Assert.Null(item.Original.Office);

            Assert.Equal(string.Empty, new OfficeTitleToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new OfficeDescriptionToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void Compile_WithAnyArgument_Throws()
        {
            var token = new OfficeTitleToken();
            var item = FilterTestHelpers.CreateRenameItem();

            foreach (var bad in new[] { "0", "1", "x" })
            {
                var ex = Assert.Throws<ArgumentException>(() => token.Compile(tokenArgs: bad)(item));
                Assert.Contains("office-title", ex.Message, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void FormatterFilter_UsesSeededOffice()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
                m.Office = new OfficeDocumentInfo { Title = "Report", Author = "Author" }
            );

            var filter = new FormatterFilter(
                Target: new FileNameTarget(),
                Options: new FormatterOptions("<office-title>-<office-author>")
            );
            filter.Setup();
            filter.Apply(item);

            Assert.Equal("Report-Author", item.Preview.FileName);
        }

        [Fact]
        public void EnsureOfficeLoaded_ReadsFromDiskWhenNotMarked()
        {
            var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "tiny-info.docx");
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
            Assert.False(item.OfficeLoadAttempted);
            Assert.Null(item.Original.Office);

            var text = new OfficeTitleToken().Compile(string.Empty)(item);

            Assert.True(item.OfficeLoadAttempted);
            Assert.NotNull(item.Original.Office);
            Assert.Equal("Sample Office Title", text);
            Assert.Equal("Sample Office Author", new OfficeAuthorToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void EnsureOfficeLoaded_NonDocx_Throws()
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
            var ex = Assert.Throws<InvalidDataException>(() => new OfficeTitleToken().Compile(string.Empty)(item));
            Assert.Contains("DOCX", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void EnsureOfficeLoaded_Directory_Throws()
        {
            var item = FilterTestHelpers.CreateRenameItem(attributes: FileAttributes.Directory);

            var ex = Assert.Throws<InvalidOperationException>(() => new OfficeTitleToken().Compile(string.Empty)(item));
            Assert.Contains("directory", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ClearOfficeCache_ClearsSnapshot()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m => m.Office = _SampleOffice());

            Assert.NotNull(item.Original.Office);
            item.ClearOfficeCache();

            Assert.False(item.OfficeLoadAttempted);
            Assert.Null(item.Original.Office);
            Assert.Null(item.Preview.Office);
        }
    }
}
