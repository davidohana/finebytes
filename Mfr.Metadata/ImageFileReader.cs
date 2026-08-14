using MetadataExtractor;
using Mfr.Utils;

namespace Mfr.Metadata
{
    /// <summary>
    /// Raster and EXIF snapshots from one MetadataExtractor open.
    /// </summary>
    /// <param name="Image">Mapped raster properties (dimensions, bit depth, DPI, frames).</param>
    /// <param name="Exif">Mapped EXIF fields and flattened extended tags.</param>
    public readonly record struct ImageFileSnapshot(ImageProperties Image, ExifData Exif);

    /// <summary>
    /// Opens a file once with MetadataExtractor and maps both image properties and EXIF.
    /// </summary>
    public static class ImageFileReader
    {
        /// <summary>
        /// Reads image properties and EXIF from an existing regular file that is a mapped raster type.
        /// </summary>
        /// <param name="absolutePath">Fully qualified filesystem path to an existing file.</param>
        /// <returns>Image and EXIF snapshots mapped from the same MetadataExtractor directories.</returns>
        /// <exception cref="ArgumentException"><paramref name="absolutePath"/> is empty, relative, missing, or a directory.</exception>
        /// <exception cref="InvalidOperationException">The file is not a mapped raster type (including audio/video MetadataExtractor opens).</exception>
        public static ImageFileSnapshot Read(string absolutePath)
        {
            absolutePath.RequireExistingRegularFile();

            var directories = ImageMetadataReader.ReadMetadata(absolutePath);
            return new ImageFileSnapshot(
                Image: ImagePropertiesReader.MapFrom(directories),
                Exif: ExifDataReader.MapFrom(directories));
        }
    }
}
