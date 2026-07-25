namespace Mfr.Models.Tags
{
    /// <summary>
    /// The physical audio container a rename row is backed by, which decides the tag blocks it can hold.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Detected once per preview cycle when embedded tags load. Containers we do not model resolve to
    /// <see cref="Unknown"/> and support no tag blocks.
    /// </para>
    /// </remarks>
    public enum AudioContainerFormat
    {
        /// <summary>Container is missing, unreadable, or not one this application models.</summary>
        Unknown,

        /// <summary>MPEG audio (MP3).</summary>
        Mpeg,

        /// <summary>Native FLAC stream.</summary>
        Flac,

        /// <summary>Ogg container (Vorbis, Opus, Ogg FLAC).</summary>
        Ogg,

        /// <summary>ISO base-media container (MP4, M4A, M4B).</summary>
        Mpeg4,

        /// <summary>Advanced Systems Format (WMA, WMV).</summary>
        Asf,

        /// <summary>RIFF container (WAV).</summary>
        Riff,

        /// <summary>Monkey's Audio stream (APE).</summary>
        Ape,
    }
}
