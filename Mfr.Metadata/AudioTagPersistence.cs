using Mfr.Models;
using TagLib;

namespace Mfr.Metadata
{
    /// <summary>
    /// Loads and saves canonical <see cref="AudioTagOverlay"/> values via TagLibSharp across supported formats.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Persisted writes go through <see cref="ApplyIfChanged"/> and merge only differing fields versus the baseline snapshot.
    /// </para>
    /// <para>
    /// String fields cleared in the overlay are written as empty strings to TagLib collections; numerics cleared with
    /// <c>0</c> when the preview value is <c>null</c> and no longer matches the baseline.
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
        /// Persists differing fields between overlays onto <paramref name="absolutePath"/>.
        /// </summary>
        /// <param name="absolutePath">Destination file path.</param>
        /// <param name="previewOverlay">Desired tag values.</param>
        /// <param name="baselineOverlay">Previously established snapshot to diff against.</param>
        /// <exception cref="ArgumentException"><paramref name="absolutePath"/> is empty, relative, missing, or a directory.</exception>
        /// <exception cref="IOException">The file cannot be opened or saved.</exception>
        public static void ApplyIfChanged(
            string absolutePath,
            AudioTagOverlay previewOverlay,
            AudioTagOverlay baselineOverlay)
        {
            if (previewOverlay.Equals(baselineOverlay))
                return;

            _ValidateWritableFilePath(absolutePath);

            using var file = TagLib.File.Create(new TagLib.File.LocalFileAbstraction(absolutePath));
            var tag = file.Tag;

            _ApplyStringDifference(previewOverlay.Title, baselineOverlay.Title, v => tag.Title = v);

            _ApplyStringDifference(previewOverlay.Album, baselineOverlay.Album, v => tag.Album = v);

            _ApplyStringDifference(previewOverlay.Comment, baselineOverlay.Comment, v => tag.Comment = v);

            _ApplyMultivalueDifference(
                previewOverlay.Performers,
                baselineOverlay.Performers,
                v => tag.Performers = v);

            _ApplyMultivalueDifference(
                previewOverlay.AlbumArtists,
                baselineOverlay.AlbumArtists,
                v => tag.AlbumArtists = v);

            _ApplyMultivalueDifference(
                previewOverlay.Composers,
                baselineOverlay.Composers,
                v => tag.Composers = v);

            _ApplyGenreDifference(previewOverlay.Genre, baselineOverlay.Genre, tag);

            _ApplyStringDifference(previewOverlay.Lyrics, baselineOverlay.Lyrics, v => tag.Lyrics = v);

            _ApplyStringDifference(previewOverlay.Copyright, baselineOverlay.Copyright, v => tag.Copyright = v);

            _ApplyStringDifference(previewOverlay.Grouping, baselineOverlay.Grouping, v => tag.Grouping = v);

            _ApplyUIntDifference(previewOverlay.Year, baselineOverlay.Year, v => tag.Year = v);

            _ApplyUIntDifference(previewOverlay.Track, baselineOverlay.Track, v => tag.Track = v);
            _ApplyUIntDifference(previewOverlay.TrackCount, baselineOverlay.TrackCount, v => tag.TrackCount = v);

            _ApplyUIntDifference(previewOverlay.Disc, baselineOverlay.Disc, v => tag.Disc = v);
            _ApplyUIntDifference(previewOverlay.DiscCount, baselineOverlay.DiscCount, v => tag.DiscCount = v);

            file.Save();
        }

        private static void _ValidateWritableFilePath(string absolutePath)
        {
            _ValidateExistingRegularFile(absolutePath);
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

        private static void _ApplyStringDifference(string? preview, string? baseline, Action<string?> assignOnTagLib)
        {
            if (string.Equals(preview, baseline, StringComparison.Ordinal))
                return;

            assignOnTagLib(string.IsNullOrEmpty(preview) ? null : preview);
        }

        private static void _ApplyMultivalueDifference(
            string? previewJoined,
            string? baselineJoined,
            Action<string[]> assign)
        {
            if (string.Equals(previewJoined, baselineJoined, StringComparison.Ordinal))
                return;

            assign(_SplitJoinedList(previewJoined));
        }

        private static void _ApplyGenreDifference(string? previewGenre, string? baselineGenre, Tag tag)
        {
            if (string.Equals(previewGenre, baselineGenre, StringComparison.Ordinal))
                return;

            tag.Genres = string.IsNullOrWhiteSpace(previewGenre) ? [] : [previewGenre.Trim()];
        }

        private static void _ApplyUIntDifference(uint? previewNumeric, uint? baselineNumeric, Action<uint> assign)
        {
            if (previewNumeric == baselineNumeric)
                return;

            assign(previewNumeric ?? 0);
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
