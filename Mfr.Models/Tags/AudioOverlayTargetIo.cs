using Mfr.Models.Filters;

namespace Mfr.Models.Tags
{
    /// <summary>
    /// Reads and writes string-valued <see cref="IAudioOverlayFilterTarget"/> rows on <see cref="AudioTagOverlay"/>.
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
        /// Returns the filter/preview string for an audio <paramref name="target"/> on <paramref name="overlay"/>.
        /// </summary>
        /// <param name="overlay">Structured tag blocks.</param>
        /// <param name="target">Audio field or frame target.</param>
        /// <returns>Current string value (empty when unset).</returns>
        /// <exception cref="NotSupportedException">Thrown when <paramref name="target"/> is an unrecognized audio overlay target type.</exception>
        public static string GetTargetString(AudioTagOverlay overlay, IAudioOverlayFilterTarget target)
        {
            ArgumentNullException.ThrowIfNull(overlay);
            ArgumentNullException.ThrowIfNull(target);

            return target switch
            {
                SemanticAudioFieldTarget audioFieldTarget =>
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
        /// <exception cref="NotSupportedException">Thrown when <paramref name="target"/> is an unrecognized audio overlay target type.</exception>
        public static void SetTargetString(AudioTagOverlay overlay, IAudioOverlayFilterTarget target, string value)
        {
            ArgumentNullException.ThrowIfNull(overlay);
            ArgumentNullException.ThrowIfNull(target);

            switch (target)
            {
                case SemanticAudioFieldTarget audioFieldTarget:
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
