namespace Mfr.Models.Tags
{
    /// <summary>
    /// Reads and writes string-valued <see cref="FilterTarget"/> rows that address <see cref="AudioTagOverlay"/> fields.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Path and file-name targets are not handled here; callers must route those through
    /// <c>FileMetaPreviewExtensions</c>. Capability checks belong to the filter layer before write.
    /// </para>
    /// </remarks>
    public static class AudioOverlayTargetIo
    {
        /// <summary>
        /// Returns whether <paramref name="target"/> addresses an audio overlay field or frame.
        /// </summary>
        /// <param name="target">Filter target to classify.</param>
        /// <returns><see langword="true"/> for semantic and format-specific audio targets.</returns>
        public static bool IsAudioTarget(FilterTarget target)
        {
            ArgumentNullException.ThrowIfNull(target);

            return target is AudioFieldTarget
                or Id3v1FieldTarget
                or Id3v2FrameTarget
                or XiphFieldTarget;
        }

        /// <summary>
        /// Returns the filter/preview string for an audio <paramref name="target"/> on <paramref name="overlay"/>.
        /// </summary>
        /// <param name="overlay">Structured tag blocks.</param>
        /// <param name="target">Audio field or frame target.</param>
        /// <returns>Current string value (empty when unset).</returns>
        /// <exception cref="NotSupportedException">Thrown when <paramref name="target"/> is not an audio overlay target.</exception>
        public static string GetTargetString(AudioTagOverlay overlay, FilterTarget target)
        {
            ArgumentNullException.ThrowIfNull(overlay);
            ArgumentNullException.ThrowIfNull(target);

            return target switch
            {
                AudioFieldTarget audioFieldTarget =>
                    SemanticFields.GetSemanticField(overlay, audioFieldTarget.Field),
                Id3v1FieldTarget id3v1FieldTarget =>
                    AudioOverlayBlockFieldIo.GetId3v1FieldString(overlay, id3v1FieldTarget.Field),
                Id3v2FrameTarget id3v2FrameTarget =>
                    AudioOverlayBlockFieldIo.GetId3v2FrameString(
                        overlay,
                        id3v2FrameTarget.FrameId,
                        id3v2FrameTarget.Language,
                        id3v2FrameTarget.Description),
                XiphFieldTarget xiphFieldTarget =>
                    AudioOverlayBlockFieldIo.GetXiphFieldString(overlay, xiphFieldTarget.Key),
                _ => throw new NotSupportedException(
                    $"Unsupported audio filter target '{target.GetType().Name}'."),
            };
        }

        /// <summary>
        /// Writes <paramref name="value"/> for an audio <paramref name="target"/> onto <paramref name="overlay"/>.
        /// </summary>
        /// <param name="overlay">Overlay whose blocks are updated.</param>
        /// <param name="target">Audio field or frame target.</param>
        /// <param name="value">Transformed string to store.</param>
        /// <exception cref="NotSupportedException">Thrown when <paramref name="target"/> is not an audio overlay target.</exception>
        public static void SetTargetString(AudioTagOverlay overlay, FilterTarget target, string value)
        {
            ArgumentNullException.ThrowIfNull(overlay);
            ArgumentNullException.ThrowIfNull(target);

            switch (target)
            {
                case AudioFieldTarget audioFieldTarget:
                    SemanticFields.SetSemanticField(
                        overlay: overlay,
                        field: audioFieldTarget.Field,
                        fieldString: value);
                    return;
                case Id3v1FieldTarget id3v1FieldTarget:
                    AudioOverlayBlockFieldIo.SetId3v1FieldString(overlay, id3v1FieldTarget.Field, value);
                    return;
                case Id3v2FrameTarget id3v2FrameTarget:
                    AudioOverlayBlockFieldIo.SetId3v2FrameString(
                        overlay,
                        id3v2FrameTarget.FrameId,
                        value,
                        id3v2FrameTarget.Language,
                        id3v2FrameTarget.Description);
                    return;
                case XiphFieldTarget xiphFieldTarget:
                    AudioOverlayBlockFieldIo.SetXiphFieldString(overlay, xiphFieldTarget.Key, value);
                    return;
                default:
                    throw new NotSupportedException(
                        $"Unsupported audio filter target '{target.GetType().Name}'.");
            }
        }
    }
}
