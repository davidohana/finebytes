namespace Mfr.Models
{
    /// <summary>
    /// Canonical in-memory overlay for embedded audio tags, shared by preview snapshots and TagLib façade I/O.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>null</c> means the field was not populated or should be left unchanged on save (see façades documentation).
    /// Non-null strings may be empty to represent an intentional clear where the persistence layer supports it.
    /// </para>
    /// <para>
    /// Multi-value frames (performers, album artists, composers) are stored as display strings separated by
    /// <c>; </c>; parsing and joining are handled in <c>Mfr.Metadata</c>.
    /// </para>
    /// <para>
    /// For MPEG/MP3 files, <see cref="Id3v1"/> and <see cref="Id3v2"/> hold per-tag snapshots; semantic properties
    /// mirror TagLib’s merged tag. Other formats leave both <see langword="null"/> until later phases add more containers.
    /// </para>
    /// </remarks>
    public sealed class AudioTagOverlay : IEquatable<AudioTagOverlay?>
    {
        /// <summary>
        /// Gets or sets the optional ID3v1 snapshot when the row is backed by MPEG/MP3 structured tags.
        /// </summary>
        public Id3v1TagData? Id3v1 { get; set; }

        /// <summary>
        /// Gets or sets the optional ID3v2 snapshot (full frame inventory) when the row is backed by MPEG/MP3 structured tags.
        /// </summary>
        public Id3v2TagData? Id3v2 { get; set; }

        /// <summary>
        /// Gets or sets the track title.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the album name.
        /// </summary>
        public string? Album { get; set; }

        /// <summary>
        /// Gets or sets the display string for primary performers (joint <c>; </c> list).
        /// </summary>
        public string? Performers { get; set; }

        /// <summary>
        /// Gets or sets the display string for album artists (joint <c>; </c> list).
        /// </summary>
        public string? AlbumArtists { get; set; }

        /// <summary>
        /// Gets or sets the display string for composers (joint <c>; </c> list).
        /// </summary>
        public string? Composers { get; set; }

        /// <summary>
        /// Gets or sets the genre tag text.
        /// </summary>
        public string? Genre { get; set; }

        /// <summary>
        /// Gets or sets the generic comment/description text.
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// Gets or sets lyrical text when present as a conventional tag property.
        /// </summary>
        public string? Lyrics { get; set; }

        /// <summary>
        /// Gets or sets the copyright notice.
        /// </summary>
        public string? Copyright { get; set; }

        /// <summary>
        /// Gets or sets grouping label metadata when supported by the container.
        /// </summary>
        public string? Grouping { get; set; }

        /// <summary>
        /// Gets or sets calendar year metadata when supplied by the backing file format.
        /// </summary>
        public uint? Year { get; set; }

        /// <summary>
        /// Gets or sets the primary track/disc index when supplied by tags.
        /// </summary>
        public uint? Track { get; set; }

        /// <summary>
        /// Gets or sets track count portion (e.g. <c>n</c> of <c>m</c>).
        /// </summary>
        public uint? TrackCount { get; set; }

        /// <summary>
        /// Gets or sets disc index when supplied by tags.
        /// </summary>
        public uint? Disc { get; set; }

        /// <summary>
        /// Gets or sets total disc count when supplied by tags.
        /// </summary>
        public uint? DiscCount { get; set; }

        /// <summary>
        /// Creates a detached copy suitable for cloning <see cref="FileMeta"/>.
        /// </summary>
        /// <returns>New instance with copied values.</returns>
        public AudioTagOverlay Clone()
        {
            return new AudioTagOverlay
            {
                Id3v1 = Id3v1 is null ? null : Id3v1 with { },
                Id3v2 = Id3v2 is null
                    ? null
                    : new Id3v2TagData
                    {
                        Version = Id3v2.Version,
                        CanonicalTagBytes = Id3v2.CanonicalTagBytes,
                        Frames = Id3v2.Frames,
                    },
                Title = Title,
                Album = Album,
                Performers = Performers,
                AlbumArtists = AlbumArtists,
                Composers = Composers,
                Genre = Genre,
                Comment = Comment,
                Lyrics = Lyrics,
                Copyright = Copyright,
                Grouping = Grouping,
                Year = Year,
                Track = Track,
                TrackCount = TrackCount,
                Disc = Disc,
                DiscCount = DiscCount,
            };
        }

        /// <inheritdoc />
        public bool Equals(AudioTagOverlay? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (!Equals(Id3v1, other.Id3v1))
                return false;

            if (!Equals(Id3v2, other.Id3v2))
                return false;

            return string.Equals(Title, other.Title, StringComparison.Ordinal)
                && string.Equals(Album, other.Album, StringComparison.Ordinal)
                && string.Equals(Performers, other.Performers, StringComparison.Ordinal)
                && string.Equals(AlbumArtists, other.AlbumArtists, StringComparison.Ordinal)
                && string.Equals(Composers, other.Composers, StringComparison.Ordinal)
                && string.Equals(Genre, other.Genre, StringComparison.Ordinal)
                && string.Equals(Comment, other.Comment, StringComparison.Ordinal)
                && string.Equals(Lyrics, other.Lyrics, StringComparison.Ordinal)
                && string.Equals(Copyright, other.Copyright, StringComparison.Ordinal)
                && string.Equals(Grouping, other.Grouping, StringComparison.Ordinal)
                && Year == other.Year
                && Track == other.Track
                && TrackCount == other.TrackCount
                && Disc == other.Disc
                && DiscCount == other.DiscCount;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return Equals(obj as AudioTagOverlay);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Id3v1);
            hashCode.Add(Id3v2);
            hashCode.Add(Title, StringComparer.Ordinal);
            hashCode.Add(Album, StringComparer.Ordinal);
            hashCode.Add(Performers, StringComparer.Ordinal);
            hashCode.Add(AlbumArtists, StringComparer.Ordinal);
            hashCode.Add(Composers, StringComparer.Ordinal);
            hashCode.Add(Genre, StringComparer.Ordinal);
            hashCode.Add(Comment, StringComparer.Ordinal);
            hashCode.Add(Lyrics, StringComparer.Ordinal);
            hashCode.Add(Copyright, StringComparer.Ordinal);
            hashCode.Add(Grouping, StringComparer.Ordinal);
            hashCode.Add(Year);
            hashCode.Add(Track);
            hashCode.Add(TrackCount);
            hashCode.Add(Disc);
            hashCode.Add(DiscCount);
            return hashCode.ToHashCode();
        }
    }
}
