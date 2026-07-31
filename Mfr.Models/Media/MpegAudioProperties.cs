namespace Mfr.Models.Media
{
    /// <summary>
    /// Read-only TagLib MPEG audio-header snapshot for <c>mpeg-*</c> formatter tokens.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Populated lazily when a codec is <c>TagLib.Mpeg.AudioHeader</c>; never written back.
    /// Integer fields use <c>0</c> for absent (TagLib convention).
    /// </para>
    /// </remarks>
    public sealed record MpegAudioProperties
    {
        /// <summary>
        /// Gets audio bitrate in kbps; <c>0</c> when absent.
        /// </summary>
        public int Bitrate { get; init; }

        /// <summary>
        /// Gets whether the MPEG copyright bit is set.
        /// </summary>
        public bool IsCopyrighted { get; init; }

        /// <summary>
        /// Gets the MPEG-header duration; <see cref="TimeSpan.Zero"/> when unknown.
        /// </summary>
        public TimeSpan Duration { get; init; }

        /// <summary>
        /// Gets whether a Xing or VBRI VBR header is present.
        /// </summary>
        public bool IsVbr { get; init; }

        /// <summary>
        /// Gets sample rate in Hz; <c>0</c> when absent.
        /// </summary>
        public int SampleRate { get; init; }

        /// <summary>
        /// Gets MPEG audio layer (1–3); <c>0</c> when unset.
        /// </summary>
        public int Layer { get; init; }

        /// <summary>
        /// Gets MPEG version text (<c>1</c>, <c>2</c>, or <c>2.5</c>); empty when unknown.
        /// </summary>
        public string MpegVersion { get; init; } = string.Empty;

        /// <summary>
        /// Gets channel mode text (e.g. <c>Stereo</c>, <c>JointStereo</c>); empty when unset.
        /// </summary>
        public string ChannelMode { get; init; } = string.Empty;

        /// <summary>
        /// Gets whether the MPEG original bit is set.
        /// </summary>
        public bool IsOriginal { get; init; }

        /// <summary>
        /// Gets whether the MPEG CRC protection bit is set.
        /// </summary>
        public bool IsProtected { get; init; }
    }
}
