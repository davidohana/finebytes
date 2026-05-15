using Mfr.Metadata;
using Mfr.Models;
using Serilog;

namespace Mfr.Core
{
    /// <summary>
    /// Per-item outcome captured while walking a commit plan.
    /// </summary>
    /// <param name="OriginalPathBeforeCommit">The item's original path when the plan step ran.</param>
    /// <param name="DestinationPath">The item's preview path when the plan step ran.</param>
    /// <param name="Changes">Property-level changes captured before <see cref="RenameItem.Original"/> was overwritten.</param>
    /// <param name="Status">The status this item finished the plan walk with.</param>
    /// <param name="ErrorMessage">Optional error message when the plan step failed.</param>
    internal sealed record PlanOutcome(
        string OriginalPathBeforeCommit,
        string DestinationPath,
        IReadOnlyList<RenamePropertyChange> Changes,
        RenameStatus Status,
        string? ErrorMessage);

    /// <summary>
    /// Executes a <see cref="CommitPlan"/> against the filesystem and produces per-item results.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The executor is the runtime counterpart of <see cref="CommitPlanner"/>: the planner
    /// decides what order items commit in; the executor carries out those steps, tracks outcomes,
    /// and handles confirmation, dry-run, and fail-fast semantics.
    /// </para>
    /// <para>
    /// Rows in <see cref="RenameStatus.PreviewError"/> are never committed; they appear in results as
    /// <see cref="RenameStatus.PreviewError"/> with no filesystem changes.
    /// </para>
    /// </remarks>
    internal static class CommitExecutor
    {
        /// <summary>
        /// Runs a commit plan and returns one result per item in <paramref name="allItems"/>.
        /// </summary>
        /// <param name="plan">The plan produced by <see cref="CommitPlanner.Build"/>.</param>
        /// <param name="allItems">All items in the rename list, in insertion order.</param>
        /// <param name="confirmBeforeApply">
        /// Optional callback invoked immediately before each item is committed.
        /// Return <c>false</c> to skip that item with <see cref="RenameStatus.CommitSkipped"/>.
        /// Items whose source is already stashed to a temp path bypass this callback to avoid orphaned files.
        /// </param>
        /// <param name="failFast">If <c>true</c>, stops after the first per-item error.</param>
        /// <param name="dryRun">If <c>true</c>, skips all filesystem writes.</param>
        /// <returns>Per-item commit outcomes in the same order as <paramref name="allItems"/>.</returns>
        internal static IReadOnlyList<RenameResultItem> Execute(
            CommitPlan plan,
            IReadOnlyList<RenameItem> allItems,
            Func<RenameItem, bool>? confirmBeforeApply,
            bool failFast,
            bool dryRun)
        {
            var outcomes = new Dictionary<RenameItem, PlanOutcome>(ReferenceEqualityComparer.Instance);

            _ExecutePlan(
                plan: plan,
                confirmBeforeApply: confirmBeforeApply,
                failFast: failFast,
                dryRun: dryRun,
                outcomes: outcomes);

            return [.. allItems.Select(item => _BuildResultForItem(item: item, outcomes: outcomes))];
        }

        /// <summary>
        /// Executes the ordered plan steps, honoring <paramref name="failFast"/> and <paramref name="dryRun"/>.
        /// </summary>
        /// <param name="plan">The plan produced during preview.</param>
        /// <param name="confirmBeforeApply">Optional per-item confirmation callback; called immediately before each finalize step.</param>
        /// <param name="failFast">Whether to stop on the first plan-step error.</param>
        /// <param name="dryRun">Whether to skip filesystem writes.</param>
        /// <param name="outcomes">Per-item outcome accumulator populated by this method.</param>
        private static void _ExecutePlan(
            CommitPlan plan,
            Func<RenameItem, bool>? confirmBeforeApply,
            bool failFast,
            bool dryRun,
            Dictionary<RenameItem, PlanOutcome> outcomes)
        {
            var stopped = false;
            var inFlightStashedItems = new HashSet<RenameItem>(ReferenceEqualityComparer.Instance);

            foreach (var step in plan.Steps)
            {
                if (stopped)
                    break;

                if (step.Item.Status != RenameStatus.PreviewOk)
                    continue;

                if (step is StashStep stashStep)
                {
                    _ExecuteStashStep(step: stashStep, dryRun: dryRun, outcomes: outcomes);
                    inFlightStashedItems.Add(stashStep.Item);
                    continue;
                }

                if (step is FinalizeStep finalizeStep)
                {
                    // While a cycle resolution is in flight (stash+finalize pair), the callback is bypassed
                    // to avoid orphaning a source that is already stashed at a temp path.
                    var withinCycleResolution = inFlightStashedItems.Contains(finalizeStep.Item);
                    var confirmed = withinCycleResolution
                        || confirmBeforeApply is null
                        || confirmBeforeApply(finalizeStep.Item);
                    if (!confirmed)
                    {
                        inFlightStashedItems.Remove(finalizeStep.Item);
                        continue;
                    }

                    var stepFailed = !_ExecuteFinalizeStep(
                        step: finalizeStep,
                        dryRun: dryRun,
                        outcomes: outcomes);
                    inFlightStashedItems.Remove(finalizeStep.Item);
                    if (stepFailed && failFast)
                        stopped = true;

                }
            }
        }

        private static void _ExecuteStashStep(
            StashStep step,
            bool dryRun,
            Dictionary<RenameItem, PlanOutcome> outcomes)
        {
            if (dryRun)
                return;

            try
            {
                RenameItemMover.StashSourceToTemp(step.Item, step.TempPath);
            }
            catch (Exception ex)
            {
                step.Item.CommitError = new RenameItemError(Message: ex.Message, Cause: ex);
                step.Item.Status = RenameStatus.CommitError;
                outcomes[step.Item] = new PlanOutcome(
                    OriginalPathBeforeCommit: step.Item.Original.FullPath,
                    DestinationPath: step.Item.Preview.FullPath,
                    Changes: [],
                    Status: RenameStatus.CommitError,
                    ErrorMessage: ex.Message);
                Log.Error(
                    ex,
                    "Stash failed for '{SourcePath}' -> '{TempPath}'.",
                    step.Item.Original.FullPath,
                    step.TempPath);
            }
        }

        private static bool _ExecuteFinalizeStep(
            FinalizeStep step,
            bool dryRun,
            Dictionary<RenameItem, PlanOutcome> outcomes)
        {
            var item = step.Item;
            item.CommitError = null;

            var originalPathBeforeCommit = item.Original.FullPath;
            var destinationPath = item.Preview.FullPath;
            var originalSnapshot = item.Original;
            var previewSnapshot = item.Preview;

            try
            {
                var shouldPersistEmbeddedTags = !dryRun
                    && !previewSnapshot.AudioTagOverlay.Equals(originalSnapshot.AudioTagOverlay);

                if (!dryRun)
                    RenameItemMover.FinalizeCommit(item, step.ActualSourcePath);

                if (shouldPersistEmbeddedTags)
                    AudioTagPersistence.Apply(item.Preview.FullPath, previewSnapshot.AudioTagOverlay);

                item.Status = RenameStatus.CommitOk;
                var changes = RenamePropertyChangeBuilder.BuildChangeRows(
                    originalSnapshot: originalSnapshot,
                    previewSnapshot: previewSnapshot);
                outcomes[item] = new PlanOutcome(
                    OriginalPathBeforeCommit: originalPathBeforeCommit,
                    DestinationPath: destinationPath,
                    Changes: changes,
                    Status: RenameStatus.CommitOk,
                    ErrorMessage: null);
                return true;
            }
            catch (Exception ex)
            {
                item.CommitError = new RenameItemError(Message: ex.Message, Cause: ex);
                item.Status = RenameStatus.CommitError;
                outcomes[item] = new PlanOutcome(
                    OriginalPathBeforeCommit: originalPathBeforeCommit,
                    DestinationPath: destinationPath,
                    Changes: [],
                    Status: RenameStatus.CommitError,
                    ErrorMessage: ex.Message);
                Log.Error(
                    ex,
                    "Commit failed for '{SourcePath}' -> '{DestinationPath}' (actual source '{ActualSourcePath}').",
                    originalPathBeforeCommit,
                    destinationPath,
                    step.ActualSourcePath);
                return false;
            }
        }

        private static RenameResultItem _BuildResultForItem(
            RenameItem item,
            Dictionary<RenameItem, PlanOutcome> outcomes)
        {
            if (outcomes.TryGetValue(item, out var outcome))
            {
                return new RenameResultItem(
                    OriginalPath: outcome.OriginalPathBeforeCommit,
                    Status: outcome.Status,
                    Error: outcome.ErrorMessage,
                    Changes: outcome.Changes);
            }

            if (item.Status == RenameStatus.PreviewError)
            {
                var previewMessage = item.PreviewError?.Message;
                return new RenameResultItem(
                    OriginalPath: item.Original.FullPath,
                    Status: RenameStatus.PreviewError,
                    Error: previewMessage,
                    Changes: []);
            }

            // No plan-walk outcome was recorded for this item. That covers preview-only no-change
            // items, confirmation rejections, and fail-fast skips.
            item.Status = RenameStatus.CommitSkipped;
            return new RenameResultItem(
                OriginalPath: item.Original.FullPath,
                Status: RenameStatus.CommitSkipped,
                Error: null,
                Changes: []);
        }
    }
}
