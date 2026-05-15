using System.Text.Json.Serialization;
using Mfr.Metadata;
using Mfr.Models.Tags;
// Namespace stays Mfr.Models for JSON preset compatibility; this file lives under Mfr.Filters so string-target filters
// can call Mfr.Metadata without a Models↔Metadata cycle.
#pragma warning disable IDE0130

namespace Mfr.Models
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

            if (Target is AudioOverlayFieldTarget audioOverlayTarget)
            {
                item.EnsureAudioTagsLoaded();
                var current = AudioOverlaySemanticIo.GetInvariantFieldString(preview.AudioTagOverlay, audioOverlayTarget.Field);
                var transformed = TransformValue(current, item);
                AudioOverlaySemanticIo.MergeInvariantStringIntoOverlay(
                    overlay: preview.AudioTagOverlay,
                    field: audioOverlayTarget.Field,
                    invariantString: transformed,
                    embeddedTagSourcePath: item.Original.FullPath);

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
