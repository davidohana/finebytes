using Mfr.Filters.Formatting;
using Mfr.Filters.Formatting.Tokens.Exif;
using Mfr.Filters.Formatting.Tokens.Image;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.Exif
{
    /// <summary>
    /// Tests for <c>exif-*</c> formatter tokens.
    /// </summary>
    public sealed class ExifPropertyTokenTests
    {
        private static ExifData _SampleExif()
        {
            return new ExifData
            {
                Make = "Canon",
                Model = "EOS 5D",
                Exposure = "1/60 sec",
                FNumber = "f/8.0",
                Iso = "100",
                FocalLength = "50 mm",
                FocalLength35mm = "50 mm",
                DateTaken = new DateTime(2020, 5, 15, 14, 30, 0, DateTimeKind.Unspecified),
                TagToDescription = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Exif/Make"] = "Canon",
                    ["Exif/271"] = "Canon",
                    ["ExifSub/Date/Time Original"] = "2020:05:15 14:30:00",
                    ["ExifSub/36867"] = "2020:05:15 14:30:00",
                },
            };
        }

        [Fact]
        public void Resolve_SeededFields_FormatPerRules()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m => m.Exif = _SampleExif());

            Assert.Equal("Canon", new ExifMakeToken().Compile(string.Empty)(item));
            Assert.Equal("EOS 5D", new ExifModelToken().Compile(string.Empty)(item));
            Assert.Equal("1/60 sec", new ExifExposureToken().Compile(string.Empty)(item));
            Assert.Equal("f/8.0", new ExifFNumberToken().Compile(string.Empty)(item));
            Assert.Equal("100", new ExifIsoToken().Compile(string.Empty)(item));
            Assert.Equal("50 mm", new ExifFocalToken().Compile(string.Empty)(item));
            Assert.Equal("50 mm", new ExifFocal35Token().Compile(string.Empty)(item));
            Assert.Equal("2020-05-15", new ExifDateToken().Compile("yyyy-MM-dd")(item));
            Assert.Equal("Canon", new ExifToken().Compile("Exif,Make")(item));
            Assert.Equal("Canon", new ExifToken().Compile("Exif,271")(item));
            Assert.Equal("2020:05:15 14:30:00", new ExifToken().Compile("ExifSub,36867")(item));
        }

        [Fact]
        public void Resolve_EmptyAndNullExif_YieldEmpty()
        {
            var emptyItem = FilterTestHelpers.CreateRenameItem(
                configureOriginal: m => m.Exif = new ExifData());
            var nullItem = FilterTestHelpers.CreateRenameItem();
            Assert.Null(nullItem.Original.Exif);

            Assert.Equal(string.Empty, new ExifMakeToken().Compile(string.Empty)(emptyItem));
            Assert.Equal(string.Empty, new ExifDateToken().Compile("yyyy")(emptyItem));
            Assert.Equal(string.Empty, new ExifToken().Compile("Exif,Make")(emptyItem));
            Assert.Equal(string.Empty, new ExifMakeToken().Compile(string.Empty)(nullItem));
            Assert.Equal(string.Empty, new ExifDateToken().Compile("yyyy-MM-dd")(nullItem));
            Assert.Equal(string.Empty, new ExifToken().Compile("Exif,Make")(nullItem));
        }

        [Fact]
        public void Compile_NoArgTokens_RejectArguments()
        {
            var token = new ExifMakeToken();
            var item = FilterTestHelpers.CreateRenameItem();

            foreach (var bad in new[] { "0", "1", "x" })
            {
                var ex = Assert.Throws<ArgumentException>(() => token.Compile(tokenArgs: bad)(item));
                Assert.Contains("exif-make", ex.Message, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void Compile_ExifDate_RejectsMissingArgs()
        {
            var token = new ExifDateToken();
            var item = FilterTestHelpers.CreateRenameItem();

            var ex = Assert.Throws<ArgumentException>(() => token.Compile(tokenArgs: "")(item));
            Assert.Contains("format", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Compile_Exif_RejectsMissingArgsAndUnknownSource()
        {
            var token = new ExifToken();
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m => m.Exif = _SampleExif());

            var missing = Assert.Throws<ArgumentException>(() => token.Compile(tokenArgs: "")(item));
            Assert.Contains("requires arguments", missing.Message, StringComparison.OrdinalIgnoreCase);

            var noComma = Assert.Throws<ArgumentException>(() => token.Compile(tokenArgs: "Exif")(item));
            Assert.Contains("comma", noComma.Message, StringComparison.OrdinalIgnoreCase);

            var unknown = Assert.Throws<ArgumentException>(() => token.Compile(tokenArgs: "Nope,Make")(item));
            Assert.Contains("invalid source", unknown.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Exif", unknown.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Resolve_Exif_UnknownTagName_YieldsEmpty()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m => m.Exif = _SampleExif());

            Assert.Equal(string.Empty, new ExifToken().Compile("Exif,NoSuchTag")(item));
        }

        [Fact]
        public void FormatterFilter_UsesSeededExif()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m => m.Exif = _SampleExif());

            var filter = new FormatterFilter(
                Target: new FilePrefixTarget(),
                Options: new FormatterOptions("<exif-make>_<exif-date:yyyy>"));
            filter.Setup();
            filter.Apply(item);

            Assert.Equal("Canon_2020", item.Preview.Prefix);
        }

        [Fact]
        public void EnsureImagePropertiesLoaded_ReadsExifFromDiskWhenNotMarked()
        {
            var item = _UnmarkedFixtureItem("tiny-exif.jpeg");
            Assert.False(item.ImagePropertiesLoadAttempted);
            Assert.Null(item.Original.Exif);

            var make = new ExifMakeToken().Compile(string.Empty)(item);

            Assert.True(item.ImagePropertiesLoadAttempted);
            Assert.NotNull(item.Original.Exif);
            Assert.NotNull(item.Original.Image);
            Assert.Equal("Canon", make);
            Assert.Equal("2020", new ExifDateToken().Compile("yyyy")(item));
        }

        [Fact]
        public void EnsureImagePropertiesLoaded_TinyJpegExifTokens_YieldEmptyNotError()
        {
            var item = _UnmarkedFixtureItem("tiny.jpeg");

            Assert.Equal(string.Empty, new ExifMakeToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new ExifDateToken().Compile("yyyy")(item));
            Assert.NotNull(item.Original.Image);
            Assert.NotNull(item.Original.Exif);
        }

        [Fact]
        public void EnsureImagePropertiesLoaded_Directory_Throws()
        {
            var item = FilterTestHelpers.CreateRenameItem(attributes: FileAttributes.Directory);

            var ex = Assert.Throws<InvalidOperationException>(
                () => new ExifMakeToken().Compile(string.Empty)(item));
            Assert.Contains("directory", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void AfterImageWidthLoad_ExifMakeUsesSameCache()
        {
            var item = _UnmarkedFixtureItem("tiny-exif.jpeg");
            Assert.False(item.ImagePropertiesLoadAttempted);

            var width = new ImageWidthToken().Compile(string.Empty)(item);

            Assert.True(item.ImagePropertiesLoadAttempted);
            Assert.NotNull(item.Original.Image);
            Assert.NotNull(item.Original.Exif);
            Assert.Equal("8", width);
            Assert.Equal("Canon", new ExifMakeToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void ClearImagePropertiesCache_ClearsExif()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                configureOriginal: m =>
                {
                    m.Image = new ImageProperties { Width = 8 };
                    m.Exif = _SampleExif();
                });

            item.ClearImagePropertiesCache();

            Assert.False(item.ImagePropertiesLoadAttempted);
            Assert.Null(item.Original.Image);
            Assert.Null(item.Preview.Image);
            Assert.Null(item.Original.Exif);
            Assert.Null(item.Preview.Exif);
        }

        private static RenameItem _UnmarkedFixtureItem(string fileName)
        {
            var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
            Assert.True(File.Exists(fixturePath), $"Missing fixture '{fixturePath}'.");

            var fullPath = Path.GetFullPath(fixturePath);
            var directory = Path.GetDirectoryName(fullPath)!;
            var prefix = Path.GetFileNameWithoutExtension(fullPath);
            var extension = Path.GetExtension(fullPath);

            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: directory,
                prefix: prefix,
                extension: extension,
                fileSize: new FileInfo(fullPath).Length);

            return new RenameItem(meta);
        }
    }
}
