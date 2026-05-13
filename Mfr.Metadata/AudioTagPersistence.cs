using Mfr.Models;
using TagLib;

namespace Mfr.Metadata
{
    /// <summary>
    /// Loads and saves canonical <see cref="AudioTagOverlay"/> values via TagLibSharp across supported formats.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call <see cref="Apply"/> only when the rename row’s embedded-tag preview differs from its original snapshot;
    /// compare outside this type (for example in <c>CommitExecutor</c>) before calling. <see cref="Apply"/>
    /// opens the file, builds an overlay snapshot from TagLib (<see cref="Read"/> normalization), compares it to the
    /// preview in full, returns without saving when they match, and otherwise writes every modeled property from the
    /// preview onto TagLib before saving.
    /// </para>
    /// <para>
    /// String fields cleared in the overlay are written as empty strings or null TagLib assigns; numerics use
    /// <c>0</c> when the preview clears a value; multiline lists use overlay <c>; </c> join/split conventions.
    /// </para>
    /// </remarks>
    public static class AudioTagPersistence
    {
        private static readonly string[] _ListSeparators = [";"];

        /// <summary>
        /// Reads embedded audio tags into a detached <see cref="AudioTagOverlay"/>.
        /// </summary>
        /// <param name="absolutePath">Fully qualified filesystem path to an existing file.</param>
        /// <returns>A new overlay built from embedded tags.</returns>
        /// <exception cref="ArgumentException"><paramref name="absolutePath"/> is empty, relative, missing, or a directory.</exception>
        /// <exception cref="IOException">TagLib cannot open or read the file.</exception>
        /// <exception cref="CorruptFileException">Thrown by TagLib when the embedded structure is unreadable.</exception>
        /// <exception cref="UnsupportedFormatException">Thrown by TagLib when the format cannot be loaded.</exception>
        public static AudioTagOverlay Read(string absolutePath)
        {
            _ValidateExistingRegularFile(absolutePath);

            using var file = TagLib.File.Create(new TagLib.File.LocalFileAbstraction(absolutePath));
            return _FromTag(file.Tag);
        }

        /// <summary>
        /// Loads the file’s normalized tag overlay via TagLib and, when <paramref name="previewOverlay"/> differs from that overlay, assigns every modeled field from <paramref name="previewOverlay"/> to TagLib tags and saves.
        /// </summary>
        /// <param name="absolutePath">Path to an existing regular file (typically the post-move destination).</param>
        /// <param name="previewOverlay">Desired tag values.</param>
        /// <exception cref="ArgumentException"><paramref name="absolutePath"/> is empty, relative, missing, or a directory.</exception>
        /// <exception cref="IOException">The file cannot be opened or saved.</exception>
        public static void Apply(string absolutePath, AudioTagOverlay previewOverlay)
        {
            _ValidateExistingRegularFile(absolutePath);

            using var file = TagLib.File.Create(new TagLib.File.LocalFileAbstraction(absolutePath));
            var tag = file.Tag;
            var baselineOverlay = _FromTag(tag);
            if (previewOverlay.Equals(baselineOverlay))
                return;

            _WriteOverlayToTag(tag, previewOverlay);

            file.Save();
        }

        private static void _WriteOverlayToTag(Tag tag, AudioTagOverlay overlay)
        {
            tag.Title = _EmptyStringToNull(overlay.Title);
            tag.Album = _EmptyStringToNull(overlay.Album);
            tag.Performers = _SplitJoinedList(overlay.Performers);
            tag.AlbumArtists = _SplitJoinedList(overlay.AlbumArtists);
            tag.Composers = _SplitJoinedList(overlay.Composers);
            tag.Genres = string.IsNullOrWhiteSpace(overlay.Genre)
                ? []
                : [overlay.Genre.Trim()];

            tag.Comment = _EmptyStringToNull(overlay.Comment);
            tag.Lyrics = _EmptyStringToNull(overlay.Lyrics);
            tag.Copyright = _EmptyStringToNull(overlay.Copyright);
            tag.Grouping = _EmptyStringToNull(overlay.Grouping);

            tag.Year = overlay.Year ?? 0;
            tag.Track = overlay.Track ?? 0;
            tag.TrackCount = overlay.TrackCount ?? 0;
            tag.Disc = overlay.Disc ?? 0;
            tag.DiscCount = overlay.DiscCount ?? 0;
        }

        private static string? _EmptyStringToNull(string? text)
        {
            return string.IsNullOrEmpty(text) ? null : text;
        }

        private static void _ValidateExistingRegularFile(string absolutePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(absolutePath);

            if (!Path.IsPathFullyQualified(absolutePath))
                throw new ArgumentException("Path must be fully qualified.", nameof(absolutePath));

            if (Directory.Exists(absolutePath))
                throw new ArgumentException($"'{absolutePath}' is a directory.", nameof(absolutePath));

            if (!System.IO.File.Exists(absolutePath))
                throw new ArgumentException($"File does not exist: '{absolutePath}'.", nameof(absolutePath));
        }

        private static AudioTagOverlay _FromTag(Tag tag)
        {
            return new AudioTagOverlay
            {
                Title = _NullIfEmpty(tag.Title),
                Album = _NullIfEmpty(tag.Album),
                Performers = _JoinList(tag.Performers),
                AlbumArtists = _JoinList(tag.AlbumArtists),
                Composers = _JoinList(tag.Composers),
                Genre = _NullIfEmpty(tag.FirstGenre),
                Comment = _NullIfEmpty(tag.Comment),
                Lyrics = _NullIfEmpty(tag.Lyrics),
                Copyright = _NullIfEmpty(tag.Copyright),
                Grouping = _NullIfEmpty(tag.Grouping),
                Year = tag.Year == 0 ? null : tag.Year,
                Track = tag.Track == 0 ? null : tag.Track,
                TrackCount = tag.TrackCount == 0 ? null : tag.TrackCount,
                Disc = tag.Disc == 0 ? null : tag.Disc,
                DiscCount = tag.DiscCount == 0 ? null : tag.DiscCount,
            };
        }

        private static string[] _SplitJoinedList(string? joined)
        {
            if (string.IsNullOrWhiteSpace(joined))
                return [];

            return [.. joined.Split(_ListSeparators, StringSplitOptions.TrimEntries)
                .Where(part => !string.IsNullOrEmpty(part))
                .Select(part => part.Trim())];
        }

        private static string? _JoinList(string[] values)
        {
            var filtered =
                values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(static v => v.Trim()).ToArray();

            return filtered.Length == 0 ? null : string.Join("; ", filtered);
        }

        private static string? _NullIfEmpty(string? text)
        {
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }
    }
}
