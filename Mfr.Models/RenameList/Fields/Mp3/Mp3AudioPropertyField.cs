namespace Mfr.Models.RenameList.Fields.Mp3
{
    /// <summary>
    /// MPEG audio-header properties shared by formatter tokens and Rename List columns.
    /// <para>
    /// Member names match <c>mp3-*</c> token vocabulary (including
    /// <see cref="Encoding"/>, <see cref="Ver"/>, <see cref="DurationSec"/>). Catalog
    /// keys that differ are mapped in
    /// <see cref="Mp3PropertyRenameListField.CatalogPropertyKey"/>; DTO accessors live in
    /// <see cref="Mp3AudioPropertiesFormatting"/>.
    /// </para>
    /// </summary>
    internal enum Mp3AudioPropertyField
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

        /// <summary>MPEG version (<c>mp3-ver</c>).</summary>
        Ver,

        /// <summary>Channel mode.</summary>
        Mode,

        /// <summary>MPEG header original/copy bit.</summary>
        Original,

        /// <summary>MPEG header CRC protection bit.</summary>
        Protection,
    }
}
