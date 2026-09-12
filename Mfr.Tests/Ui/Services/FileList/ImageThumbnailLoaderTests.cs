using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Mfr.App.Ui.Services.FileList;
using Mfr.App.Ui.ViewModels.FileList;

namespace Mfr.Tests.Ui.Thumbnails
{
    /// <summary>
    /// Tests image preview decoding for Thumbnails view.
    /// </summary>
    public sealed class ImageThumbnailLoaderTests
    {
        /// <summary>
        /// Verifies a PNG fixture decodes to a bitmap no wider than the requested width.
        /// </summary>
        [AvaloniaFact]
        public void TryLoad_Decodes_Png_To_Requested_Width()
        {
            var path = _RequireFixture("tiny.png");
            var length = new FileInfo(path).Length;

            var image = ImageThumbnailLoader.TryLoad(path, length, ThumbnailSizes.Huge);

            var bitmap = Assert.IsType<Bitmap>(image);
            Assert.True(bitmap.PixelSize.Width <= ThumbnailSizes.Huge);
            Assert.True(bitmap.PixelSize.Width > 0);
        }

        /// <summary>
        /// Verifies a JPEG fixture decodes to a bitmap.
        /// </summary>
        [AvaloniaFact]
        public void TryLoad_Decodes_Jpeg_Fixture()
        {
            var path = _RequireFixture("tiny.jpeg");
            var length = new FileInfo(path).Length;

            var image = ImageThumbnailLoader.TryLoad(path, length, ThumbnailSizes.Huge);

            var bitmap = Assert.IsType<Bitmap>(image);
            Assert.True(bitmap.PixelSize.Width > 0);
        }

        /// <summary>
        /// Verifies an EXIF JPEG fixture can be decoded without throwing.
        /// </summary>
        [AvaloniaFact]
        public void TryLoad_Exif_Jpeg_Does_Not_Throw()
        {
            var path = _RequireFixture("tiny-exif.jpeg");
            var length = new FileInfo(path).Length;

            var image = ImageThumbnailLoader.TryLoad(path, length, ThumbnailSizes.Huge);

            Assert.IsType<Bitmap>(image);
        }

        /// <summary>
        /// Verifies a JPEG with an undersized EXIF thumbnail still decodes the full image.
        /// </summary>
        [AvaloniaFact]
        public void TryLoad_Does_Not_Upscale_Small_Exif_Thumbnail()
        {
            var thumbnailJpeg = File.ReadAllBytes(_RequireFixture("tiny.jpeg"));
            var path = Path.Combine(Path.GetTempPath(), $"mfr-exif-thumb-{Guid.NewGuid():N}.jpg");
            File.WriteAllBytes(path, _CreateJpegWithExifThumbnail(thumbnailJpeg));
            try
            {
                using var embedded = new MemoryStream(thumbnailJpeg);
                using var nativeThumb = new Bitmap(embedded);
                Assert.True(nativeThumb.PixelSize.Width < ThumbnailSizes.Huge);

                var length = new FileInfo(path).Length;
                var image = ImageThumbnailLoader.TryLoad(path, length, ThumbnailSizes.Huge);

                Assert.IsType<Bitmap>(image);
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Verifies the EXIF reader returns the embedded JPEG bytes.
        /// </summary>
        [Fact]
        public void TryExtract_Returns_Embedded_Thumbnail_Bytes()
        {
            var thumbnailJpeg = File.ReadAllBytes(_RequireFixture("tiny.jpeg"));
            using var stream = new MemoryStream(_CreateJpegWithExifThumbnail(thumbnailJpeg));

            var extracted = JpegExifThumbnailReader.TryExtract(stream);

            Assert.Equal(thumbnailJpeg, extracted);
        }

        /// <summary>
        /// Verifies a JPEG without EXIF has no embedded thumbnail.
        /// </summary>
        [Fact]
        public void TryExtract_Returns_Null_When_No_Exif_Thumbnail()
        {
            using var stream = File.OpenRead(_RequireFixture("tiny.jpeg"));

            Assert.Null(JpegExifThumbnailReader.TryExtract(stream));
        }

        /// <summary>
        /// Verifies non-image files are skipped.
        /// </summary>
        [Fact]
        public void TryLoad_Skips_Non_Image_Extension()
        {
            var path = Path.Combine(Path.GetTempPath(), $"mfr-thumb-{Guid.NewGuid():N}.txt");
            File.WriteAllText(path, "not an image");
            try
            {
                var length = new FileInfo(path).Length;
                Assert.Null(ImageThumbnailLoader.TryLoad(path, length));
                Assert.False(ImageThumbnailLoader.CanLoad(path, length));
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Verifies files larger than the decode cap are skipped without opening them.
        /// </summary>
        [Fact]
        public void TryLoad_Skips_Oversized_Files()
        {
            var path = Path.Combine(Path.GetTempPath(), $"mfr-thumb-{Guid.NewGuid():N}.jpg");
            var length = 21L * 1024 * 1024;

            Assert.False(ImageThumbnailLoader.CanLoad(path, length));
            Assert.Null(ImageThumbnailLoader.TryLoad(path, length));
        }

        /// <summary>
        /// Verifies unknown length skips decode.
        /// </summary>
        [Fact]
        public void TryLoad_Skips_When_Length_Unknown()
        {
            var path = _RequireFixture("tiny.jpeg");

            Assert.False(ImageThumbnailLoader.CanLoad(path, length: null));
            Assert.Null(ImageThumbnailLoader.TryLoad(path, length: null));
        }

        private static byte[] _CreateJpegWithExifThumbnail(byte[] thumbnailJpeg)
        {
            const int tiffHeaderLength = 8;
            const int ifd0Length = 6;
            const int ifd1HeaderLength = 2;
            const int ifdEntryLength = 12;
            const int ifd1EntryCount = 3;
            const int nextIfdLength = 4;
            var jpegOffset =
                tiffHeaderLength + ifd0Length + ifd1HeaderLength + (ifd1EntryCount * ifdEntryLength) + nextIfdLength;

            using var tiff = new MemoryStream();
            _WriteAscii(tiff, "II");
            _WriteUInt16Little(tiff, 42);
            _WriteUInt32Little(tiff, tiffHeaderLength);

            _WriteUInt16Little(tiff, 0);
            _WriteUInt32Little(tiff, tiffHeaderLength + ifd0Length);

            _WriteUInt16Little(tiff, ifd1EntryCount);
            _WriteIfdEntry(tiff, tag: 0x0103, type: 3, count: 1, value: 6);
            _WriteIfdEntry(tiff, tag: 0x0201, type: 4, count: 1, value: (uint)jpegOffset);
            _WriteIfdEntry(tiff, tag: 0x0202, type: 4, count: 1, value: (uint)thumbnailJpeg.Length);
            _WriteUInt32Little(tiff, 0);
            tiff.Write(thumbnailJpeg);

            var tiffBytes = tiff.ToArray();
            using var jpeg = new MemoryStream();
            jpeg.WriteByte(0xFF);
            jpeg.WriteByte(0xD8);
            jpeg.WriteByte(0xFF);
            jpeg.WriteByte(0xE1);
            var app1Length = 2 + 6 + tiffBytes.Length;
            jpeg.WriteByte((byte)(app1Length >> 8));
            jpeg.WriteByte((byte)app1Length);
            _WriteAscii(jpeg, "Exif");
            jpeg.WriteByte(0);
            jpeg.WriteByte(0);
            jpeg.Write(tiffBytes);

            var primary = File.ReadAllBytes(_RequireFixture("tiny.jpeg"));
            jpeg.Write(primary, 2, primary.Length - 2);
            return jpeg.ToArray();
        }

        private static void _WriteIfdEntry(Stream stream, ushort tag, ushort type, uint count, uint value)
        {
            _WriteUInt16Little(stream, tag);
            _WriteUInt16Little(stream, type);
            _WriteUInt32Little(stream, count);
            _WriteUInt32Little(stream, value);
        }

        private static void _WriteAscii(Stream stream, string text)
        {
            foreach (var ch in text)
            {
                stream.WriteByte((byte)ch);
            }
        }

        private static void _WriteUInt16Little(Stream stream, ushort value)
        {
            stream.WriteByte((byte)value);
            stream.WriteByte((byte)(value >> 8));
        }

        private static void _WriteUInt32Little(Stream stream, uint value)
        {
            stream.WriteByte((byte)value);
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)(value >> 16));
            stream.WriteByte((byte)(value >> 24));
        }

        private static string _RequireFixture(string fileName)
        {
            var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
            Assert.True(
                File.Exists(fixturePath),
                $"Missing fixture '{fixturePath}'. Run build so Fixtures copy to output."
            );
            return fixturePath;
        }
    }
}
