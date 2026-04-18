using Mfr.Models;

namespace Mfr.Filters.Case
{
    /// <summary>
    /// Options for <see cref="SentenceEndCharactersFilter"/>.
    /// </summary>
    /// <param name="Characters">
    /// Characters that mark sentence endings for filters that consult <see cref="RenameItem.SentenceEndChars"/>
    /// (for example <see cref="LettersCaseFilter"/> in <see cref="LettersCaseMode.SentenceCase"/> and
    /// <see cref="CasingListFilter"/> when <c>UppercaseSentenceInitial</c> is enabled).
    /// When empty, sentence-style rules only capitalize at the start of the segment.
    /// </param>
    public sealed record SentenceEndCharactersOptions(string Characters = ".!?");

    /// <summary>
    /// Defines which characters separate sentences for later filters in the chain. Does not change the target text.
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Sentence-end character list.</param>
    public sealed record SentenceEndCharactersFilter(
        FilterTarget Target,
        SentenceEndCharactersOptions Options) : FileNameSegmentFilter(Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "SentenceEndCharacters";

        /// <summary>
        /// Updates <see cref="RenameItem.SentenceEndChars"/> and returns the segment unchanged.
        /// </summary>
        /// <param name="segment">Input text segment (unchanged).</param>
        /// <param name="item">Rename item whose sentence-end settings are updated.</param>
        /// <returns>The same <paramref name="segment"/> value.</returns>
        protected override string _TransformSegment(string segment, RenameItem item)
        {
            item.SentenceEndChars = Options.Characters;
            return segment;
        }
    }
}
