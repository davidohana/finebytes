namespace Mfr.Models.Tags.Id3v1
{
    /// <summary>
    /// User-facing tooltips for every <see cref="Id3v1Field"/>.
    /// </summary>
    public static class Id3v1FieldTips
    {
        /// <summary>
        /// Returns the tooltip for <paramref name="field"/>.
        /// </summary>
        /// <param name="field">ID3v1 scalar.</param>
        /// <returns>User-visible tooltip text.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="field"/> is not mapped.</exception>
        public static string For(Id3v1Field field)
        {
            return field switch
            {
                Id3v1Field.Title => "Track title in the ID3v1 tag (max ~30 characters on disk).",
                Id3v1Field.Artist => "Artist name in the ID3v1 tag (max ~30 characters on disk).",
                Id3v1Field.Album => "Album title in the ID3v1 tag (max ~30 characters on disk).",
                Id3v1Field.Year => "Four-digit year in the ID3v1 tag.",
                Id3v1Field.Comment => "Comment in the ID3v1 tag (slightly shorter when a track number is also stored).",
                Id3v1Field.Track => "Track number in the ID3v1 tag (ID3v1.1).",
                Id3v1Field.Genre => "Genre name from the ID3v1 genre list (empty when unset).",
                _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Missing ID3v1 field tip."),
            };
        }
    }
}
