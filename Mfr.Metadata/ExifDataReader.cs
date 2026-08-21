using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Exif.Makernotes;
using MetadataExtractor.Formats.Iptc;
using Mfr.Utils;
using MeDirectory = MetadataExtractor.Directory;

namespace Mfr.Metadata
{
    /// <summary>
    /// Reads MetadataExtractor EXIF/IPTC/makernote directories into a detached <see cref="ExifData"/> snapshot.
    /// </summary>
    public static class ExifDataReader
    {
        private static readonly IReadOnlyDictionary<string, string> _emptyTags = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase
        );

        /// <summary>
        /// Reads EXIF from an existing regular file that is a mapped raster type.
        /// </summary>
        /// <param name="absolutePath">Fully qualified filesystem path to an existing file.</param>
        /// <returns>A new snapshot mapped from MetadataExtractor directories.</returns>
        /// <exception cref="ArgumentException"><paramref name="absolutePath"/> is empty, relative, missing, or a directory.</exception>
        /// <exception cref="InvalidOperationException">The file is not a mapped raster type (including audio/video MetadataExtractor opens).</exception>
        public static ExifData Read(string absolutePath)
        {
            return ImageFileReader.Read(absolutePath).Exif;
        }

        /// <summary>
        /// Maps MetadataExtractor directories to an <see cref="ExifData"/> DTO and discards the raw list.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Does not re-check the raster allowlist; callers must map image properties first via
        /// <see cref="ImagePropertiesReader.MapFrom"/>. Missing fields stay <see langword="null"/>.
        /// Thumbnail DateTime is not copied into <see cref="ExifData.DateTaken"/>.
        /// </para>
        /// </remarks>
        /// <param name="directories">Directories from one MetadataExtractor open.</param>
        /// <returns>A detached EXIF snapshot; no MetadataExtractor types are retained.</returns>
        internal static ExifData MapFrom(IReadOnlyList<MeDirectory> directories)
        {
            var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
            var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            return new ExifData
            {
                DateTaken = _ReadDateTaken(subIfd),
                Make = _ReadDescription(ifd0, ExifDirectoryBase.TagMake),
                Model = _ReadDescription(ifd0, ExifDirectoryBase.TagModel),
                Artist = _ReadDescription(ifd0, ExifDirectoryBase.TagArtist),
                Description = _ReadDescription(ifd0, ExifDirectoryBase.TagImageDescription),
                Title = _ReadDescription(ifd0, ExifDirectoryBase.TagWinTitle),
                Subject = _ReadDescription(ifd0, ExifDirectoryBase.TagWinSubject),
                Author = _ReadDescription(ifd0, ExifDirectoryBase.TagWinAuthor),
                Keywords = _ReadDescription(ifd0, ExifDirectoryBase.TagWinKeywords),
                Comments = _ReadDescription(ifd0, ExifDirectoryBase.TagWinComment),
                Exposure = _ReadDescription(subIfd, ExifDirectoryBase.TagExposureTime),
                FNumber = _ReadDescription(subIfd, ExifDirectoryBase.TagFNumber),
                Iso = _ReadDescription(subIfd, ExifDirectoryBase.TagIsoEquivalent),
                FocalLength = _ReadDescription(subIfd, ExifDirectoryBase.TagFocalLength),
                FocalLength35mm = _ReadDescription(subIfd, ExifDirectoryBase.Tag35MMFilmEquivFocalLength),
                UserComment = _ReadDescription(subIfd, ExifDirectoryBase.TagUserComment),
                TagToDescription = _FlattenTagToDescription(directories),
            };
        }

        private static DateTime? _ReadDateTaken(ExifSubIfdDirectory? subIfd)
        {
            if (subIfd is null)
                return null;

            if (!subIfd.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var dateTaken))
                return null;

            return DateTime.SpecifyKind(dateTaken, DateTimeKind.Unspecified);
        }

        private static string? _ReadDescription(MeDirectory? directory, int tag)
        {
            if (directory is null)
                return null;

            return _CleanDescription(directory.GetDescription(tag));
        }

        private static string? _CleanDescription(string? raw)
        {
            if (raw is null)
                return null;

            var cleaned = raw.Replace('\n', ' ').Trim();
            return cleaned.TrimmedOrNull();
        }

        /// <summary>
        /// Flattens aliased directories into <see cref="ExifData.TagToDescription"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Only directories with a <see cref="ExifData.SourceAliases"/> mapping are included.
        /// Each non-blank description is stored twice: <c>{alias}/{tag-name}</c> and
        /// <c>{alias}/{tag-id}</c>. Existing keys are not overwritten (first tag wins).
        /// </para>
        /// </remarks>
        /// <param name="directories">Directories from one MetadataExtractor open.</param>
        /// <returns>A case-insensitive map, or empty when nothing flattened.</returns>
        private static IReadOnlyDictionary<string, string> _FlattenTagToDescription(
            IReadOnlyList<MeDirectory> directories
        )
        {
            Dictionary<string, string>? tagToDescription = null;
            foreach (var directory in directories)
            {
                var alias = _TryGetSourceAlias(directory);
                if (alias is null)
                    continue;

                foreach (var tag in directory.Tags)
                {
                    var description = _CleanDescription(directory.GetDescription(tag.Type));
                    if (description is null)
                        continue;

                    tagToDescription ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    tagToDescription.TryAdd($"{alias}/{tag.Name}", description);
                    tagToDescription.TryAdd($"{alias}/{tag.Type}", description);
                }
            }

            if (tagToDescription is null || tagToDescription.Count == 0)
                return _emptyTags;

            return tagToDescription;
        }

        private static string? _TryGetSourceAlias(MeDirectory directory)
        {
            return directory switch
            {
                ExifThumbnailDirectory => "Thumb",
                ExifIfd0Directory => "Exif",
                ExifSubIfdDirectory => "ExifSub",
                GpsDirectory => "GPS",
                IptcDirectory => "IPTC",
                CanonMakernoteDirectory => "Canon",
                CasioType1MakernoteDirectory => "Casio",
                CasioType2MakernoteDirectory => "Casio",
                FujifilmMakernoteDirectory => "FujiFilm",
                NikonType1MakernoteDirectory => "Nikon",
                NikonType2MakernoteDirectory => "Nikon",
                OlympusMakernoteDirectory => "Olympus",
                ExifInteropDirectory => "Interop",
                _ => null,
            };
        }
    }
}
