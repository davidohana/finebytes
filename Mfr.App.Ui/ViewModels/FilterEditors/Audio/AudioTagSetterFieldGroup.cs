namespace Mfr.App.Ui.ViewModels.FilterEditors.Audio
{
    /// <summary>
    /// Fieldset area for Audio Tag Setter options.
    /// </summary>
    internal enum AudioTagSetterFieldGroup
    {
        /// <summary>Track and disc indexes / counts.</summary>
        TrackDisc,

        /// <summary>Core identity tags (title, album, artists, year, genre, comment).</summary>
        Basic,

        /// <summary>Extended tags (credits, BPM, lyrics, …).</summary>
        Extended,
    }
}
