namespace Mfr.Models.Tags.Id3v1
{
    /// <summary>
    /// ID3v1 audio genre name ↔ index table (Winamp-extended list, matching TagLib Sharp 2.3.0).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Vendored so semantic/overlay merge can run in <c>Mfr.Models</c> without referencing TagLib.
    /// Unknown names map to <c>255</c>; out-of-range indexes yield <see langword="null"/>.
    /// </para>
    /// </remarks>
    public static class Id3v1Genres
    {
        private static readonly string[] _Audio =
        [
            "Blues",
            "Classic Rock",
            "Country",
            "Dance",
            "Disco",
            "Funk",
            "Grunge",
            "Hip-Hop",
            "Jazz",
            "Metal",
            "New Age",
            "Oldies",
            "Other",
            "Pop",
            "R&B",
            "Rap",
            "Reggae",
            "Rock",
            "Techno",
            "Industrial",
            "Alternative",
            "Ska",
            "Death Metal",
            "Pranks",
            "Soundtrack",
            "Euro-Techno",
            "Ambient",
            "Trip-Hop",
            "Vocal",
            "Jazz+Funk",
            "Fusion",
            "Trance",
            "Classical",
            "Instrumental",
            "Acid",
            "House",
            "Game",
            "Sound Clip",
            "Gospel",
            "Noise",
            "Alternative Rock",
            "Bass",
            "Soul",
            "Punk",
            "Space",
            "Meditative",
            "Instrumental Pop",
            "Instrumental Rock",
            "Ethnic",
            "Gothic",
            "Darkwave",
            "Techno-Industrial",
            "Electronic",
            "Pop-Folk",
            "Eurodance",
            "Dream",
            "Southern Rock",
            "Comedy",
            "Cult",
            "Gangsta",
            "Top 40",
            "Christian Rap",
            "Pop/Funk",
            "Jungle",
            "Native American",
            "Cabaret",
            "New Wave",
            "Psychedelic",
            "Rave",
            "Showtunes",
            "Trailer",
            "Lo-Fi",
            "Tribal",
            "Acid Punk",
            "Acid Jazz",
            "Polka",
            "Retro",
            "Musical",
            "Rock & Roll",
            "Hard Rock",
            "Folk",
            "Folk/Rock",
            "National Folk",
            "Swing",
            "Fusion",
            "Bebob",
            "Latin",
            "Revival",
            "Celtic",
            "Bluegrass",
            "Avantgarde",
            "Gothic Rock",
            "Progressive Rock",
            "Psychedelic Rock",
            "Symphonic Rock",
            "Slow Rock",
            "Big Band",
            "Chorus",
            "Easy Listening",
            "Acoustic",
            "Humour",
            "Speech",
            "Chanson",
            "Opera",
            "Chamber Music",
            "Sonata",
            "Symphony",
            "Booty Bass",
            "Primus",
            "Porn Groove",
            "Satire",
            "Slow Jam",
            "Club",
            "Tango",
            "Samba",
            "Folklore",
            "Ballad",
            "Power Ballad",
            "Rhythmic Soul",
            "Freestyle",
            "Duet",
            "Punk Rock",
            "Drum Solo",
            "A Cappella",
            "Euro-House",
            "Dance Hall",
            "Goa",
            "Drum & Bass",
            "Club-House",
            "Hardcore",
            "Terror",
            "Indie",
            "BritPop",
            "Negerpunk",
            "Polsk Punk",
            "Beat",
            "Christian Gangsta Rap",
            "Heavy Metal",
            "Black Metal",
            "Crossover",
            "Contemporary Christian",
            "Christian Rock",
            "Merengue",
            "Salsa",
            "Thrash Metal",
            "Anime",
            "Jpop",
            "Synthpop",
        ];

        /// <summary>
        /// Maps a genre name to its ID3v1 index.
        /// </summary>
        /// <param name="name">Genre display name (case-insensitive).</param>
        /// <returns>Index 0–147, or <c>255</c> when unrecognized.</returns>
        public static byte AudioToIndex(string name)
        {
            ArgumentNullException.ThrowIfNull(name);

            for (var i = 0; i < _Audio.Length; i++)
            {
                if (string.Equals(_Audio[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return (byte)i;
                }
            }

            return 255;
        }

        /// <summary>
        /// Maps an ID3v1 genre index to its display name.
        /// </summary>
        /// <param name="index">Genre byte from the ID3v1 trailer.</param>
        /// <returns>Genre name, or <see langword="null"/> when <paramref name="index"/> is out of range.</returns>
        public static string? IndexToAudio(byte index)
        {
            if (index >= _Audio.Length)
            {
                return null;
            }

            return _Audio[index];
        }
    }
}
