namespace Mfr.Models.Tags.Id3v2
{
    /// <summary>
    /// User-visible labels for modeled ID3v2 frame ids (Apply-To and field-setter combos).
    /// </summary>
    public static class Id3v2FrameLabels
    {
        /// <summary>
        /// Returns <c>FRAMEID (Short name)</c> for a modeled frame, or the trimmed uppercase id when unknown.
        /// </summary>
        /// <param name="frameId">Four-character frame id (any casing).</param>
        /// <returns>Friendly label for pickers and Applied-list subtitles.</returns>
        public static string For(string frameId)
        {
            if (string.IsNullOrWhiteSpace(frameId))
            {
                return string.Empty;
            }

            var normalized = frameId.Trim().ToUpperInvariant();
            if (!Id3v2ModeledFrame.FrameIdToShortName.TryGetValue(normalized, out var shortName))
            {
                return normalized;
            }

            return $"{normalized} ({shortName})";
        }
    }
}
