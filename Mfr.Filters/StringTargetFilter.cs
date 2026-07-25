using System.Text.Json.Serialization;
using Mfr.Metadata;
using Mfr.Models;
using Mfr.Models.Tags;

namespace Mfr.Filters
{
    /// <summary>
    /// Filter that transforms one string-valued preview field identified by <see cref="FilterTarget"/>.
    /// </summary>
    /// <param name="Target">Polymorphic target (for example <see cref="FilePrefixTarget"/>).</param>
    /// <param name="ApplyScope">When non-null, only that substring or token is transformed; result is spliced back into the full target.</param>
    public abstract record StringTargetFilter(
        FilterTarget Target,
        [property: JsonPropertyName("applyScope")] StringApplyScope? ApplyScope = null) : BaseFilter
    {
        /// <inheritdoc />
        protected internal sealed override void ApplyCore(RenameItem item)
        {
            var preview = item.Preview;

            if (Target is AudioFieldTarget audioFieldTarget)
            {
                item.EnsureEmbeddedTagsLoaded();
                var currentValue = SemanticAudioFieldIo.GetSemanticField(preview.AudioTagOverlay, audioFieldTarget.Field);
                var transformed = TransformValue(currentValue, item);
                SemanticAudioFieldIo.SetSemanticField(
                    overlay: preview.AudioTagOverlay,
                    field: audioFieldTarget.Field,
                    fieldString: transformed);

                return;
            }

            if (Target is Id3v1FieldTarget id3v1FieldTarget)
            {
                item.EnsureAudioTagBlockSupported(AudioTagBlockKind.Id3v1);
                var currentValue = AudioOverlayBlockFieldIo.GetId3v1FieldString(
                    preview.AudioTagOverlay,
                    id3v1FieldTarget.Field);
                var transformed = TransformValue(currentValue, item);
                AudioOverlayBlockFieldIo.SetId3v1FieldString(
                    preview.AudioTagOverlay,
                    id3v1FieldTarget.Field,
                    transformed);
                return;
            }

            if (Target is Id3v2FrameTarget id3v2FrameTarget)
            {
                item.EnsureAudioTagBlockSupported(AudioTagBlockKind.Id3v2);
                var currentValue = AudioOverlayBlockFieldIo.GetId3v2FrameString(
                    preview.AudioTagOverlay,
                    id3v2FrameTarget.FrameId,
                    id3v2FrameTarget.Language,
                    id3v2FrameTarget.Description);
                var transformed = TransformValue(currentValue, item);
                AudioOverlayBlockFieldIo.SetId3v2FrameString(
                    preview.AudioTagOverlay,
                    id3v2FrameTarget.FrameId,
                    transformed,
                    id3v2FrameTarget.Language,
                    id3v2FrameTarget.Description);
                return;
            }

            if (Target is XiphFieldTarget xiphFieldTarget)
            {
                item.EnsureAudioTagBlockSupported(AudioTagBlockKind.Xiph);
                var currentValue = AudioOverlayBlockFieldIo.GetXiphFieldString(
                    preview.AudioTagOverlay,
                    xiphFieldTarget.Key);
                var transformed = TransformValue(currentValue, item);
                AudioOverlayBlockFieldIo.SetXiphFieldString(
                    preview.AudioTagOverlay,
                    xiphFieldTarget.Key,
                    transformed);
                return;
            }

            var previewCurrent = preview.GetTargetString(Target);
            var transformedValue = TransformValue(previewCurrent, item);
            preview.SetTargetString(Target, transformedValue);
        }

        internal string TransformValue(string value, RenameItem item)
        {
            VerifySetupComplete();
            return StringApplyScopeTransform.Apply(ApplyScope, value, item, _TransformValue);
        }

        /// <summary>
        /// Transforms one string after <see cref="BaseFilter.Setup"/> has completed.
        /// </summary>
        /// <param name="value">The current preview string for this filter's target.</param>
        /// <param name="item">The item being renamed.</param>
        /// <returns>The transformed string to write back to the preview field.</returns>
        protected abstract string _TransformValue(string value, RenameItem item);
    }
}
