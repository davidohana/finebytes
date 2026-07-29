using Mfr.Models;
using Mfr.Utils;
using TagLib;
using TagLib.Mpeg;

namespace Mfr.Metadata
{
    /// <summary>
    /// Combined TagLib stream snapshots from one file open.
    /// </summary>
    /// <param name="Media">Always populated when open succeeds.</param>
    /// <param name="Mpeg">MPEG audio header when a codec is <see cref="AudioHeader"/>; otherwise <see langword="null"/>.</param>
    public readonly record struct TagLibStreamProperties(MediaProperties Media, MpegAudioProperties? Mpeg);

    /// <summary>
    /// Reads TagLib stream and image properties into detached snapshots.
    /// </summary>
    public static class MediaPropertiesReader
    {
        /// <summary>
        /// Reads media properties from an existing regular file.
        /// </summary>
        /// <param name="absolutePath">Fully qualified filesystem path to an existing file.</param>
        /// <returns>A new snapshot built from TagLib file properties and related fields.</returns>
        /// <exception cref="ArgumentException"><paramref name="absolutePath"/> is empty, relative, missing, or a directory.</exception>
        /// <exception cref="IOException">TagLib cannot open or read the file.</exception>
        /// <exception cref="CorruptFileException">Thrown by TagLib when the embedded structure is unreadable.</exception>
        /// <exception cref="UnsupportedFormatException">Thrown by TagLib when the format cannot be loaded.</exception>
        public static MediaProperties Read(string absolutePath)
        {
            return ReadStream(absolutePath).Media;
        }

        /// <summary>
        /// Reads media properties and optional MPEG audio-header properties from one TagLib open.
        /// </summary>
        /// <param name="absolutePath">Fully qualified filesystem path to an existing file.</param>
        /// <returns>Media snapshot plus MPEG snapshot when an <see cref="AudioHeader"/> codec is present.</returns>
        /// <exception cref="ArgumentException"><paramref name="absolutePath"/> is empty, relative, missing, or a directory.</exception>
        /// <exception cref="IOException">TagLib cannot open or read the file.</exception>
        /// <exception cref="CorruptFileException">Thrown by TagLib when the embedded structure is unreadable.</exception>
        /// <exception cref="UnsupportedFormatException">Thrown by TagLib when the format cannot be loaded.</exception>
        public static TagLibStreamProperties ReadStream(string absolutePath)
        {
            _ValidateExistingRegularFile(absolutePath);

            using var file = TagLib.File.Create(new TagLib.File.LocalFileAbstraction(absolutePath));
            var properties = file.Properties;
            var mediaTypes = properties.MediaTypes;
            string? mediaTypesText = null;
            if (mediaTypes != MediaTypes.None)
                mediaTypesText = mediaTypes.ToString();

            var media = new MediaProperties
            {
                MimeType = file.MimeType.TrimmedOrNull(),
                PossiblyCorrupt = file.PossiblyCorrupt,
                Duration = properties.Duration,
                MediaTypes = mediaTypesText,
                Description = properties.Description.TrimmedOrNull(),
                AudioBitrate = properties.AudioBitrate,
                AudioSampleRate = properties.AudioSampleRate,
                BitsPerSample = properties.BitsPerSample,
                AudioChannels = properties.AudioChannels,
                VideoWidth = properties.VideoWidth,
                VideoHeight = properties.VideoHeight,
                PhotoWidth = properties.PhotoWidth,
                PhotoHeight = properties.PhotoHeight,
                PhotoQuality = properties.PhotoQuality,
            };

            return new TagLibStreamProperties(media, _TryMapMpeg(properties));
        }

        private static MpegAudioProperties? _TryMapMpeg(Properties properties)
        {
            foreach (var codec in properties.Codecs)
            {
                if (codec is not AudioHeader header)
                    continue;

                var isVbr = header.XingHeader.Present || header.VBRIHeader.Present;
                return new MpegAudioProperties
                {
                    Bitrate = header.AudioBitrate,
                    IsCopyrighted = header.IsCopyrighted,
                    Duration = header.Duration,
                    IsVbr = isVbr,
                    SampleRate = header.AudioSampleRate,
                    Layer = header.AudioLayer,
                    MpegVersion = _FormatMpegVersion(header.Version),
                    ChannelMode = header.ChannelMode.ToString(),
                    IsOriginal = header.IsOriginal,
                    IsProtected = header.IsProtected,
                };
            }

            return null;
        }

        private static string _FormatMpegVersion(TagLib.Mpeg.Version version)
        {
            return version switch
            {
                TagLib.Mpeg.Version.Version1 => "1",
                TagLib.Mpeg.Version.Version2 => "2",
                TagLib.Mpeg.Version.Version25 => "2.5",
                TagLib.Mpeg.Version.Unknown => string.Empty,
                _ => string.Empty,
            };
        }

        private static void _ValidateExistingRegularFile(string absolutePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(absolutePath);

            if (!Path.IsPathFullyQualified(absolutePath))
                throw new ArgumentException("Path must be fully qualified.", nameof(absolutePath));

            if (Directory.Exists(absolutePath))
                throw new ArgumentException($"'{absolutePath}' is a directory.", nameof(absolutePath));

            if (!System.IO.File.Exists(absolutePath))
                throw new ArgumentException($"File does not exist: '{absolutePath}'.", nameof(absolutePath));
        }
    }
}
