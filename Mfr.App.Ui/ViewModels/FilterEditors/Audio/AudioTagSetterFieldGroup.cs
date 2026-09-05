namespace Mfr.App.Ui.ViewModels.FilterEditors.Audio
{
    /// <summary>
    /// Fieldset area for Audio Tag Setter options (display order).
    /// </summary>
    internal enum AudioTagSetterFieldGroup
    {
        /// <summary>Core identity tags (title, album, artists, year, genre, comment).</summary>
        Basic,

        /// <summary>Track and disc indexes / counts.</summary>
        TrackDisc,

        /// <summary>Extended tags (credits, BPM, lyrics, …).</summary>
        Extended,
    }
}
