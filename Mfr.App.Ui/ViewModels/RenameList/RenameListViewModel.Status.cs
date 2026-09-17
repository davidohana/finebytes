namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Status-bar fragment helpers shared by GO commit and Prepare Undo outcomes.
    /// </summary>
    public sealed partial class RenameListViewModel
    {
        /// <summary>
        /// Builds the stopped or success primary fragment for GO status lines.
        /// </summary>
        /// <param name="successCount">CommitOk count.</param>
        /// <param name="stopped">Whether the operation was canceled mid-progress.</param>
        /// <param name="successPastVerb">Past-tense verb (<c>Renamed</c>).</param>
        /// <returns>
        /// Warning when stopped; Neutral success when <paramref name="successCount"/> &gt; 0 and not stopped;
        /// otherwise <see langword="null"/>.
        /// </returns>
        private static StyledTextDisplay? _FormatSuccessPrimary(int successCount, bool stopped, string successPastVerb)
        {
            if (stopped)
            {
                return successCount > 0
                    ? StatusBarText.Warning($"Stopped. {successPastVerb} {successCount} item(s).")
                    : StatusBarText.Warning("Stopped.");
            }

            if (successCount > 0)
            {
                return StatusBarText.Neutral($"{successPastVerb} {successCount} item(s).");
            }

            return null;
        }

        /// <summary>
        /// Space-joins non-empty status fragments (single part returned as-is).
        /// </summary>
        /// <param name="parts">Ordered fragments to combine.</param>
        /// <returns>Combined display.</returns>
        private static StyledTextDisplay _CombineStatusParts(List<StyledTextDisplay> parts)
        {
            ArgumentNullException.ThrowIfNull(parts);

            if (parts.Count == 0)
            {
                return StatusBarText.Neutral(string.Empty);
            }

            if (parts.Count == 1)
            {
                return parts[0];
            }

            var combined = parts[0];
            for (var i = 1; i < parts.Count; i++)
            {
                combined = StatusBarText.Combine(combined, StatusBarText.Neutral(" "), parts[i]);
            }

            return combined;
        }
    }
}
