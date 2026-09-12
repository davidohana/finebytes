namespace Mfr.App.Ui.ViewModels
{
    /// <summary>
    /// Builds status-bar <see cref="StyledTextDisplay"/> messages with severity brushes.
    /// </summary>
    public static class StatusBarText
    {
        /// <summary>
        /// Theme resource key for error-severity status text.
        /// </summary>
        public const string ErrorForegroundResourceKey = "AppChromeErrorForegroundBrush";

        /// <summary>
        /// Theme resource key for warning-severity status text.
        /// </summary>
        public const string WarningForegroundResourceKey = "StatusBarWarningForegroundBrush";

        /// <summary>
        /// Builds uncolored (default foreground) status text.
        /// </summary>
        /// <param name="text">Message to show.</param>
        /// <returns>Neutral styled text, or empty when <paramref name="text"/> is empty.</returns>
        public static StyledTextDisplay Neutral(string text)
        {
            return StyledTextDisplay.FromPlain(text);
        }

        /// <summary>
        /// Builds warning-severity status text.
        /// </summary>
        /// <param name="text">Message to show.</param>
        /// <returns>Warning styled text, or empty when <paramref name="text"/> is empty.</returns>
        public static StyledTextDisplay Warning(string text)
        {
            return _Single(text, WarningForegroundResourceKey);
        }

        /// <summary>
        /// Builds error-severity status text.
        /// </summary>
        /// <param name="text">Message to show.</param>
        /// <returns>Error styled text, or empty when <paramref name="text"/> is empty.</returns>
        public static StyledTextDisplay Error(string text)
        {
            return _Single(text, ErrorForegroundResourceKey);
        }

        /// <summary>
        /// Concatenates non-empty status displays left-to-right.
        /// </summary>
        /// <param name="parts">Segments to join.</param>
        /// <returns>Combined display, or empty when every part is empty.</returns>
        public static StyledTextDisplay Combine(params StyledTextDisplay[] parts)
        {
            ArgumentNullException.ThrowIfNull(parts);

            var runs = new List<StyledTextRun>();
            foreach (var part in parts)
            {
                if (part is null || part.IsEmpty)
                {
                    continue;
                }

                runs.AddRange(part.Runs);
            }

            if (runs.Count == 0)
            {
                return StyledTextDisplay.Empty;
            }

            return new StyledTextDisplay(runs);
        }

        private static StyledTextDisplay _Single(string text, string foregroundResourceKey)
        {
            if (string.IsNullOrEmpty(text))
            {
                return StyledTextDisplay.Empty;
            }

            return StyledTextDisplay.FromRuns(
                new StyledTextRun(text) { ForegroundResourceKey = foregroundResourceKey }
            );
        }
    }
}
