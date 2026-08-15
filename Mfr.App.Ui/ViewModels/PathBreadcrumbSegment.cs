namespace Mfr.App.Ui.ViewModels
{
    /// <summary>
    /// One clickable folder in the File Explorer address bar.
    /// </summary>
    public sealed class PathBreadcrumbSegment
    {
        /// <summary>
        /// Gets the text shown for this folder.
        /// </summary>
        public required string Label { get; init; }

        /// <summary>
        /// Gets the filesystem path to open, or This PC / Network display names for those roots.
        /// </summary>
        public required string TargetPath { get; init; }

        /// <summary>
        /// Gets whether a chevron is shown before this segment.
        /// </summary>
        public bool ShowLeadingChevron { get; init; }
    }
}
