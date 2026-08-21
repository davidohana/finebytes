namespace Mfr.Models.Tags.Id3v2
{
    /// <summary>
    /// Guards ID3v2 frame writes against silent version upgrades (v2.4-only frames into a v2.3 tag).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Create paths use ID3v2.3. Patch paths preserve the on-disk version. Setting a frame that exists only in
    /// ID3v2.4 on a lower-version tag throws <see cref="NotSupportedException"/> (PreviewError); the tag is never
    /// upgraded in place.
    /// </para>
    /// </remarks>
    public static class Id3v2FrameVersionPolicy
    {
        private static readonly HashSet<string> _Id3v24OnlyFrameIds = new(StringComparer.Ordinal)
        {
            "TDEN",
            "TDOR",
            "TDRC",
            "TDRL",
            "TDTG",
            "TIPL",
            "TMCL",
            "TMOO",
            "TPRO",
            "TSOA",
            "TSOP",
            "TSOT",
            "TSST",
        };

        /// <summary>
        /// Whether <paramref name="frameId"/> is defined only for ID3v2.4 (and later).
        /// </summary>
        /// <param name="frameId">Four-character frame id (case-insensitive).</param>
        /// <returns><see langword="true"/> when the frame requires tag version ≥ 4.</returns>
        public static bool RequiresId3v24(string frameId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(frameId);
            return _Id3v24OnlyFrameIds.Contains(frameId.Trim().ToUpperInvariant());
        }

        /// <summary>
        /// Throws when writing <paramref name="frameId"/> into a tag whose <paramref name="tagVersion"/> is below 4
        /// and the frame is v2.4-only.
        /// </summary>
        /// <param name="tagVersion">Current ID3v2 minor version on the overlay block (3 = v2.3, 4 = v2.4).</param>
        /// <param name="frameId">Four-character frame id to write (case-insensitive).</param>
        /// <exception cref="NotSupportedException">The frame requires ID3v2.4 and the tag is an older version.</exception>
        public static void EnsureCompatible(byte tagVersion, string frameId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(frameId);

            if (tagVersion >= 4)
            {
                return;
            }

            var normalizedId = frameId.Trim().ToUpperInvariant();
            if (!_Id3v24OnlyFrameIds.Contains(normalizedId))
            {
                return;
            }

            throw new NotSupportedException(
                $"ID3v2 frame '{normalizedId}' requires version 2.4; this tag is version 2.{tagVersion} (no silent upgrade)."
            );
        }
    }
}
