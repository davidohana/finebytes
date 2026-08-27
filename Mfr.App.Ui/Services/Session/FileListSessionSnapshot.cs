using Mfr.Models.Config;

namespace Mfr.App.Ui.Services.Session
{
    /// <summary>
    /// File List mask and folder fields exchanged with <see cref="SessionState"/>.
    /// </summary>
    /// <param name="LastOpenedDirectory">Current folder path when captured for save.</param>
    /// <param name="FileMask">Include mask, or <see langword="null"/> when unset in session.</param>
    /// <param name="ExcludeMasks">
    /// Exclude masks, or <see langword="null"/> when unset in session.
    /// </param>
    /// <param name="ExcludeMasksEnabled">
    /// Exclude-mask enable flag, or <see langword="null"/> when unset in session.
    /// </param>
    /// <param name="MaskSuggestions">
    /// Recently used include masks, or <see langword="null"/> when unset in session.
    /// </param>
    internal sealed record FileListSessionSnapshot(
        string? LastOpenedDirectory,
        string? FileMask,
        IReadOnlyList<string>? ExcludeMasks,
        bool? ExcludeMasksEnabled,
        IReadOnlyList<string>? MaskSuggestions
    )
    {
        /// <summary>
        /// Builds a snapshot from persisted session fields.
        /// </summary>
        /// <param name="session">Loaded session document.</param>
        /// <returns>Snapshot used when restoring File List mask fields from session.</returns>
        public static FileListSessionSnapshot FromSessionState(SessionState session)
        {
            ArgumentNullException.ThrowIfNull(session);

            var fileList = session.FileList;

            return new FileListSessionSnapshot(
                fileList?.LastOpenedDirectory,
                fileList?.FileMask,
                fileList?.ExcludeMasks,
                fileList?.ExcludeMasksEnabled,
                fileList?.MaskSuggestions
            );
        }
    }
}
