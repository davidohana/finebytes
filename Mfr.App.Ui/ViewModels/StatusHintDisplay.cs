namespace Mfr.App.Ui.ViewModels
{
    /// <summary>
    /// Status-bar hint content as one or more styled text runs.
    /// </summary>
    public sealed class StatusHintDisplay
    {
        private static readonly StatusHintRun[] EmptyRuns = [];

        /// <summary>
        /// Gets an empty hint.
        /// </summary>
        public static StatusHintDisplay Empty { get; } = new(EmptyRuns);

        /// <summary>
        /// Initializes a hint from styled runs.
        /// </summary>
        /// <param name="runs">Ordered segments to render.</param>
        public StatusHintDisplay(IReadOnlyList<StatusHintRun> runs)
        {
            ArgumentNullException.ThrowIfNull(runs);
            Runs = runs;
        }

        /// <summary>
        /// Gets the styled segments to render left-to-right.
        /// </summary>
        public IReadOnlyList<StatusHintRun> Runs { get; }

        /// <summary>
        /// Gets whether the hint has no visible content.
        /// </summary>
        public bool IsEmpty => Runs.Count == 0;

        /// <summary>
        /// Builds a single-run hint with default styling.
        /// </summary>
        /// <param name="text">Message to show.</param>
        /// <returns>Plain hint display.</returns>
        public static StatusHintDisplay FromPlain(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return Empty;
            }

            return new StatusHintDisplay([new StatusHintRun(text)]);
        }

        /// <summary>
        /// Builds a hint from explicit runs.
        /// </summary>
        /// <param name="runs">Ordered segments to render.</param>
        /// <returns>Rich hint display.</returns>
        public static StatusHintDisplay FromRuns(params StatusHintRun[] runs)
        {
            if (runs.Length == 0)
            {
                return Empty;
            }

            return new StatusHintDisplay(runs);
        }

        /// <summary>
        /// Concatenates all run text (accessibility, tests, logging).
        /// </summary>
        /// <returns>Single-line hint text without styling.</returns>
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
