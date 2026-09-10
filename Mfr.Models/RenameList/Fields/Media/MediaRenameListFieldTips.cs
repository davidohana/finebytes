namespace Mfr.Models.RenameList.Fields.Media
{
    /// <summary>
    /// Clarifying tooltips for Media Properties Rename List columns and format tokens.
    /// </summary>
    public static class MediaRenameListFieldTips
    {
        /// <summary>Formatted duration column.</summary>
        public const string Duration = "Formatted duration (h:mm:ss).";

        /// <summary>Whole-second duration column.</summary>
        public const string DurationSeconds = "Same duration as a whole-second number.";

        /// <summary>Possibly-corrupt flag.</summary>
        public const string PossiblyCorrupt = "TagLib flagged the file as possibly corrupt.";

        /// <summary>Video frame width.</summary>
        public const string VideoWidth = "Video stream width in pixels (not still-image / cover art).";

        /// <summary>Video frame height.</summary>
        public const string VideoHeight = "Video stream height in pixels (not still-image / cover art).";

        /// <summary>Photo / still width.</summary>
        public const string PhotoWidth = "Still-image or embedded photo width (not video frame size).";

        /// <summary>Photo / still height.</summary>
        public const string PhotoHeight = "Still-image or embedded photo height (not video frame size).";
    }
}
