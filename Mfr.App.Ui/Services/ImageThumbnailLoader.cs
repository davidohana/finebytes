using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Mfr.App.Ui.Services
{
    /// <summary>
    /// Decodes a small preview bitmap for common image files.
    /// </summary>
    internal static class ImageThumbnailLoader
    {
        /// <summary>
        /// Pixel width used when decoding image previews.
        /// </summary>
        public const int DecodeWidth = 96;

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
        /// Tries to decode a thumbnail for <paramref name="path"/>.
        /// </summary>
        /// <param name="path">Full filesystem path of a file.</param>
        /// <param name="length">Known file length, or <see langword="null"/> when unknown.</param>
        /// <returns>A preview image, or <see langword="null"/> when the file is skipped or cannot be decoded.</returns>
        public static IImage? TryLoad(string path, long? length)
        {
            if (length is null or > _MaxBytes)
                return null;

            var extension = Path.GetExtension(path);
            if (!_extensionToIsImage.Contains(extension))
                return null;

            try
            {
                using var stream = File.OpenRead(path);
                return Bitmap.DecodeToWidth(stream, DecodeWidth);
            }
            catch (Exception ex) when (ex is IOException
                or UnauthorizedAccessException
                or ArgumentException
                or InvalidOperationException
                or NotSupportedException)
            {
                return null;
            }
        }
    }
}
