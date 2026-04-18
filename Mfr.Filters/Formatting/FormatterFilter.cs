using Mfr.Models;

namespace Mfr.Filters.Formatting
{
    /// <summary>
    /// Options for formatter templates.
    /// </summary>
    /// <param name="Template">Template expression with formatter tokens.</param>
    public sealed record FormatterOptions(string Template);

    /// <summary>
    /// Applies formatter template tokens.
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Formatter options.</param>
    public sealed partial record FormatterFilter(
        FilterTarget Target,
        FormatterOptions Options) : FileNameSegmentFilter(Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "Formatter";

        protected override string _TransformSegment(string segment, RenameItem item)
        {
            return FormatterTokenResolver.ResolveTemplate(Options.Template, item);
        }
    }
}
