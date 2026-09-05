namespace Mfr.App.Ui.ViewModels.FilterEditors.Audio
{
    /// <summary>
    /// One configurable overlay field in the Audio Tag Setter editor.
    /// </summary>
    internal enum AudioTagSetterFieldKind
    {
        /// <summary>Track index.</summary>
        Track,

        /// <summary>Track count (of n/m).</summary>
        TrackCount,

        /// <summary>Disc index.</summary>
        Disc,

        /// <summary>Disc count.</summary>
        DiscCount,

        /// <summary>Primary performers.</summary>
        Performers,

        /// <summary>Album artists.</summary>
        AlbumArtists,

        /// <summary>Track title.</summary>
        Title,

        /// <summary>Album name.</summary>
        Album,

        /// <summary>Release year.</summary>
        Year,

        /// <summary>Genre(s).</summary>
        Genre,

        /// <summary>Comment.</summary>
        Comment,

        /// <summary>Composer(s).</summary>
        Composers,

        /// <summary>Conductor.</summary>
        Conductor,

        /// <summary>Content group / work title.</summary>
        Grouping,

        /// <summary>Copyright notice.</summary>
        Copyright,

        /// <summary>Tempo in BPM.</summary>
        BeatsPerMinute,

        /// <summary>Unsynchronised lyrics.</summary>
        Lyrics,
    }
}
