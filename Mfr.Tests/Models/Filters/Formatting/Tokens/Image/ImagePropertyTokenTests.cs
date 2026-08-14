using Mfr.Filters.Formatting;
using Mfr.Filters.Formatting.Tokens.Image;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.Image
{
    /// <summary>
    /// Tests for <c>image-*</c> formatter tokens.
    /// </summary>
    public sealed class ImagePropertyTokenTests
    {
        private static ImageProperties _SampleImage()
        {
            return new ImageProperties
            {
                Format = "JPEG",
                Width = 1920,
                Height = 1080,
                BitDepth = 24,
                HorizontalResolutionDpi = 96,
                VerticalResolutionDpi = 72.009,
                FrameCount = 1,
            };
        }

        [Fact]
        public void Resolve_SeededFields_FormatPerRules()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m => m.Image = _SampleImage());

            Assert.Equal("1920", new ImageWidthToken().Compile(string.Empty)(item));
            Assert.Equal("1080", new ImageHeightToken().Compile(string.Empty)(item));
            Assert.Equal("24", new ImageBitDepthToken().Compile(string.Empty)(item));
            Assert.Equal("JPEG", new ImageFormatToken().Compile(string.Empty)(item));
            Assert.Equal("96", new ImageHorzResToken().Compile(string.Empty)(item));
            Assert.Equal("72.009", new ImageVertResToken().Compile(string.Empty)(item));
            Assert.Equal("1", new ImageFrameCountToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void Resolve_ZeroIntsAndDpi_YieldEmpty()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                configureOriginal: m => m.Image = new ImageProperties());

            Assert.Equal(string.Empty, new ImageWidthToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new ImageHeightToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new ImageBitDepthToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new ImageFormatToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new ImageHorzResToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new ImageVertResToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new ImageFrameCountToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void Resolve_NullImage_YieldsEmpty()
        {
            var item = FilterTestHelpers.CreateRenameItem();
            Assert.Null(item.Original.Image);

            Assert.Equal(string.Empty, new ImageWidthToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new ImageFormatToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new ImageHorzResToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void Compile_WithAnyArgument_Throws()
        {
            var token = new ImageWidthToken();
            var item = FilterTestHelpers.CreateRenameItem();

            foreach (var bad in new[] { "0", "1", "x" })
            {
                var ex = Assert.Throws<ArgumentException>(() => token.Compile(tokenArgs: bad)(item));
                Assert.Contains("image-width", ex.Message, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void FormatterFilter_UsesSeededImage()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                configureOriginal: m => m.Image = new ImageProperties
                {
                    Width = 2048,
                    Height = 1536,
                    Format = "JPEG",
                });

            var filter = new FormatterFilter(
                Target: new FilePrefixTarget(),
                Options: new FormatterOptions("<image-width>x<image-height>.<image-format>"));
            filter.Setup();
            filter.Apply(item);

            Assert.Equal("2048x1536.JPEG", item.Preview.Prefix);
        }

        [Fact]
        public void EnsureImagePropertiesLoaded_ReadsFromDiskWhenNotMarked()
        {
            var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "tiny.jpeg");
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

            var item = new RenameItem(meta);
            Assert.False(item.ImagePropertiesLoadAttempted);
            Assert.Null(item.Original.Image);

            var text = new ImageWidthToken().Compile(string.Empty)(item);

            Assert.True(item.ImagePropertiesLoadAttempted);
            Assert.NotNull(item.Original.Image);
            Assert.Equal("8", text);
            Assert.Equal("JPEG", new ImageFormatToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void EnsureImagePropertiesLoaded_Directory_Throws()
        {
            var item = FilterTestHelpers.CreateRenameItem(attributes: FileAttributes.Directory);

            var ex = Assert.Throws<InvalidOperationException>(
                () => new ImageWidthToken().Compile(string.Empty)(item));
            Assert.Contains("directory", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ClearImagePropertiesCache_ClearsSnapshot()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                configureOriginal: m => m.Image = _SampleImage());

            Assert.NotNull(item.Original.Image);
            item.ClearImagePropertiesCache();

            Assert.False(item.ImagePropertiesLoadAttempted);
            Assert.Null(item.Original.Image);
            Assert.Null(item.Preview.Image);
            Assert.Null(item.Original.Exif);
            Assert.Null(item.Preview.Exif);
        }
    }
}
