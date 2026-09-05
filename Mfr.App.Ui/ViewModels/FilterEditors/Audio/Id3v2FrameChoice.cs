using Mfr.Models.Tags.Id3v2;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Audio
{
    /// <summary>
    /// ComboBox row for a modeled ID3v2 frame id with a user-visible label.
    /// </summary>
    /// <param name="FrameId">Four-character frame id (uppercase).</param>
    /// <param name="DisplayName">Label shown in the field picker (e.g. <c>TIT2 (Title/songname/content description)</c>).</param>
    internal sealed record Id3v2FrameChoice(string FrameId, string DisplayName)
    {
        /// <summary>
        /// Modeled frame rows in Apply-To order (<see cref="Id3v2ModeledFrame.AllModeledFrameIds"/>).
        /// </summary>
        public static IReadOnlyList<Id3v2FrameChoice> All { get; } =
        [.. Id3v2ModeledFrame.AllModeledFrameIds.Select(id => new Id3v2FrameChoice(id, _LabelFor(id)))];

        /// <summary>
        /// Default frame row (<c>TIT2</c>) used for empty/unknown ids.
        /// </summary>
        public static Id3v2FrameChoice Tit2 { get; } = All.First(c => c.FrameId == "TIT2");

        /// <summary>
        /// Returns the combo row for <paramref name="frameId"/>, or TIT2 when unknown/empty.
        /// </summary>
        /// <param name="frameId">Frame id from filter options (any casing).</param>
        /// <returns>Matching choice, or the TIT2 row as fallback.</returns>
        public static Id3v2FrameChoice For(string? frameId)
        {
            if (string.IsNullOrWhiteSpace(frameId))
            {
                return Tit2;
            }

            var normalized = frameId.Trim().ToUpperInvariant();
            foreach (var choice in All)
            {
                if (choice.FrameId == normalized)
                {
                    return choice;
                }
            }

            return Tit2;
        }

        /// <summary>
        /// Gets whether the language box applies (<c>COMM</c> / <c>USLT</c>).
        /// </summary>
        public bool ShowsLanguage => FrameId is "COMM" or "USLT";

        /// <summary>
        /// Gets whether the description box applies (<c>COMM</c> / <c>USLT</c> / <c>TXXX</c>).
        /// </summary>
        public bool ShowsDescription => Id3v2ModeledFrame.MultiInstanceFrameIds.Contains(FrameId);

        /// <inheritdoc />
        public override string ToString()
        {
            return DisplayName;
        }

        private static string _LabelFor(string frameId)
        {
            return frameId switch
            {
                "TALB" => "TALB (Album/Movie/Show title)",
                "TBPM" => "TBPM (BPM)",
                "TCOM" => "TCOM (Composer)",
                "TCON" => "TCON (Genre)",
                "TCOP" => "TCOP (Copyright)",
                "TDAT" => "TDAT (Date)",
                "TDEN" => "TDEN (Encoding Time)",
                "TDOR" => "TDOR (Original Release Time)",
                "TDRC" => "TDRC (Recording Time)",
                "TDRL" => "TDRL (Release Time)",
                "TDTG" => "TDTG (Tagging Time)",
                "TENC" => "TENC (Encoded by)",
                "TEXT" => "TEXT (Lyricist/Text writer)",
                "TFLT" => "TFLT (File type)",
                "TIPL" => "TIPL (Involved People List)",
                "TIT1" => "TIT1 (Content group description)",
                "TIT2" => "TIT2 (Title/songname/content description)",
                "TIT3" => "TIT3 (Subtitle/Description refinement)",
                "TKEY" => "TKEY (Initial key)",
                "TLAN" => "TLAN (Language(s))",
                "TLEN" => "TLEN (Length)",
                "TMED" => "TMED (Media type)",
                "TMOO" => "TMOO (Mood)",
                "TOAL" => "TOAL (Original album/movie/show title)",
                "TOFN" => "TOFN (Original filename)",
                "TOLY" => "TOLY (Original lyricist(s)/text writer(s))",
                "TOPE" => "TOPE (Original artist(s)/performer(s))",
                "TORY" => "TORY (Original release year)",
                "TOWN" => "TOWN (File Owner)",
                "TPE1" => "TPE1 (Lead performer(s)/Soloist(s))",
                "TPE2" => "TPE2 (Band/orchestra/accompaniment)",
                "TPE3" => "TPE3 (Conductor/performer refinement)",
                "TPE4" => "TPE4 (Interpreted, remixed, or otherwise modified by)",
                "TPOS" => "TPOS (Part of a set)",
                "TPUB" => "TPUB (Publisher)",
                "TRCK" => "TRCK (Track number/Position in set)",
                "TRDA" => "TRDA (Recording dates)",
                "TRSN" => "TRSN (Internet radio station name)",
                "TRSO" => "TRSO (Internet radio station owner)",
                "TSIZ" => "TSIZ (Size)",
                "TSOA" => "TSOA (Album Sort Order)",
                "TSOP" => "TSOP (Performer Sort Order)",
                "TSSE" => "TSSE (Software/Hardware and settings used for encoding)",
                "TSST" => "TSST (Set Subtitle)",
                "TYER" => "TYER (Year)",
                "COMM" => "COMM (Comments)",
                "USLT" => "USLT (Unsynchronized lyrics)",
                "TXXX" => "TXXX (User defined text information)",
                _ => frameId,
            };
        }
    }
}
