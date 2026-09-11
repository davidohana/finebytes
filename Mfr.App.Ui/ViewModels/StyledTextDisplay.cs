namespace Mfr.App.Ui.ViewModels
{
    /// <summary>
    /// Rich text as one or more styled runs (status bar, dialogs, and similar).
    /// </summary>
    public sealed class StyledTextDisplay
    {
        private static readonly StyledTextRun[] EmptyRuns = [];

        /// <summary>
        /// Gets empty content.
        /// </summary>
        public static StyledTextDisplay Empty { get; } = new(EmptyRuns);

        /// <summary>
        /// Initializes content from styled runs.
        /// </summary>
        /// <param name="runs">Ordered segments to render.</param>
        public StyledTextDisplay(IReadOnlyList<StyledTextRun> runs)
        {
            ArgumentNullException.ThrowIfNull(runs);
            Runs = runs;
        }

        /// <summary>
        /// Gets the styled segments to render left-to-right.
        /// </summary>
        public IReadOnlyList<StyledTextRun> Runs { get; }

        /// <summary>
        /// Gets whether there is no visible content.
        /// </summary>
        public bool IsEmpty => Runs.Count == 0;

        /// <summary>
        /// Builds single-run content with default styling.
        /// </summary>
        /// <param name="text">Message to show.</param>
        /// <returns>Plain styled text.</returns>
        public static StyledTextDisplay FromPlain(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return Empty;
            }

            return new StyledTextDisplay([new StyledTextRun(text)]);
        }

        /// <summary>
        /// Builds content from explicit runs.
        /// </summary>
        /// <param name="runs">Ordered segments to render.</param>
        /// <returns>Rich styled text.</returns>
        public static StyledTextDisplay FromRuns(params StyledTextRun[] runs)
        {
            if (runs.Length == 0)
            {
                return Empty;
            }

            return new StyledTextDisplay(runs);
        }

        /// <summary>
        /// Concatenates all run text (accessibility, tests, logging).
        /// </summary>
        /// <returns>Plain text without styling.</returns>
        public string ToPlainText()
        {
            if (IsEmpty)
            {
                return string.Empty;
            }

            return string.Concat(Runs.Select(run => run.Text));
        }
    }
}
