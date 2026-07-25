using Mfr.Models.Tags.Id3v1;
using TagLib;
using Id3v1Tag = TagLib.Id3v1.Tag;

namespace Mfr.Metadata.TagFields
{
    /// <summary>
    /// Reads and writes the fixed ID3v1 scalar fields on a live TagLib tag.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The trailer has no per-field presence, so there is nothing to diff: any change rewrites all scalars.
    /// Numeric fields clear to <c>0</c> on disk and read back as <see langword="null"/>.
    /// </para>
    /// </remarks>
    internal static class Id3v1TagFields
    {
        /// <summary>
        /// Reads the file's ID3v1 scalars.
        /// </summary>
        /// <param name="file">Open TagLib file.</param>
        /// <returns>Block data, or <see langword="null"/> when the tag is absent or holds no values.</returns>
        public static Id3v1TagData? Read(TagLib.File file)
        {
            if (file.GetTag(TagTypes.Id3v1, false) is not Id3v1Tag live)
                return null;

            if (_IsEffectivelyEmpty(live))
                return null;

            var genreByte = live.FirstGenre is null ? (byte)0 : Genres.AudioToIndex(live.FirstGenre);

            return new Id3v1TagData
            {
                Title = TagFieldText.NullIfEmpty(live.Title),
                Artist = TagFieldText.NullIfEmpty(live.FirstPerformer),
                Album = TagFieldText.NullIfEmpty(live.Album),
                Year = live.Year == 0 ? null : live.Year,
                Comment = TagFieldText.NullIfEmpty(live.Comment),
                Track = live.Track == 0 ? null : (byte)Math.Min(live.Track, 255u),
                Genre = genreByte,
            };
        }

        /// <summary>
        /// Creates or rewrites the file's ID3v1 scalars from <paramref name="original"/> → <paramref name="preview"/>.
        /// </summary>
        /// <param name="file">Open TagLib file.</param>
        /// <param name="original">Block as read from disk, or <see langword="null"/> to create.</param>
        /// <param name="preview">Block the preview wants on disk.</param>
        public static void Apply(TagLib.File file, Id3v1TagData? original, Id3v1TagData preview)
        {
            if (Equals(original, preview))
                return;

            var live = (Id3v1Tag)file.GetTag(TagTypes.Id3v1, true);

            live.Title = preview.Title ?? string.Empty;
            live.Performers = string.IsNullOrWhiteSpace(preview.Artist) ? [] : [preview.Artist.Trim()];
            live.Album = preview.Album ?? string.Empty;
            live.Year = preview.Year ?? 0;
            live.Comment = preview.Comment ?? string.Empty;
            live.Track = preview.Track ?? 0;

            var genreName = Id3v1Genres.IndexToAudio(preview.Genre);
            live.Genres = string.IsNullOrEmpty(genreName) ? [] : [genreName];
        }

        private static bool _IsEffectivelyEmpty(Id3v1Tag live)
        {
            return string.IsNullOrWhiteSpace(live.Title)
                && (live.Performers.Length == 0 || string.IsNullOrWhiteSpace(live.FirstPerformer))
                && string.IsNullOrWhiteSpace(live.Album)
                && live.Year == 0
                && string.IsNullOrWhiteSpace(live.Comment)
                && live.Track == 0
                && (live.Genres.Length == 0 || string.IsNullOrWhiteSpace(live.FirstGenre));
        }
    }
}
