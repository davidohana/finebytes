using Mfr.Engine.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Dialog title, metadata line label, and initial phase for each Rename List progress operation.
    /// </summary>
    internal static class RenameListProgressCopy
    {
        /// <summary>
        /// Progress dialog copy and starting phase for one <see cref="RenameListProgressOperation"/>.
        /// </summary>
        /// <param name="InitialPhase">Phase set when the operation starts.</param>
        /// <param name="ShowResolve">Whether scanned/added resolve lines are shown.</param>
        /// <param name="Title">Dialog title for the resolve stage (or for the whole run when metadata title is null).</param>
        /// <param name="TitleWhenLoadingMetadata">
        /// Dialog title while in <see cref="RenameListProgressPhase.LoadMetadata"/>; null keeps <paramref name="Title"/>.
        /// </param>
        /// <param name="MetadataLineLabel">Prefix for the per-row line (<c>{label}: N of M files</c>).</param>
        internal readonly record struct Spec(
            RenameListProgressPhase InitialPhase,
            bool ShowResolve,
            string Title,
            string? TitleWhenLoadingMetadata,
            string MetadataLineLabel
        );

        /// <summary>
        /// Looks up copy for <paramref name="operation"/>.
        /// </summary>
        /// <param name="operation">Active background operation.</param>
        /// <returns>Title, labels, and initial phase for that operation.</returns>
        internal static Spec For(RenameListProgressOperation operation)
        {
            return operation switch
            {
                RenameListProgressOperation.Add => new Spec(
                    InitialPhase: RenameListProgressPhase.ResolveSources,
                    ShowResolve: true,
                    Title: "Adding to Rename List",
                    TitleWhenLoadingMetadata: "Reading file metadata",
                    MetadataLineLabel: "Reading metadata"
                ),
                RenameListProgressOperation.MetadataHydrate => new Spec(
                    InitialPhase: RenameListProgressPhase.LoadMetadata,
                    ShowResolve: false,
                    Title: "Reading file metadata",
                    TitleWhenLoadingMetadata: null,
                    MetadataLineLabel: "Reading metadata"
                ),
                RenameListProgressOperation.Refresh => new Spec(
                    InitialPhase: RenameListProgressPhase.LoadMetadata,
                    ShowResolve: false,
                    Title: "Refreshing Rename List",
                    TitleWhenLoadingMetadata: null,
                    MetadataLineLabel: "Refreshing"
                ),
                RenameListProgressOperation.Preview => new Spec(
                    InitialPhase: RenameListProgressPhase.LoadMetadata,
                    ShowResolve: false,
                    Title: "Previewing ...",
                    TitleWhenLoadingMetadata: null,
                    MetadataLineLabel: "Previewing"
                ),
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, message: null),
            };
        }

        /// <summary>
        /// Resolves the dialog window title for the current operation and phase.
        /// </summary>
        /// <param name="operation">Active background operation.</param>
        /// <param name="phase">Current engine-reported phase.</param>
        /// <returns>Title string for the progress dialog.</returns>
        internal static string DialogTitle(RenameListProgressOperation operation, RenameListProgressPhase phase)
        {
            var copy = For(operation);
            if (phase == RenameListProgressPhase.LoadMetadata && copy.TitleWhenLoadingMetadata is not null)
            {
                return copy.TitleWhenLoadingMetadata;
            }

            return copy.Title;
        }

        /// <summary>
        /// Formats the per-row metadata/refresh/preview progress line.
        /// </summary>
        /// <param name="operation">Active background operation.</param>
        /// <param name="processedCount">Rows processed so far.</param>
        /// <param name="totalCount">Total rows for this stage.</param>
        /// <returns>User-visible progress line.</returns>
        internal static string MetadataProgressText(
            RenameListProgressOperation operation,
            int processedCount,
            int totalCount
        )
        {
            return $"{For(operation).MetadataLineLabel}: {processedCount} of {totalCount} files";
        }
    }
}
