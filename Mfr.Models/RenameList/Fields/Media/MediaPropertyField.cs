namespace Mfr.Models.RenameList.Fields.Media
{
    /// <summary>
    /// Media properties shared by formatter tokens and Rename List columns.
    /// <para>
    /// Member names match <c>media-*</c> token vocabulary (including
    /// <see cref="Corrupt"/>, <see cref="DurationSec"/>, <see cref="SampleRate"/>,
    /// <see cref="Channels"/>). Catalog keys and DTO names that differ are mapped in
    /// <see cref="MediaPropertyRenameListField.CatalogPropertyKey"/> and
    /// <see cref="MediaPropertiesFormatting"/>.
    /// </para>
    /// </summary>
    internal enum MediaPropertyField
    {
        /// <summary>TagLib MIME type.</summary>
        MimeType,

        /// <summary>Whether TagLib marked the file as possibly corrupt.</summary>
        Corrupt,

        /// <summary>Media duration.</summary>
        Duration,

        /// <summary>Media duration in whole seconds.</summary>
        DurationSec,

        /// <summary>TagLib media-type flags as text.</summary>
        MediaTypes,

        /// <summary>Aggregate codec description.</summary>
        Description,

        /// <summary>Audio bitrate in kbps.</summary>
        AudioBitrate,

        /// <summary>Audio sample rate in Hz.</summary>
        SampleRate,

        /// <summary>Bits per sample.</summary>
        BitsPerSample,

        /// <summary>Audio channel count.</summary>
        Channels,

        /// <summary>Video frame width in pixels.</summary>
        VideoWidth,

        /// <summary>Video frame height in pixels.</summary>
        VideoHeight,

        /// <summary>Photo width in pixels.</summary>
        PhotoWidth,

        /// <summary>Photo height in pixels.</summary>
        PhotoHeight,

        /// <summary>Format-specific photo quality.</summary>
        PhotoQuality,
    }
}
