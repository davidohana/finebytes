using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Mfr.App.Ui.Services.FileList
{
    /// <summary>
    /// Decodes a small preview bitmap for common image files.
    /// </summary>
    internal static class ImageThumbnailLoader
    {
        /// <summary>
        /// Pixel width used when decoding image previews (matches the largest thumbnail step).
        /// </summary>
        public const int DecodeWidth = 256;

        private const long _MaxBytes = 20 * 1024 * 1024;

        private static readonly HashSet<string> _extensionToIsImage = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".bmp",
            ".webp",
        };

        /// <summary>
        /// Gets whether <paramref name="path"/> is an image that <see cref="TryLoad"/> will attempt to decode.
        /// </summary>
        /// <param name="path">Full filesystem path of a file.</param>
        /// <param name="length">Known file length, or <see langword="null"/> when unknown.</param>
        /// <returns><see langword="true"/> when the file is an allowed image within the size cap.</returns>
        public static bool CanLoad(string path, long? length)
        {
            if (length is null or > _MaxBytes)
                return false;

            return _extensionToIsImage.Contains(Path.GetExtension(path));
        }

        /// <summary>
        /// Tries to decode a thumbnail for <paramref name="path"/>.
        /// </summary>
        /// <param name="path">Full filesystem path of a file.</param>
        /// <param name="length">Known file length, or <see langword="null"/> when unknown.</param>
        /// <param name="decodeWidth">Target pixel width. Defaults to <see cref="DecodeWidth"/>.</param>
        /// <returns>A preview image, or <see langword="null"/> when the file is skipped or cannot be decoded.</returns>
        public static IImage? TryLoad(string path, long? length, int decodeWidth = DecodeWidth)
        {
            if (!CanLoad(path, length))
                return null;

            var width = decodeWidth < 1 ? DecodeWidth : decodeWidth;
            var extension = Path.GetExtension(path);
            var isJpeg =
                extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
            try
            {
                using var stream = File.OpenRead(path);
                if (isJpeg)
                {
                    var fromExif = _TryLoadFromExifThumbnail(stream, width);
                    if (fromExif is not null)
                        return fromExif;

                    if (stream.CanSeek)
                        stream.Position = 0;
                }

                return Bitmap.DecodeToWidth(stream, width);
            }
            catch (Exception ex)
                when (ex
                        is IOException
                            or UnauthorizedAccessException
                            or ArgumentException
                            or InvalidOperationException
                            or NotSupportedException
                )
            {
                return null;
            }
        }

        private static Bitmap? _TryLoadFromExifThumbnail(Stream stream, int width)
        {
            var embeddedJpeg = JpegExifThumbnailReader.TryExtract(stream);
            if (embeddedJpeg is null)
                return null;

            try
            {
                using var thumbStream = new MemoryStream(embeddedJpeg, writable: false);
                var native = new Bitmap(thumbStream);
                // Camera EXIF thumbs are often ~160px; stretching them to Huge (256) looks blocky.
                if (native.PixelSize.Width < width)
                {
                    native.Dispose();
                    return null;
                }

                if (native.PixelSize.Width == width)
                    return native;

                native.Dispose();
                thumbStream.Position = 0;
                return Bitmap.DecodeToWidth(thumbStream, width);
            }
            catch (Exception ex)
                when (ex is IOException or ArgumentException or InvalidOperationException or NotSupportedException)
            {
                return null;
            }
        }
    }
}
