namespace Mfr.Models.Tags.Id3v2
{
    /// <summary>
    /// User-facing tooltips for modeled ID3v2 frames and the Version column.
    /// </summary>
    public static class Id3v2FrameTips
    {
        private static readonly Dictionary<string, string> s_FrameIdToTip = _BuildMap();

        /// <summary>
        /// Tooltip for the read-only ID3v2 version Rename List column and <c>&lt;id3v2-version&gt;</c> token.
        /// </summary>
        public const string Version = "ID3v2 tag major version on disk (shown as 2.3 / 2.4).";

        /// <summary>
        /// Returns the tooltip for a modeled frame id.
        /// </summary>
        /// <param name="frameId">Four-character frame id (any casing).</param>
        /// <returns>User-visible tooltip text.</returns>
        public static string For(string frameId)
        {
            if (string.IsNullOrWhiteSpace(frameId))
            {
                return string.Empty;
            }

            var normalized = frameId.Trim().ToUpperInvariant();
            return s_FrameIdToTip.TryGetValue(normalized, out var tip) ? tip : $"ID3v2 frame {normalized}.";
        }

        private static Dictionary<string, string> _BuildMap()
        {
            var frameIdToTip = new Dictionary<string, string>(StringComparer.Ordinal);

            void Add(string frameId, string tip)
            {
                frameIdToTip[frameId] = tip;
            }

            void AddSemantic(string frameId, SemanticAudioField field)
            {
                Add(frameId, _WithFrameId(SemanticAudioFieldTips.For(field), frameId));
            }

            AddSemantic("TIT2", SemanticAudioField.Title);
            AddSemantic("TIT1", SemanticAudioField.Grouping);
            AddSemantic("TALB", SemanticAudioField.Album);
            AddSemantic("TPE1", SemanticAudioField.Performers);
            AddSemantic("TPE2", SemanticAudioField.AlbumArtists);
            AddSemantic("TPE3", SemanticAudioField.Conductor);
            AddSemantic("TCOM", SemanticAudioField.Composers);
            AddSemantic("TCON", SemanticAudioField.Genre);
            AddSemantic("TCOP", SemanticAudioField.Copyright);
            AddSemantic("TBPM", SemanticAudioField.BeatsPerMinute);

            Add("TRCK", _WithFrameId("Track number, optionally as n/total", "TRCK"));
            Add("TPOS", _WithFrameId("Disc number, optionally as n/total", "TPOS"));
            Add("TYER", _WithFrameId(SemanticAudioFieldTips.Year, "TYER; v2.3"));
            Add("TDRC", "Recording time (ID3v2 TDRC; v2.4).");
            Add("TDAT", "Date (ID3v2 TDAT; v2.3 DDMM).");
            Add("TDOR", "Original release time (ID3v2 TDOR; v2.4).");
            Add("TDRL", "Release time (ID3v2 TDRL; v2.4).");
            Add("TDEN", "Encoding time (ID3v2 TDEN; v2.4).");
            Add("TDTG", "Tagging time (ID3v2 TDTG; v2.4).");
            Add(
                "COMM",
                _WithFrameId(
                    $"{SemanticAudioFieldTips.Comment.TrimEnd('.')}; Rename List / primary Apply-To use the primary instance",
                    "COMM"
                )
            );
            Add(
                "USLT",
                _WithFrameId(
                    $"{SemanticAudioFieldTips.Lyrics.TrimEnd('.')}; primary instance when language/description omitted",
                    "USLT"
                )
            );
            Add("TXXX", "User-defined text; primary instance when description omitted (ID3v2 TXXX).");
            Add("TSIZ", "Size of the audio in bytes (ID3v2 TSIZ; obsolete in v2.4).");
            Add("TRDA", "Recording dates (ID3v2 TRDA; obsolete in v2.4).");

            foreach (var (frameId, shortName) in Id3v2ModeledFrame.FrameIdToShortName)
            {
                if (!frameIdToTip.ContainsKey(frameId))
                {
                    Add(frameId, $"{shortName} (ID3v2 {frameId}).");
                }
            }

            return frameIdToTip;
        }

        /// <summary>
        /// Appends the frame id in parentheses after a field meaning (strips a trailing period first).
        /// </summary>
        private static string _WithFrameId(string meaning, string frameId)
        {
            return $"{meaning.TrimEnd('.')} (ID3v2 {frameId}).";
        }
    }
}
