using Mfr.Filters.Case;

namespace Mfr.App.Ui.ViewModels.FilterEditors
{
    /// <summary>
    /// One casing-mode choice for <see cref="LettersCaseFilterEditorViewModel"/>.
    /// </summary>
    /// <param name="Label">Combo display text.</param>
    /// <param name="Mode">Letters-case mode written to the filter.</param>
    internal sealed record LettersCaseModeOption(string Label, LettersCaseMode Mode)
    {
        /// <summary>
        /// Gets the casing modes shown in Filter Configuration.
        /// </summary>
        public static IReadOnlyList<LettersCaseModeOption> All { get; } =
        [
            new("Capitalize", LettersCaseMode.TitleCase),
            new("Sentence case", LettersCaseMode.SentenceCase),
            new("tOGGLE cASE", LettersCaseMode.InvertCase),
            new("First letter up", LettersCaseMode.FirstLetterUp),
            new("wEiRd CaSe", LettersCaseMode.WeirdCase),
            new("UPPER CASE", LettersCaseMode.UpperCase),
            new("lower case", LettersCaseMode.LowerCase),
        ];

        /// <summary>
        /// Maps a filter mode to the matching combo entry.
        /// </summary>
        /// <param name="mode">Current filter mode.</param>
        /// <returns>The list entry for <paramref name="mode"/>.</returns>
        public static LettersCaseModeOption FromMode(LettersCaseMode mode)
        {
            return All.First(option => option.Mode == mode);
        }
    }
}
