using Mfr.Models.Config;

namespace Mfr.App.Ui.Services.Session
{
    /// <summary>
    /// File List mask, folder, and view-mode fields exchanged with <see cref="SessionState"/>.
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
    /// <param name="ViewMode">
    /// File List layout mode, or <see langword="null"/> when unset in session.
    /// </param>
    internal sealed record FileListSessionSnapshot(
        string? LastOpenedDirectory,
        string? FileMask,
        IReadOnlyList<string>? ExcludeMasks,
        bool? ExcludeMasksEnabled,
        IReadOnlyList<string>? MaskSuggestions,
        FileListViewMode? ViewMode
    )
    {
        /// <summary>
        /// Builds a snapshot from persisted session fields.
        /// </summary>
        /// <param name="session">Loaded session document.</param>
        /// <returns>Snapshot used when restoring File List fields from session.</returns>
        public static FileListSessionSnapshot FromSessionState(SessionState session)
        {
            ArgumentNullException.ThrowIfNull(session);

            var fileList = session.FileList;

            return new FileListSessionSnapshot(
                fileList?.LastOpenedDirectory,
                fileList?.FileMask,
                fileList?.ExcludeMasks,
                fileList?.ExcludeMasksEnabled,
                fileList?.MaskSuggestions,
                _ParseViewMode(fileList?.ViewMode)
            );
        }

        /// <summary>
        /// Formats <paramref name="viewMode"/> for <see cref="SessionStateFileList.ViewMode"/>.
        /// </summary>
        /// <param name="viewMode">Layout mode to persist.</param>
        /// <returns>Camel-case session token.</returns>
        internal static string FormatViewMode(FileListViewMode viewMode)
        {
            return viewMode switch
            {
                FileListViewMode.LargeIcons => "largeIcons",
                FileListViewMode.SmallIcons => "smallIcons",
                FileListViewMode.Report => "report",
                FileListViewMode.List => "list",
                FileListViewMode.Tiles => "tiles",
                FileListViewMode.Thumbnails => "thumbnails",
                _ => "report",
            };
        }

        /// <summary>
        /// Parses a session view-mode token; unrecognized values are treated as unset.
        /// </summary>
        /// <param name="viewMode">Persisted token, or <see langword="null"/>.</param>
        /// <returns>Parsed mode, or <see langword="null"/> when missing or unknown.</returns>
        private static FileListViewMode? _ParseViewMode(string? viewMode)
        {
            if (string.IsNullOrWhiteSpace(viewMode))
            {
                return null;
            }

            return viewMode.Trim().ToLowerInvariant() switch
            {
                "largeicons" => FileListViewMode.LargeIcons,
                "smallicons" => FileListViewMode.SmallIcons,
                "report" => FileListViewMode.Report,
                "list" => FileListViewMode.List,
                "tiles" => FileListViewMode.Tiles,
                "thumbnails" => FileListViewMode.Thumbnails,
                _ => null,
            };
        }
    }
}
