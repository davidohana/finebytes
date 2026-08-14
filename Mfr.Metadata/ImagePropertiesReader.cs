using MetadataExtractor;
using MetadataExtractor.Formats.Bmp;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.FileType;
using MetadataExtractor.Formats.Gif;
using MetadataExtractor.Formats.Ico;
using MetadataExtractor.Formats.Jfif;
using MetadataExtractor.Formats.Jpeg;
using MetadataExtractor.Formats.Png;
using MetadataExtractor.Formats.WebP;
using Mfr.Utils;
using MeDirectory = MetadataExtractor.Directory;

namespace Mfr.Metadata
{
    /// <summary>
    /// Reads MetadataExtractor raster directories into a detached <see cref="ImageProperties"/> snapshot.
    /// </summary>
    public static class ImagePropertiesReader
    {
        private static readonly HashSet<string> _rasterTypeNameToIsAllowed = new(StringComparer.Ordinal)
        {
            "JPEG",
            "PNG",
            "GIF",
            "BMP",
            "TIFF",
            "ICO",
            "WebP",
        };

        /// <summary>
        /// Reads image properties from an existing regular file that is a mapped raster type.
        /// </summary>
        /// <param name="absolutePath">Fully qualified filesystem path to an existing file.</param>
        /// <returns>A new snapshot mapped from MetadataExtractor directories.</returns>
        /// <exception cref="ArgumentException"><paramref name="absolutePath"/> is empty, relative, missing, or a directory.</exception>
        /// <exception cref="InvalidOperationException">The file is not a mapped raster type (including audio/video MetadataExtractor opens).</exception>
        public static ImageProperties Read(string absolutePath)
        {
            return ImageFileReader.Read(absolutePath).Image;
        }

        /// <summary>
        /// Maps MetadataExtractor directories to an <see cref="ImageProperties"/> DTO and discards the raw list.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Split from the disk open so <see cref="ImageFileReader"/> can map EXIF from the same in-memory
        /// directories without a second file read. Non-allowlist types (including audio/video MetadataExtractor
        /// opens) throw; missing fields on a mapped raster stay <c>0</c> / null.
        /// </para>
        /// </remarks>
        /// <param name="directories">Directories from one <see cref="ImageMetadataReader.ReadMetadata(string)"/> call.</param>
        /// <returns>A detached raster snapshot; no MetadataExtractor types are retained.</returns>
        /// <exception cref="InvalidOperationException">The detected file type is not a mapped raster.</exception>
        internal static ImageProperties MapFrom(IReadOnlyList<MeDirectory> directories)
        {
            var format = _EnsureRasterAllowlist(_ReadFormatName(directories));
            var (width, height) = _ReadDimensions(directories, format);
            var (horizontalDpi, verticalDpi) = _ReadDpiPair(directories);
            return new ImageProperties
            {
                Format = format,
                Width = width,
                Height = height,
                BitDepth = _ReadBitDepth(directories, format),
                HorizontalResolutionDpi = horizontalDpi,
                VerticalResolutionDpi = verticalDpi,
                FrameCount = _ReadFrameCount(directories, format, width, height),
            };
        }

        private static string _EnsureRasterAllowlist(string? format)
        {
            if (format is not null && _rasterTypeNameToIsAllowed.Contains(format))
                return format;

            var displayName = format ?? "unknown";
            throw new InvalidOperationException($"Cannot read image properties for file type '{displayName}'.");
        }

        private static string? _ReadFormatName(IReadOnlyList<MeDirectory> directories)
        {
            var fileTypeDirectory = directories.OfType<FileTypeDirectory>().FirstOrDefault();
            return fileTypeDirectory?.GetString(FileTypeDirectory.TagDetectedFileTypeName).TrimmedOrNull();
        }

        private static (int Width, int Height) _ReadDimensions(IReadOnlyList<MeDirectory> directories, string format)
        {
            return format switch
            {
                "JPEG" => _ReadJpegDimensions(directories),
                "PNG" => _ReadPngDimensions(directories),
                "GIF" => _ReadGifDimensions(directories),
                "BMP" => _ReadBmpDimensions(directories),
                "TIFF" => _ReadTiffDimensions(directories),
                "WebP" => _ReadWebPDimensions(directories),
                "ICO" => _ReadIcoDimensions(directories),
                _ => (0, 0),
            };
        }

        private static (int Width, int Height) _ReadJpegDimensions(IReadOnlyList<MeDirectory> directories)
        {
            var jpeg = directories.OfType<JpegDirectory>().FirstOrDefault();
            return (_TryGetInt(jpeg, JpegDirectory.TagImageWidth), _TryGetInt(jpeg, JpegDirectory.TagImageHeight));
        }

        private static (int Width, int Height) _ReadPngDimensions(IReadOnlyList<MeDirectory> directories)
        {
            var ihdr = _FindPngChunk(directories, PngChunkType.IHDR);
            return (_TryGetInt(ihdr, PngDirectory.TagImageWidth), _TryGetInt(ihdr, PngDirectory.TagImageHeight));
        }

        private static (int Width, int Height) _ReadGifDimensions(IReadOnlyList<MeDirectory> directories)
        {
            var header = directories.OfType<GifHeaderDirectory>().FirstOrDefault();
            return (_TryGetInt(header, GifHeaderDirectory.TagImageWidth), _TryGetInt(header, GifHeaderDirectory.TagImageHeight));
        }

        private static (int Width, int Height) _ReadBmpDimensions(IReadOnlyList<MeDirectory> directories)
        {
            var bmp = directories.OfType<BmpHeaderDirectory>().FirstOrDefault();
            var width = _TryGetInt(bmp, BmpHeaderDirectory.TagImageWidth);
            var height = _TryGetInt(bmp, BmpHeaderDirectory.TagImageHeight);
            if (height < 0)
                height = Math.Abs(height);

            return (width, height);
        }

        private static (int Width, int Height) _ReadTiffDimensions(IReadOnlyList<MeDirectory> directories)
        {
            var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
            return (_TryGetInt(ifd0, ExifDirectoryBase.TagImageWidth), _TryGetInt(ifd0, ExifDirectoryBase.TagImageHeight));
        }

        private static (int Width, int Height) _ReadWebPDimensions(IReadOnlyList<MeDirectory> directories)
        {
            var webp = directories.OfType<WebPDirectory>().FirstOrDefault();
            return (_TryGetInt(webp, WebPDirectory.TagImageWidth), _TryGetInt(webp, WebPDirectory.TagImageHeight));
        }

        private static (int Width, int Height) _ReadIcoDimensions(IReadOnlyList<MeDirectory> directories)
        {
            var ico = directories.OfType<IcoDirectory>().FirstOrDefault();
            return (_ReadIcoDimension(ico, IcoDirectory.TagImageWidth), _ReadIcoDimension(ico, IcoDirectory.TagImageHeight));
        }

        private static int _ReadIcoDimension(IcoDirectory? directory, int tag)
        {
            if (directory is null || !directory.ContainsTag(tag))
                return 0;

            var value = _TryGetInt(directory, tag);
            return value == 0 ? 256 : value;
        }

        private static int _ReadBitDepth(IReadOnlyList<MeDirectory> directories, string format)
        {
            return format switch
            {
                "JPEG" => _ReadJpegBitDepth(directories),
                "PNG" => _ReadPngBitDepth(directories),
                "GIF" => _TryGetInt(directories.OfType<GifHeaderDirectory>().FirstOrDefault(), GifHeaderDirectory.TagBitsPerPixel),
                "BMP" => _TryGetInt(directories.OfType<BmpHeaderDirectory>().FirstOrDefault(), BmpHeaderDirectory.TagBitsPerPixel),
                "ICO" => _TryGetInt(directories.OfType<IcoDirectory>().FirstOrDefault(), IcoDirectory.TagBitsPerPixel),
                "TIFF" => _ReadTiffBitDepth(directories),
                _ => 0,
            };
        }

        private static int _ReadJpegBitDepth(IReadOnlyList<MeDirectory> directories)
        {
            var jpeg = directories.OfType<JpegDirectory>().FirstOrDefault();
            var precision = _TryGetInt(jpeg, JpegDirectory.TagDataPrecision);
            var components = _TryGetInt(jpeg, JpegDirectory.TagNumberOfComponents);
            if (precision == 0 || components == 0)
                return 0;

            return precision * components;
        }

        private static int _ReadPngBitDepth(IReadOnlyList<MeDirectory> directories)
        {
            var ihdr = _FindPngChunk(directories, PngChunkType.IHDR);
            var bitsPerSample = _TryGetInt(ihdr, PngDirectory.TagBitsPerSample);
            if (bitsPerSample == 0 || ihdr is null || !ihdr.TryGetInt32(PngDirectory.TagColorType, out var colorType))
                return 0;

            var channelCount = colorType switch
            {
                0 or 3 => 1,
                2 => 3,
                4 => 2,
                6 => 4,
                _ => 0,
            };
            if (channelCount == 0)
                return 0;

            return bitsPerSample * channelCount;
        }

        private static int _ReadTiffBitDepth(IReadOnlyList<MeDirectory> directories)
        {
            var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
            if (ifd0 is null)
                return 0;

            return _SumNumericTag(ifd0, ExifDirectoryBase.TagBitsPerSample);
        }

        private static (double Horizontal, double Vertical) _ReadDpiPair(IReadOnlyList<MeDirectory> directories)
        {
            var jfif = directories.OfType<JfifDirectory>().FirstOrDefault();
            if (jfif is not null)
                return _ReadJfifDpi(jfif);

            var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
            if (ifd0 is not null && _TryReadExifDpi(ifd0, out var exifDpi))
                return exifDpi;

            var phys = _FindPngChunk(directories, PngChunkType.pHYs);
            if (phys is not null)
                return _ReadPngDpi(phys);

            var bmp = directories.OfType<BmpHeaderDirectory>().FirstOrDefault();
            if (bmp is not null)
                return _ReadBmpDpi(bmp);

            return (0, 0);
        }

        private static (double Horizontal, double Vertical) _ReadJfifDpi(JfifDirectory jfif)
        {
            if (!jfif.TryGetInt32(JfifDirectory.TagUnits, out var units))
                return (0, 0);

            var x = _TryGetDouble(jfif, JfifDirectory.TagResX);
            var y = _TryGetDouble(jfif, JfifDirectory.TagResY);
            return _ConvertDensityToDpi(x, y, units, isJfifUnits: true);
        }

        private static bool _TryReadExifDpi(ExifIfd0Directory ifd0, out (double Horizontal, double Vertical) dpi)
        {
            var hasX = ifd0.ContainsTag(ExifDirectoryBase.TagXResolution);
            var hasY = ifd0.ContainsTag(ExifDirectoryBase.TagYResolution);
            if (!hasX && !hasY)
            {
                dpi = (0, 0);
                return false;
            }

            var x = _TryGetDouble(ifd0, ExifDirectoryBase.TagXResolution);
            var y = _TryGetDouble(ifd0, ExifDirectoryBase.TagYResolution);
            var unit = 2;
            if (ifd0.TryGetInt32(ExifDirectoryBase.TagResolutionUnit, out var taggedUnit))
                unit = taggedUnit;

            dpi = _ConvertDensityToDpi(x, y, unit, isJfifUnits: false);
            return true;
        }

        private static (double Horizontal, double Vertical) _ReadPngDpi(PngDirectory phys)
        {
            if (!phys.TryGetInt32(PngDirectory.TagUnitSpecifier, out var unit) || unit != 1)
                return (0, 0);

            var x = _TryGetDouble(phys, PngDirectory.TagPixelsPerUnitX);
            var y = _TryGetDouble(phys, PngDirectory.TagPixelsPerUnitY);
            return (_PixelsPerMetreToDpi(x), _PixelsPerMetreToDpi(y));
        }

        private static (double Horizontal, double Vertical) _ReadBmpDpi(BmpHeaderDirectory bmp)
        {
            var x = _TryGetDouble(bmp, BmpHeaderDirectory.TagXPixelsPerMeter);
            var y = _TryGetDouble(bmp, BmpHeaderDirectory.TagYPixelsPerMeter);
            return (_PixelsPerMetreToDpi(x), _PixelsPerMetreToDpi(y));
        }

        private static (double Horizontal, double Vertical) _ConvertDensityToDpi(
            double x,
            double y,
            int unit,
            bool isJfifUnits)
        {
            if (isJfifUnits)
            {
                if (unit == 1)
                    return (_NonPositiveToZero(x), _NonPositiveToZero(y));
                if (unit == 2)
                    return (_NonPositiveToZero(x * 2.54), _NonPositiveToZero(y * 2.54));

                return (0, 0);
            }

            if (unit == 2)
                return (_NonPositiveToZero(x), _NonPositiveToZero(y));
            if (unit == 3)
                return (_NonPositiveToZero(x * 2.54), _NonPositiveToZero(y * 2.54));

            return (0, 0);
        }

        private static int _ReadFrameCount(IReadOnlyList<MeDirectory> directories, string format, int width, int height)
        {
            if (format == "GIF")
                return directories.OfType<GifImageDirectory>().Count();

            if (format == "ICO")
                return directories.OfType<IcoDirectory>().Count();

            if (format == "TIFF")
            {
                var imageIfdCount = directories.OfType<ExifIfd0Directory>().Count(_HasDimensionTag);
                if (imageIfdCount > 0)
                    return imageIfdCount;
            }

            var isStillFormat = format is "JPEG" or "PNG" or "BMP" or "WebP" or "TIFF";
            if (isStillFormat && (width > 0 || height > 0))
                return 1;

            return 0;
        }

        private static bool _HasDimensionTag(ExifIfd0Directory directory)
        {
            return directory.ContainsTag(ExifDirectoryBase.TagImageWidth)
                || directory.ContainsTag(ExifDirectoryBase.TagImageHeight);
        }

        private static PngDirectory? _FindPngChunk(IReadOnlyList<MeDirectory> directories, PngChunkType chunkType)
        {
            return directories.OfType<PngDirectory>().FirstOrDefault(d => d.GetPngChunkType().Equals(chunkType));
        }

        private static int _SumNumericTag(MeDirectory directory, int tag)
        {
            var ints = directory.GetInt32Array(tag);
            if (ints is { Length: > 0 })
                return ints.Sum();

            var raw = directory.GetObject(tag);
            if (raw is ushort[] ushorts)
                return ushorts.Sum(v => v);
            if (raw is short[] shorts)
                return shorts.Sum(v => v);

            return _TryGetInt(directory, tag);
        }

        private static int _TryGetInt(MeDirectory? directory, int tag)
        {
            if (directory is null || !directory.TryGetInt32(tag, out var value))
                return 0;

            return value;
        }

        private static double _TryGetDouble(MeDirectory directory, int tag)
        {
            if (directory.TryGetDouble(tag, out var value))
                return value;

            if (directory.TryGetRational(tag, out var rational))
                return rational.ToDouble();

            return 0;
        }

        private static double _PixelsPerMetreToDpi(double pixelsPerMetre)
        {
            if (pixelsPerMetre <= 0)
                return 0;

            return pixelsPerMetre * 0.0254;
        }

        private static double _NonPositiveToZero(double value)
        {
            return value <= 0 ? 0 : value;
        }
    }
}
