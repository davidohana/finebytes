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
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Formatter options.</param>
    public sealed partial record FormatterFilter(
        bool Enabled,
        FilterTarget Target,
        FormatterOptions Options) : BaseFilter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "Formatter";

        internal override string TransformSegment(string segment, RenameItem item, FilterChainContext context)
        {
            return FormatterTokenResolver.ResolveTemplate(Options.Template, item);
        }
    }
}
