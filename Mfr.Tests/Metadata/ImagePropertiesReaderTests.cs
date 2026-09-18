using Mfr.Metadata;

namespace Mfr.Tests.Metadata
{
    /// <summary>
    /// Tests for <see cref="ImagePropertiesReader"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// HEIF fixture <c>tiny.heic</c> is Crest Convert <c>pampas-grass-small.heic</c> (CC0 1.0, SHA-256
    /// <c>bb24331d3d5b7c54a4e98faf40fe465be524f83f841076c63b699c6a74fe67bf</c>); <c>.heif</c> coverage
    /// uses a temp copy of the same bytes. <c>tiny-exif.heic</c> is <c>samplefilehub.heif</c> from
    /// <c>ianare/exif-samples</c> (<c>heic/samplefilehub.heif</c>; CC BY-SA 4.0, SHA-256
    /// <c>f86ec0d3a6c82e31657bb1886e1ec95579329fa98d8be511ac1e8497c778e07f</c>).
    /// </para>
    /// </remarks>
    public sealed class ImagePropertiesReaderTests
    {
        [Fact]
        public void Read_JpegFixture_MapsRasterFields()
        {
            var path = FixturePaths.Require("tiny.jpeg");

            var image = ImagePropertiesReader.Read(path);

            Assert.Equal("JPEG", image.Format);
            Assert.Equal(8, image.Width);
            Assert.Equal(6, image.Height);
            Assert.Equal(24, image.BitDepth);
            Assert.Equal(72, image.HorizontalResolutionDpi);
            Assert.Equal(72, image.VerticalResolutionDpi);
            Assert.Equal(1, image.FrameCount);
        }

        [Fact]
        public void Read_PngFixture_MapsIhdrAndPhys()
        {
            var path = FixturePaths.Require("tiny.png");

            var image = ImagePropertiesReader.Read(path);

            Assert.Equal("PNG", image.Format);
            Assert.Equal(4, image.Width);
            Assert.Equal(3, image.Height);
            Assert.Equal(24, image.BitDepth);
            Assert.Equal(72.009, image.HorizontalResolutionDpi, precision: 3);
            Assert.Equal(72.009, image.VerticalResolutionDpi, precision: 3);
            Assert.Equal(1, image.FrameCount);
        }

        [Fact]
        public void Read_AnimatedGifFixture_FrameCountGreaterThanOne()
        {
            var path = FixturePaths.Require("tiny-animated.gif");

            var image = ImagePropertiesReader.Read(path);

            Assert.Equal("GIF", image.Format);
            Assert.Equal(1, image.Width);
            Assert.Equal(1, image.Height);
            Assert.True(image.FrameCount > 1);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Read_HeifFixture_MapsDimensionsAndFormat(bool useHeifExtension)
        {
            using var tempHeif = useHeifExtension ? HeifFixtures.CopyTinyAsHeif() : null;
            var path = tempHeif?.FullPath ?? HeifFixtures.TinyHeicPath;

            var image = ImagePropertiesReader.Read(path);

            Assert.Equal("HEIF", image.Format);
            Assert.Equal(600, image.Width);
            Assert.Equal(800, image.Height);
            Assert.Equal(24, image.BitDepth);
            Assert.Equal(1, image.FrameCount);
        }

        [Fact]
        public void Read_HeifExifFixture_MapsPrimaryDimensions()
        {
            var path = FixturePaths.Require("tiny-exif.heic");

            var image = ImagePropertiesReader.Read(path);

            Assert.Equal("HEIF", image.Format);
            Assert.Equal(640, image.Width);
            Assert.Equal(426, image.Height);
            Assert.Equal(24, image.BitDepth);
            Assert.Equal(72, image.HorizontalResolutionDpi);
            Assert.Equal(72, image.VerticalResolutionDpi);
            Assert.Equal(1, image.FrameCount);
        }

        [Fact]
        public void Read_PngTempWithPhys_ConvertsMetresToDpi()
        {
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
            try
            {
                File.WriteAllBytes(path, TinyPngBuilder.BuildRgb(2, 2, pixelsPerMetreX: 2835, pixelsPerMetreY: 2835));

                var image = ImagePropertiesReader.Read(path);

                Assert.Equal("PNG", image.Format);
                Assert.Equal(2, image.Width);
                Assert.Equal(2, image.Height);
                Assert.Equal(24, image.BitDepth);
                Assert.Equal(72.009, image.HorizontalResolutionDpi, precision: 3);
                Assert.Equal(1, image.FrameCount);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Read_BmpTemp_MapsBitDepthAndDpi()
        {
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.bmp");
            try
            {
                File.WriteAllBytes(path, _Build24BitBmp(width: 2, height: 1, pixelsPerMetre: 3780));

                var image = ImagePropertiesReader.Read(path);

                Assert.Equal("BMP", image.Format);
                Assert.Equal(2, image.Width);
                Assert.Equal(1, image.Height);
                Assert.Equal(24, image.BitDepth);
                Assert.Equal(96.012, image.HorizontalResolutionDpi, precision: 3);
                Assert.Equal(1, image.FrameCount);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Read_MissingFile_ThrowsArgumentException()
        {
            var missing = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}_missing.jpeg");

            var ex = Assert.Throws<ArgumentException>(() => ImagePropertiesReader.Read(missing));
            Assert.Contains("does not exist", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Read_Directory_ThrowsArgumentException()
        {
            var dir = Path.GetTempPath();

            var ex = Assert.Throws<ArgumentException>(() => ImagePropertiesReader.Read(Path.GetFullPath(dir)));
            Assert.Contains("directory", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Read_RelativePath_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => ImagePropertiesReader.Read("relative.jpeg"));
        }

        [Fact]
        public void Read_PlainText_Throws()
        {
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}_plain.txt");
            try
            {
                File.WriteAllText(path, "not an image");
                Assert.ThrowsAny<Exception>(() => ImagePropertiesReader.Read(path));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Read_WavFixture_ThrowsInvalidOperationException()
        {
            var path = FixturePaths.Require("minimal-silent.wav");

            var ex = Assert.Throws<InvalidOperationException>(() => ImagePropertiesReader.Read(path));
            Assert.Contains("WAV", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Read_Mp3Fixture_ThrowsInvalidOperationException()
        {
            var path = FixturePaths.Require("l3-compl-cut.mp3");

            var ex = Assert.Throws<InvalidOperationException>(() => ImagePropertiesReader.Read(path));
            Assert.Contains("MP3", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        private static byte[] _Build24BitBmp(int width, int height, int pixelsPerMetre)
        {
            var rowStride = ((width * 3) + 3) & ~3;
            var pixelBytes = rowStride * height;
            var fileSize = 54 + pixelBytes;
            var bytes = new byte[fileSize];

            bytes[0] = (byte)'B';
            bytes[1] = (byte)'M';
            BitConverter.TryWriteBytes(bytes.AsSpan(2), fileSize);
            BitConverter.TryWriteBytes(bytes.AsSpan(10), 54);
            BitConverter.TryWriteBytes(bytes.AsSpan(14), 40);
            BitConverter.TryWriteBytes(bytes.AsSpan(18), width);
            BitConverter.TryWriteBytes(bytes.AsSpan(22), height);
            BitConverter.TryWriteBytes(bytes.AsSpan(26), (short)1);
            BitConverter.TryWriteBytes(bytes.AsSpan(28), (short)24);
            BitConverter.TryWriteBytes(bytes.AsSpan(38), pixelsPerMetre);
            BitConverter.TryWriteBytes(bytes.AsSpan(42), pixelsPerMetre);

            for (var y = 0; y < height; y++)
            {
                var row = 54 + (y * rowStride);
                for (var x = 0; x < width; x++)
                {
                    var i = row + (x * 3);
                    bytes[i] = 0;
                    bytes[i + 1] = 0;
                    bytes[i + 2] = 255;
                }
            }

            return bytes;
        }
    }
}
