using Mfr.Models.Tags;
using TagLib;

namespace Mfr.Metadata
{
    /// <summary>
    /// TagLib adapters for <see cref="SemanticAudioTag"/>.
    /// </summary>
    public static class SemanticAudioTagTagLib
    {
        /// <summary>
        /// Projects common fields from a live TagLib tag (combined or single-type).
        /// </summary>
        /// <param name="tag">TagLib tag whose string/list/numeric fields are read.</param>
        /// <returns>Common fields reconstructed from the tag's strings/lists and numerics.</returns>
        public static SemanticAudioTag FromCombinedTag(Tag tag)
        {
            ArgumentNullException.ThrowIfNull(tag);

            return new SemanticAudioTag(
                Title: _NullIfWhitespace(tag.Title),
                Album: _NullIfWhitespace(tag.Album),
                Performers: _JoinList(tag.Performers),
                AlbumArtists: _JoinList(tag.AlbumArtists),
                Composers: _JoinList(tag.Composers),
                Genre: tag.Genres.Length == 0 ? null : _NullIfWhitespace(tag.Genres[0]),
                Comment: _NullIfWhitespace(tag.Comment),
                Lyrics: _NullIfWhitespace(tag.Lyrics),
                Copyright: _NullIfWhitespace(tag.Copyright),
                Grouping: _NullIfWhitespace(tag.Grouping),
                Year: tag.Year == 0 ? null : tag.Year,
                Track: tag.Track == 0 ? null : tag.Track,
                TrackCount: tag.TrackCount == 0 ? null : tag.TrackCount,
                Disc: tag.Disc == 0 ? null : tag.Disc,
                DiscCount: tag.DiscCount == 0 ? null : tag.DiscCount);
        }

        private static string? _JoinList(string[]? values)
        {
            if (values is null || values.Length == 0)
                return null;

            var filtered = values
                .Where(static v => !string.IsNullOrWhiteSpace(v))
                .Select(static v => v.Trim())
                .ToArray();

            return filtered.Length == 0 ? null : string.Join("; ", filtered);
        }

        private static string? _NullIfWhitespace(string? text)
        {
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }
    }
}
