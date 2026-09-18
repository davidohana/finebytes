namespace Mfr.Models.RenameList.Fields.Mpeg
{
    /// <summary>
    /// MPEG audio-header properties shared by formatter tokens and Rename List columns.
    /// <para>
    /// Member names match <c>mpeg-*</c> token vocabulary (including
    /// <see cref="Encoding"/>, <see cref="MpegVer"/>, <see cref="DurationSec"/>). Catalog
    /// keys that differ are mapped in
    /// <see cref="MpegPropertyRenameListField.CatalogPropertyKey"/>; DTO accessors live in
    /// <see cref="MpegAudioPropertiesFormatting"/>.
    /// </para>
    /// </summary>
    internal enum MpegAudioPropertyField
    {
        /// <summary>Audio bitrate in kbps (VBR-prefixed when applicable).</summary>
        Bitrate,

        /// <summary>MPEG header copyright bit.</summary>
        Copyright,

        /// <summary>Header duration.</summary>
        Duration,

        /// <summary>Header duration in whole seconds.</summary>
        DurationSec,

        /// <summary>CBR or VBR encoding.</summary>
        Encoding,

        /// <summary>Sample rate in Hz.</summary>
        Frequency,

        /// <summary>MPEG audio layer.</summary>
        Layer,

        /// <summary>MPEG version.</summary>
        MpegVer,

        /// <summary>Channel mode.</summary>
        Mode,

        /// <summary>MPEG header original/copy bit.</summary>
        Original,

        /// <summary>MPEG header CRC protection bit.</summary>
        Protection,
    }
}
