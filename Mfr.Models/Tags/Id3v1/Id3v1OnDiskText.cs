using System.Text;
using Mfr.Utils;

namespace Mfr.Models.Tags.Id3v1
{
    /// <summary>
    /// Truncates ID3v1 text the same way TagLib writes the 128-byte trailer (Latin-1, fixed byte fields).
    /// </summary>
    /// <remarks>
    /// <para>
    /// TagLib <c>Id3v1.Tag.Render</c> encodes with Latin-1 and resizes Title/Artist/Album to 30 bytes and
    /// Comment to 28 bytes (ID3v1.1 layout). Preview and overlay writes use these limits so ID3v1 columns
    /// match what will land on disk after Apply.
    /// </para>
    /// </remarks>
    public static class Id3v1OnDiskText
    {
        /// <summary>Max Latin-1 bytes for Title, Artist, and Album.</summary>
        public const int TitleArtistAlbumMaxBytes = 30;

        /// <summary>Max Latin-1 bytes for Comment (TagLib always writes the ID3v1.1 28-byte comment field).</summary>
        public const int CommentMaxBytes = 28;

        private static readonly Encoding s_Latin1 = Encoding.GetEncoding(
            28591,
            EncoderFallback.ReplacementFallback,
            DecoderFallback.ReplacementFallback
        );

        /// <summary>
        /// Returns <paramref name="text"/> encoded as Latin-1 and clipped to <paramref name="maxBytes"/>.
        /// </summary>
        /// <param name="text">Input text (may be longer than the on-disk field).</param>
        /// <param name="maxBytes">Maximum Latin-1 byte length (30 or 28).</param>
        /// <returns>
        /// Truncated string (empty when <paramref name="text"/> is null/empty); non-Latin-1 characters
        /// become replacement characters before clipping.
        /// </returns>
        public static string Truncate(string? text, int maxBytes)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBytes);

            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var bytes = s_Latin1.GetBytes(text);
            if (bytes.Length > maxBytes)
            {
                return s_Latin1.GetString(bytes, 0, maxBytes);
            }

            return s_Latin1.GetString(bytes);
        }

        /// <summary>
        /// Truncates a Title/Artist/Album value to the on-disk field size, or <see langword="null"/> when empty.
        /// </summary>
        /// <param name="text">Input text.</param>
        /// <returns>Truncated text, or <see langword="null"/> when blank after truncation.</returns>
        public static string? TruncateTitleArtistAlbumOrNull(string? text)
        {
            return Truncate(text, TitleArtistAlbumMaxBytes).TrimmedOrNull();
        }

        /// <summary>
        /// Truncates a Comment value to the on-disk field size, or <see langword="null"/> when empty.
        /// </summary>
        /// <param name="text">Input text.</param>
        /// <returns>Truncated text, or <see langword="null"/> when blank after truncation.</returns>
        public static string? TruncateCommentOrNull(string? text)
        {
            return Truncate(text, CommentMaxBytes).TrimmedOrNull();
        }
    }
}
