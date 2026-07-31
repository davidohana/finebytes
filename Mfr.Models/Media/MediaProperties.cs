namespace Mfr.Models
{
    /// <summary>
    /// Read-only TagLib stream/image snapshot for media formatter tokens.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Populated lazily from disk; never written back. Integer fields use <c>0</c> for absent (TagLib convention).
    /// </para>
    /// </remarks>
    public sealed record MediaProperties
    {
        /// <summary>
        /// Gets the MIME type reported by TagLib, when present.
        /// </summary>
        public string? MimeType { get; init; }

        /// <summary>
        /// Gets whether TagLib marked the file as possibly corrupt.
        /// </summary>
        public bool PossiblyCorrupt { get; init; }

        /// <summary>
        /// Gets the media duration; <see cref="TimeSpan.Zero"/> when unknown.
        /// </summary>
        public TimeSpan Duration { get; init; }

        /// <summary>
        /// Gets TagLib media-type flags as text (e.g. <c>Audio</c>); <see langword="null"/> when none.
        /// </summary>
        public string? MediaTypes { get; init; }

        /// <summary>
        /// Gets the aggregate codec description from TagLib properties.
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// Gets audio bitrate in kbps; <c>0</c> when absent.
        /// </summary>
        public int AudioBitrate { get; init; }

        /// <summary>
        /// Gets audio sample rate in Hz; <c>0</c> when absent.
        /// </summary>
        public int AudioSampleRate { get; init; }

        /// <summary>
        /// Gets bits per sample; <c>0</c> when absent.
        /// </summary>
        public int BitsPerSample { get; init; }

        /// <summary>
        /// Gets audio channel count; <c>0</c> when absent.
        /// </summary>
        public int AudioChannels { get; init; }

        /// <summary>
        /// Gets video frame width in pixels; <c>0</c> when absent.
        /// </summary>
        public int VideoWidth { get; init; }

        /// <summary>
        /// Gets video frame height in pixels; <c>0</c> when absent.
        /// </summary>
        public int VideoHeight { get; init; }

        /// <summary>
        /// Gets photo width in pixels; <c>0</c> when absent.
        /// </summary>
        public int PhotoWidth { get; init; }

        /// <summary>
        /// Gets photo height in pixels; <c>0</c> when absent.
        /// </summary>
        public int PhotoHeight { get; init; }

        /// <summary>
        /// Gets format-specific photo quality; <c>0</c> when absent.
        /// </summary>
        public int PhotoQuality { get; init; }

        /// <summary>
        /// Gets MPEG audio-header properties when a codec is <c>TagLib.Mpeg.AudioHeader</c>; otherwise <see langword="null"/>.
        /// </summary>
        public MpegAudioProperties? Mpeg { get; init; }
    }
}
