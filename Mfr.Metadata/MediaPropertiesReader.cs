using Mfr.Utils;
using TagLib;
using TagLib.Mpeg;

namespace Mfr.Metadata
{
    /// <summary>
    /// Reads TagLib stream and image properties into a detached <see cref="MediaProperties"/> snapshot.
    /// </summary>
    public static class MediaPropertiesReader
    {
        /// <summary>
        /// Reads media properties (and nested MPEG audio header when present) from an existing regular file.
        /// </summary>
        /// <param name="absolutePath">Fully qualified filesystem path to an existing file.</param>
        /// <returns>A new snapshot built from TagLib file properties and related fields.</returns>
        /// <exception cref="ArgumentException"><paramref name="absolutePath"/> is empty, relative, missing, or a directory.</exception>
        /// <exception cref="IOException">TagLib cannot open or read the file.</exception>
        /// <exception cref="CorruptFileException">Thrown by TagLib when the embedded structure is unreadable.</exception>
        /// <exception cref="UnsupportedFormatException">Thrown by TagLib when the format cannot be loaded.</exception>
        public static MediaProperties Read(string absolutePath)
        {
            absolutePath.RequireExistingRegularFile();

            using var file = TagLib.File.Create(new TagLib.File.LocalFileAbstraction(absolutePath));
            var properties = file.Properties;
            var mediaTypes = properties.MediaTypes;
            string? mediaTypesText = null;
            if (mediaTypes != MediaTypes.None)
                mediaTypesText = mediaTypes.ToString();

            return new MediaProperties
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
                Mpeg = _TryMapMpeg(properties),
            };
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
    }
}
