using Mfr.Models;
using Mfr.Models.Tags;

namespace Mfr.Filters
{
    /// <summary>
    /// Loads and validates embedded tags before a string-target filter reads or writes an audio <see cref="FilterTarget"/>.
    /// </summary>
    internal static class RenameItemTargetStringExtensions
    {
        /// <summary>
        /// Ensures disk tags are loaded and the container supports <paramref name="target"/> when it addresses audio overlay fields.
        /// </summary>
        /// <param name="item">Rename row whose preview will be read or written.</param>
        /// <param name="target">Path or audio filter target.</param>
        /// <exception cref="NotSupportedException">Thrown when an audio target is unsupported for the row's container.</exception>
        internal static void EnsureTargetReady(this RenameItem item, FilterTarget target)
        {
            ArgumentNullException.ThrowIfNull(item);
            ArgumentNullException.ThrowIfNull(target);

            if (!AudioOverlayTargetIo.IsAudioTarget(target))
                return;

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
