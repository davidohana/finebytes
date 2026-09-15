namespace Mfr.Models.Tags.Id3v2
{
    /// <summary>
    /// User-visible labels for modeled ID3v2 frame ids (Apply-To and field-setter combos).
    /// </summary>
    public static class Id3v2FrameLabels
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
            var shortName = _ShortName(normalized);
            if (shortName is null)
            {
                return normalized;
            }

            return $"{normalized} ({shortName})";
        }

        /// <summary>
        /// Friendly short name for a known modeled frame id, or <see langword="null"/> when unknown.
        /// </summary>
        private static string? _ShortName(string normalizedFrameId)
        {
            return normalizedFrameId switch
            {
                "TALB" => "Album",
                "TBPM" => "BPM",
                "TCOM" => "Composer",
                "TCON" => "Genre",
                "TCOP" => "Copyright",
                "TDAT" => "Date",
                "TDEN" => "Encoding Time",
                "TDOR" => "Original Release Time",
                "TDRC" => "Recording Date",
                "TDRL" => "Release Time",
                "TDTG" => "Tagging Time",
                "TENC" => "Encoded By",
                "TEXT" => "Lyricist",
                "TFLT" => "File Type",
                "TIPL" => "Involved People",
                "TIT1" => "Grouping",
                "TIT2" => "Title",
                "TIT3" => "Subtitle",
                "TKEY" => "Initial Key",
                "TLAN" => "Language(s)",
                "TLEN" => "Length",
                "TMED" => "Media Type",
                "TMOO" => "Mood",
                "TOAL" => "Original Album",
                "TOFN" => "Original Filename",
                "TOLY" => "Original Lyricist",
                "TOPE" => "Original Artist",
                "TORY" => "Original Year",
                "TOWN" => "File Owner",
                "TPE1" => "Artist",
                "TPE2" => "Album Artist",
                "TPE3" => "Conductor",
                "TPE4" => "Remixer",
                "TPOS" => "Disc",
                "TPUB" => "Publisher",
                "TRCK" => "Track",
                "TRDA" => "Recording Dates",
                "TRSN" => "Radio Station Name",
                "TRSO" => "Radio Station Owner",
                "TSIZ" => "Size",
                "TSOA" => "Album Sort",
                "TSOP" => "Performer Sort",
                "TSSE" => "Encoder Settings",
                "TSST" => "Set Subtitle",
                "TYER" => "Year",
                "COMM" => "Comment",
                "USLT" => "Lyrics",
                "TXXX" => "Custom",
                _ => null,
            };
        }
    }
}
