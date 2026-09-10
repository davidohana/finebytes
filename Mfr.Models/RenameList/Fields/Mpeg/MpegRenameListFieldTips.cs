namespace Mfr.Models.RenameList.Fields.Mpeg
{
    /// <summary>
    /// Clarifying tooltips for MP3 Properties Rename List columns and format tokens.
    /// </summary>
    public static class MpegRenameListFieldTips
    {
        /// <summary>Formatted duration column.</summary>
        public const string Duration = "MPEG header duration as h:mm:ss.";

        /// <summary>Whole-second duration column.</summary>
        public const string DurationSecs = "Same MPEG header duration as a whole-second number.";

        /// <summary>Copyright header bit.</summary>
        public const string Copyright = "MPEG header copyright bit — not the audio-tag Copyright text.";

        /// <summary>Original/copy header bit.</summary>
        public const string Original = "MPEG header original/copy bit.";

        /// <summary>CRC protection header bit.</summary>
        public const string Protection = "MPEG header CRC protection bit.";

        /// <summary>Sample-rate column labeled Frequency.</summary>
        public const string Frequency = "Sample rate in Hz (MPEG header).";

        /// <summary>MPEG version column labeled Level.</summary>
        public const string Level = "MPEG version (e.g. Version 1).";

        /// <summary>MPEG audio layer.</summary>
        public const string Layer = "MPEG audio layer (I / II / III).";
    }
}
