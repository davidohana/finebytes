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
    /// Defines which characters separate sentences for later filters in the chain. Does not change any target text.
    /// <para>
    /// State-only (like MFR7): no <see cref="FilterTarget"/> or <see cref="StringApplyScope"/> — always updates
    /// <see cref="RenameItem.SentenceEndChars"/> when the filter runs.
    /// </para>
    /// </summary>
    /// <param name="Options">Sentence-end character list.</param>
    [FilterPalette(FilterGroup.Case, "Sentence End Characters")]
    public sealed record SentenceEndCharactersFilter(SentenceEndCharactersOptions Options) : BaseFilter
    {
        /// <summary>
        /// Creates a filter with MFR7 add-to-list defaults (MFR7 sentence-end character set).
        /// </summary>
        public SentenceEndCharactersFilter()
            : this(new SentenceEndCharactersOptions(Characters: "-.!")) { }

        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "SentenceEndCharacters";

        /// <inheritdoc />
        protected internal override void ApplyCore(RenameItem item)
        {
            item.SentenceEndChars = Options.Characters;
        }
    }
}
