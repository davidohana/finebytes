using Mfr.Models;
using Mfr.Models.Tags;

namespace Mfr.Filters
{
    /// <summary>
    /// Unified string get/set for <see cref="FilterTarget"/> on a rename row (path slices and audio overlay fields).
    /// </summary>
    internal static class RenameItemTargetStringExtensions
    {
        /// <summary>
        /// Returns the preview string addressed by <paramref name="target"/>, loading tags when needed for audio targets.
        /// </summary>
        /// <param name="item">Rename row whose preview is read.</param>
        /// <param name="target">Path or audio filter target.</param>
        /// <returns>Current preview string for the target.</returns>
        /// <exception cref="NotSupportedException">Thrown when no handler exists for <paramref name="target"/>.</exception>
        internal static string GetTargetString(this RenameItem item, FilterTarget target)
        {
            ArgumentNullException.ThrowIfNull(item);
            ArgumentNullException.ThrowIfNull(target);

            if (AudioOverlayTargetIo.IsAudioTarget(target))
                _EnsureReadyForAudioTarget(item, target);

            return item.Preview.GetTargetString(target);
        }

        /// <summary>
        /// Writes <paramref name="value"/> onto the preview field addressed by <paramref name="target"/>.
        /// </summary>
        /// <param name="item">Rename row whose preview is mutated.</param>
        /// <param name="target">Path or audio filter target.</param>
        /// <param name="value">Transformed string to store.</param>
        /// <exception cref="NotSupportedException">Thrown when no handler exists for <paramref name="target"/>.</exception>
        internal static void SetTargetString(this RenameItem item, FilterTarget target, string value)
        {
            ArgumentNullException.ThrowIfNull(item);
            ArgumentNullException.ThrowIfNull(target);

            if (AudioOverlayTargetIo.IsAudioTarget(target))
                _EnsureReadyForAudioTarget(item, target);

            item.Preview.SetTargetString(target, value);
        }

        private static void _EnsureReadyForAudioTarget(RenameItem item, FilterTarget target)
        {
            switch (target)
            {
                case AudioFieldTarget:
                    item.EnsureEmbeddedTagsLoaded();
                    return;
                case Id3v1FieldTarget:
                    item.EnsureAudioTagBlockSupported(AudioTagBlockKind.Id3v1);
                    return;
                case Id3v2FrameTarget:
                    item.EnsureAudioTagBlockSupported(AudioTagBlockKind.Id3v2);
                    return;
                case XiphFieldTarget:
                    item.EnsureAudioTagBlockSupported(AudioTagBlockKind.Xiph);
                    return;
                default:
                    throw new NotSupportedException(
                        $"Unsupported audio filter target '{target.GetType().Name}'.");
            }
        }
    }
}
