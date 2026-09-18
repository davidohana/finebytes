namespace Mfr.Models.Tags.Id3v2
{
    /// <summary>
    /// User-facing tooltips for modeled ID3v2 frames and the Version column.
    /// </summary>
    public static class Id3v2FrameTips
    {
        private static readonly Dictionary<string, string> s_FrameIdToTip = _BuildMap();

        /// <summary>
        /// Tooltip for the read-only ID3v2 version Rename List column.
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

            // Prefer specific wording for common / multi-instance / structured frames.
            Add("TIT2", "Track title (ID3v2 TIT2).");
            Add("TIT1", "Content group / work set (ID3v2 TIT1).");
            Add("TIT3", "Subtitle / description refinement (ID3v2 TIT3).");
            Add("TALB", "Album / release title (ID3v2 TALB).");
            Add("TPE1", "Lead performer(s) / artist (ID3v2 TPE1).");
            Add("TPE2", "Band / orchestra / album artist (ID3v2 TPE2).");
            Add("TPE3", "Conductor / performer refinement (ID3v2 TPE3).");
            Add("TPE4", "Interpreted, remixed, or otherwise modified by (ID3v2 TPE4).");
            Add("TCOM", "Composer (ID3v2 TCOM).");
            Add("TCON", "Content type / genre (ID3v2 TCON).");
            Add("TCOP", "Copyright message (ID3v2 TCOP).");
            Add("TBPM", "Beats per minute (ID3v2 TBPM).");
            Add("TRCK", "Track number, optionally as n/total (ID3v2 TRCK).");
            Add("TPOS", "Disc number, optionally as n/total (ID3v2 TPOS).");
            Add("TYER", "Year of recording (ID3v2 TYER; v2.3).");
            Add("TDRC", "Recording time (ID3v2 TDRC; v2.4).");
            Add("TDAT", "Date (ID3v2 TDAT; v2.3 DDMM).");
            Add("TORY", "Original release year (ID3v2 TORY).");
            Add("TDOR", "Original release time (ID3v2 TDOR; v2.4).");
            Add("TDRL", "Release time (ID3v2 TDRL; v2.4).");
            Add("TDEN", "Encoding time (ID3v2 TDEN; v2.4).");
            Add("TDTG", "Tagging time (ID3v2 TDTG; v2.4).");
            Add("COMM", "Comment; Rename List / primary Apply-To use the primary instance (ID3v2 COMM).");
            Add("USLT", "Unsynchronised lyrics; primary instance when language/description omitted (ID3v2 USLT).");
            Add("TXXX", "User-defined text; primary instance when description omitted (ID3v2 TXXX).");
            Add("TEXT", "Lyricist / text writer (ID3v2 TEXT).");
            Add("TOLY", "Original lyricist / text writer (ID3v2 TOLY).");
            Add("TOPE", "Original artist / performer (ID3v2 TOPE).");
            Add("TOAL", "Original album title (ID3v2 TOAL).");
            Add("TOFN", "Original filename (ID3v2 TOFN).");
            Add("TOWN", "File owner / licensee (ID3v2 TOWN).");
            Add("TPUB", "Publisher (ID3v2 TPUB).");
            Add("TRSN", "Internet radio station name (ID3v2 TRSN).");
            Add("TRSO", "Internet radio station owner (ID3v2 TRSO).");
            Add("TENC", "Encoded by (ID3v2 TENC).");
            Add("TSSE", "Software / hardware and settings used for encoding (ID3v2 TSSE).");
            Add("TFLT", "File type (ID3v2 TFLT).");
            Add("TMED", "Media type (ID3v2 TMED).");
            Add("TMOO", "Mood (ID3v2 TMOO).");
            Add("TKEY", "Initial musical key (ID3v2 TKEY).");
            Add("TLAN", "Language(s) (ID3v2 TLAN).");
            Add("TLEN", "Length of the audio in milliseconds (ID3v2 TLEN).");
            Add("TSIZ", "Size of the audio in bytes (ID3v2 TSIZ; obsolete in v2.4).");
            Add("TIPL", "Involved people list (ID3v2 TIPL).");
            Add("TRDA", "Recording dates (ID3v2 TRDA; obsolete in v2.4).");
            Add("TSOA", "Album sort order (ID3v2 TSOA).");
            Add("TSOP", "Performer sort order (ID3v2 TSOP).");
            Add("TSST", "Set subtitle (ID3v2 TSST).");

            // Any modeled frame missing above still gets a short-name-based tip.
            foreach (var (frameId, shortName) in Id3v2ModeledFrame.FrameIdToShortName)
            {
                if (!frameIdToTip.ContainsKey(frameId))
                {
                    Add(frameId, $"{shortName} (ID3v2 {frameId}).");
                }
            }

            return frameIdToTip;
        }
    }
}
