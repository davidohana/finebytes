using Mfr.Models;
using Mfr.Utils;
using TagLib;

namespace Mfr.Metadata
{
    /// <summary>
    /// Reads TagLib stream and image properties into a detached <see cref="MediaProperties"/> snapshot.
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
            _ValidateExistingRegularFile(absolutePath);

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
