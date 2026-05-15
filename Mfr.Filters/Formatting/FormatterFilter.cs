using Mfr.Models;
using Mfr.Utils;

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
    /// <param name="ApplyScope">When non-null, restricts this filter to a substring or token of the target; see <see cref="StringApplyScope"/>.</param>
    public sealed partial record FormatterFilter(
        FilterTarget Target,
        FormatterOptions Options, StringApplyScope? ApplyScope = null) : StringTargetFilter(Target, ApplyScope)
    {
        private Formatter? _compiledTemplate;

        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "Formatter";

        /// <inheritdoc />
        protected override void _Setup()
        {
            _compiledTemplate = FormatStringCompiler.Compile(Options.Template);
        }

        protected override string _TransformValue(string value, RenameItem item)
        {
            _ = value;
            return Check.NotNull(_compiledTemplate, "FormatterFilter setup must complete before transform.")(item);
        }
    }
}
