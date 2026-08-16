using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Mfr.App.Ui.Services;
using Mfr.App.Ui.ViewModels;

namespace Mfr.Tests.Ui
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
            }
            finally
            {
                File.Delete(path);
            }
        }

        private static string _RequireFixture(string fileName)
        {
            var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
            Assert.True(
                File.Exists(fixturePath),
                $"Missing fixture '{fixturePath}'. Run build so Fixtures copy to output.");
            return fixturePath;
        }
    }
}
