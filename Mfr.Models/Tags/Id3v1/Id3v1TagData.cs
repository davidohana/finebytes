namespace Mfr.Models.Tags.Id3v1
{
    /// <summary>
    /// Detached snapshot of an ID3v1 tag (fixed 128-byte style fields) for MP3 overlay persistence.
    /// </summary>
    public sealed record Id3v1TagData
    {
        /// <summary>
        /// Track title (up to 30 characters in the on-disk encoding).
        /// </summary>
        public string? Title { get; init; }

        /// <summary>
        /// Artist string (up to 30 characters in the on-disk encoding).
        /// </summary>
        public string? Artist { get; init; }

        /// <summary>
        /// Album string (up to 30 characters in the on-disk encoding).
        /// </summary>
        public string? Album { get; init; }

        /// <summary>
        /// Four-digit year when present.
        /// </summary>
        public uint? Year { get; init; }

        /// <summary>
        /// Free-form comment string (28 or 30 characters depending on v1.0 vs v1.1 layouts).
        /// </summary>
        public string? Comment { get; init; }

        /// <summary>
        /// Track number when ID3v1.1 layout is used; otherwise <see langword="null"/>.
        /// </summary>
        public byte? Track { get; init; }

        /// <summary>
        /// WinAmp genre table index (0–255), or <see langword="null"/> when unset.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Index <c>0</c> is Blues; do not use <c>0</c> as an empty sentinel (that used to make “clear genre”
        /// read back as Blues).
        /// </para>
        /// </remarks>
        public byte? Genre { get; init; }
    }
}
