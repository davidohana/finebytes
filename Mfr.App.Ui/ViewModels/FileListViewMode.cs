namespace Mfr.App.Ui.ViewModels
{
    /// <summary>
    /// Layout used by the File Explorer item pane.
    /// </summary>
    public enum FileListViewMode
    {
        /// <summary>
        /// Wrapping grid with a large shell icon above the name.
        /// </summary>
        LargeIcons,

        /// <summary>
        /// Wrapping grid with a small shell icon to the left of the name.
        /// </summary>
        SmallIcons,

        /// <summary>
        /// Details grid with a Name column. This is the default.
        /// </summary>
        Report,

        /// <summary>
        /// Compact vertical list with a small icon and name.
        /// </summary>
        List,

        /// <summary>
        /// Wrapping grid with a large icon, name, and type/size details.
        /// </summary>
        Tiles,

        /// <summary>
        /// Wrapping grid with an image thumbnail or jumbo shell icon above the name.
        /// </summary>
        Thumbnails,
    }
}
