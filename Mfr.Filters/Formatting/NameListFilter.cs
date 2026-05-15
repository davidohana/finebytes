using Mfr.Models;
using Mfr.Utils;

namespace Mfr.Filters.Formatting
{
    /// <summary>
    /// Options for loading and applying a name list from a text file.
    /// </summary>
    /// <param name="FilePath">Path to the name-list file (one name per line).</param>
    /// <param name="Prefix">Optional format string prepended to each list entry; supports formatter tokens (for example <c>&lt;counter:...&gt;</c>).</param>
    /// <param name="Suffix">Optional format string appended after each list entry; supports formatter tokens.</param>
    public sealed record NameListOptions(
        string FilePath,
        string Prefix = "",
        string Suffix = "");

    /// <summary>
    /// Replaces the target field with the name-list line matching the item's list position, with optional prefix and suffix templates.
    /// </summary>
    /// <remarks>
    /// Entry index <c>k</c> in the loaded list applies to the rename item whose <see cref="FileMeta.RenameListIndex"/> is <c>k</c> (zero-based).
    /// This matches a column exported via Export Name List edited in-place.
    /// </remarks>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Name list and optional prefix/suffix templates.</param>
    /// <param name="ApplyScope">When non-null, restricts this filter to a substring or token of the target; see <see cref="StringApplyScope"/>.</param>
    public sealed record NameListFilter(
        FilterTarget Target,
        NameListOptions Options, StringApplyScope? ApplyScope = null) : StringTargetFilter(Target, ApplyScope)
    {
        private IReadOnlyList<string>? _entries;
        private Formatter? _compiledPrefix;
        private Formatter? _compiledSuffix;

        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "NameList";

        /// <summary>
        /// Loads and validates the name-list file for this filter instance, and compiles prefix/suffix templates.
        /// </summary>
        protected override void _Setup()
        {
            _entries = NameListParser.ParseFile(filePath: Options.FilePath);
            _compiledPrefix = FormatStringCompiler.Compile(Options.Prefix);
            _compiledSuffix = FormatStringCompiler.Compile(Options.Suffix);
        }

        /// <summary>
        /// Replaces the segment with the list entry for this item, wrapped by resolved prefix and suffix templates.
        /// </summary>
        /// <param name="value">Current field text (ignored; the result is fully determined by the list and templates).</param>
        /// <param name="item">Rename item providing list index and token context.</param>
        /// <returns>The new field value.</returns>
        protected override string _TransformValue(string value, RenameItem item)
        {
            _ = value;
            var entries = Check.NotNull(_entries, "Name-list setup must complete before transform.");
            var compiledPrefix = Check.NotNull(_compiledPrefix, "Name-list setup must complete before transform.");
            var compiledSuffix = Check.NotNull(_compiledSuffix, "Name-list setup must complete before transform.");

            var index = item.Original.RenameListIndex;
            if (index < 0 || index >= entries.Count)
            {
                throw new UserException(
                    $"Name-list has {entries.Count} line(s) but rename item index is {index} (expected 0..{entries.Count - 1}). Add lines or adjust the rename list.");
            }

            var middle = entries[index];
            return compiledPrefix(item) + middle + compiledSuffix(item);
        }
    }
}
