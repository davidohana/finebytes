namespace Mfr.App.Ui.ViewModels.AppliedFilters
{
    /// <summary>
    /// User-visible labels for modeled ID3v2 frame ids (Apply-To and field-setter combos).
    /// </summary>
    internal static class Id3v2FrameLabels
    {
        /// <summary>
        /// Returns <c>FRAMEID (Short name)</c> for a modeled frame, or the trimmed uppercase id when unknown.
        /// </summary>
        /// <param name="frameId">Four-character frame id (any casing).</param>
        /// <returns>Friendly label for pickers and Applied-list subtitles.</returns>
        public static string For(string frameId)
        {
            if (string.IsNullOrWhiteSpace(frameId))
            {
                return string.Empty;
            }

            var normalized = frameId.Trim().ToUpperInvariant();
            return normalized switch
            {
                "TALB" => "TALB (Album)",
                "TBPM" => "TBPM (BPM)",
                "TCOM" => "TCOM (Composer)",
                "TCON" => "TCON (Genre)",
                "TCOP" => "TCOP (Copyright)",
                "TDAT" => "TDAT (Date)",
                "TDEN" => "TDEN (Encoding Time)",
                "TDOR" => "TDOR (Original Release Time)",
                "TDRC" => "TDRC (Recording Date)",
                "TDRL" => "TDRL (Release Time)",
                "TDTG" => "TDTG (Tagging Time)",
                "TENC" => "TENC (Encoded By)",
                "TEXT" => "TEXT (Lyricist)",
                "TFLT" => "TFLT (File Type)",
                "TIPL" => "TIPL (Involved People)",
                "TIT1" => "TIT1 (Grouping)",
                "TIT2" => "TIT2 (Title)",
                "TIT3" => "TIT3 (Subtitle)",
                "TKEY" => "TKEY (Initial Key)",
                "TLAN" => "TLAN (Language(s))",
                "TLEN" => "TLEN (Length)",
                "TMED" => "TMED (Media Type)",
                "TMOO" => "TMOO (Mood)",
                "TOAL" => "TOAL (Original Album)",
                "TOFN" => "TOFN (Original Filename)",
                "TOLY" => "TOLY (Original Lyricist)",
                "TOPE" => "TOPE (Original Artist)",
                "TORY" => "TORY (Original Year)",
                "TOWN" => "TOWN (File Owner)",
                "TPE1" => "TPE1 (Artist)",
                "TPE2" => "TPE2 (Album Artist)",
                "TPE3" => "TPE3 (Conductor)",
                "TPE4" => "TPE4 (Remixer)",
                "TPOS" => "TPOS (Disc)",
                "TPUB" => "TPUB (Publisher)",
                "TRCK" => "TRCK (Track)",
                "TRDA" => "TRDA (Recording Dates)",
                "TRSN" => "TRSN (Radio Station Name)",
                "TRSO" => "TRSO (Radio Station Owner)",
                "TSIZ" => "TSIZ (Size)",
                "TSOA" => "TSOA (Album Sort)",
                "TSOP" => "TSOP (Performer Sort)",
                "TSSE" => "TSSE (Encoder Settings)",
                "TSST" => "TSST (Set Subtitle)",
                "TYER" => "TYER (Year)",
                "COMM" => "COMM (Comment)",
                "USLT" => "USLT (Lyrics)",
                "TXXX" => "TXXX (Custom)",
                _ => normalized,
            };
        }
    }
}
