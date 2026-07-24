using Mfr.Models;
using Mfr.Utils;

namespace Mfr.Core
{
    /// <summary>
    /// Represents one operation in the commit plan for a rename batch.
    /// </summary>
    /// <param name="Item">The rename item this step operates on.</param>
    public abstract record CommitStep(RenameItem Item);

    /// <summary>
    /// Stashes an item's on-disk source to a unique temp path so that other items can claim its original path.
    /// </summary>
    /// <param name="Item">The item being stashed.</param>
    /// <param name="TempPath">The temp destination path.</param>
    public sealed record StashStep(RenameItem Item, string TempPath) : CommitStep(Item);

    /// <summary>
    /// Finalizes an item's commit, optionally moving from a stashed source path.
    /// </summary>
    /// <param name="Item">The item being committed.</param>
    /// <param name="ActualSourcePath">The on-disk source path to move from (may equal <see cref="RenameItem.Original"/>'s full path or a stash temp path or an ancestor-rebased path).</param>
    public sealed record FinalizeStep(RenameItem Item, string ActualSourcePath) : CommitStep(Item);

    /// <summary>
    /// Ordered rename operations produced by <see cref="CommitPlanner.Build"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Pass the instance returned from <see cref="RenameList.Preview"/> into <see cref="RenameList.Commit"/> on the same
    /// <see cref="RenameList"/>; steps reference that list's <see cref="RenameItem"/> instances.
    /// </para>
    /// </remarks>
    /// <param name="Steps">Commit steps to apply in order.</param>
    /// <param name="UnresolvableCycleItems">Items that participate in a cycle the planner could not break with a single stash.</param>
    public sealed record CommitPlan(
        IReadOnlyList<CommitStep> Steps,
        IReadOnlyList<RenameItem> UnresolvableCycleItems);

    /// <summary>
    /// Builds an ordered commit plan that respects ancestor/descendant containment, path-shift chains, and cycles.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The planner only considers items in <see cref="RenameStatus.PreviewOk"/> state with actual preview changes.
    /// Items that have no preview changes are omitted from the plan; the host's commit loop handles them via the
    /// existing skip path.
    /// </para>
    /// <para>
    /// Dependency edges <c>X depends on Y</c> mean <c>Y</c> must commit before <c>X</c>. Two kinds of edges exist:
    /// </para>
    /// <para>
    /// 1. Containment: if <c>Y</c> is a folder being renamed and <c>X.Original.FullPath</c> is a descendant of
    /// <c>Y.Original.FullPath</c>, then <c>Y</c> must commit first; <c>X</c>'s actual source is rebased onto <c>Y.Preview.FullPath</c>.
    /// </para>
    /// <para>
    /// 2. Path-shift: if <c>X.Preview.FullPath</c> equals <c>Y.Original.FullPath</c>, <c>Y</c> must move first to vacate
    /// the path <c>X</c> claims.
    /// </para>
    /// </remarks>
    internal static class CommitPlanner
    {
        /// <summary>
        /// Builds a commit plan for the given rename items.
        /// </summary>
        /// <param name="items">All rename items participating in the current preview pass.</param>
        /// <returns>An ordered <see cref="CommitPlan"/>.</returns>
        internal static CommitPlan Build(IReadOnlyList<RenameItem> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            var participants = items
                .Where(item => item.Status == RenameStatus.PreviewOk && item.HasPreviewChanges())
                .ToList();
            if (participants.Count == 0)
                return new CommitPlan(Steps: [], UnresolvableCycleItems: []);

            var dependsOn = _BuildDependencyEdges(participants);
            var steps = new List<CommitStep>();
            var unresolvable = new List<RenameItem>();
            var remaining = new HashSet<RenameItem>(participants);
            var stashedTempPaths = new Dictionary<RenameItem, string>();

            while (remaining.Count > 0)
            {
                var readyItem = _PickReadyItem(remaining, dependsOn);
                if (readyItem is not null)
                {
                    var actualSourcePath = _ResolveActualSourcePath(
                        item: readyItem,
                        participants: participants,
                        stashedTempPaths: stashedTempPaths);
                    steps.Add(new FinalizeStep(readyItem, actualSourcePath));
                    remaining.Remove(readyItem);
                    continue;
                }

                var cycleHandled = _TryHandleCycle(
                    remaining: remaining,
                    dependsOn: dependsOn,
                    participants: participants,
                    stashedTempPaths: stashedTempPaths,
                    steps: steps,
                    unresolvable: unresolvable);
                if (!cycleHandled)
                {
                    unresolvable.AddRange(remaining);
                    break;
                }
            }

            return new CommitPlan(Steps: steps, UnresolvableCycleItems: unresolvable);
        }

        /// <summary>
        /// Builds a dependency graph over <paramref name="participants"/> as a map of
        /// <c>item → set of items that must commit before it</c>.
        /// </summary>
        /// <param name="participants">All items that will be committed in this batch.</param>
        /// <returns>
        /// A dictionary keyed by every participant; the value is the set of other participants
        /// whose commit must precede this item's commit.
        /// An empty set means the item has no prerequisites and is immediately eligible.
        /// </returns>
        /// <remarks>
        /// <para>Two kinds of edge are recognised:</para>
        /// <para>
        /// <b>Containment:</b> if <c>other</c> is a folder being renamed and <c>subject.Original.FullPath</c>
        /// is a descendant of <c>other.Original.FullPath</c>, then <c>other</c> must commit first so the
        /// folder is in its new location before the child is moved.
        /// </para>
        /// <para>
        /// <b>Path-shift:</b> if <c>subject.Preview.FullPath</c> equals <c>other.Original.FullPath</c>,
        /// then <c>other</c> must vacate that path before <c>subject</c> can claim it.
        /// </para>
        /// </remarks>
        private static Dictionary<RenameItem, HashSet<RenameItem>> _BuildDependencyEdges(
            IReadOnlyList<RenameItem> participants)
        {
            var dependsOn = new Dictionary<RenameItem, HashSet<RenameItem>>(ReferenceEqualityComparer.Instance);
            foreach (var item in participants)
            {
                dependsOn[item] = new HashSet<RenameItem>(ReferenceEqualityComparer.Instance);
            }

            foreach (var subject in participants)
            {
                foreach (var other in participants)
                {
                    if (ReferenceEquals(subject, other))
                        continue;

                    var containmentEdge = _IsAncestorRenameOf(ancestor: other, descendant: subject);
                    var pathShiftEdge = _SubjectPreviewClaimsOtherSource(subject: subject, other: other);
                    if (containmentEdge || pathShiftEdge)
                        dependsOn[subject].Add(other);

                }
            }

            return dependsOn;
        }

        private static bool _IsAncestorRenameOf(RenameItem ancestor, RenameItem descendant)
        {
            if (!ancestor.Original.Attributes.IsDirectory())
                return false;

            var ancestorRenames = !string.Equals(
                            ancestor.Original.FullPath,
                            ancestor.Preview.FullPath,
                            StringComparison.Ordinal);
            if (!ancestorRenames)
                return false;

            return PathRelations.IsDescendantOf(
                            candidate: descendant.Original.FullPath,
                            ancestor: ancestor.Original.FullPath);
        }

        private static bool _SubjectPreviewClaimsOtherSource(RenameItem subject, RenameItem other)
        {
            var subjectPathChanges = !string.Equals(
                subject.Original.FullPath,
                subject.Preview.FullPath,
                StringComparison.Ordinal);
            if (!subjectPathChanges)
                return false;

            var otherPathChanges = !string.Equals(
                            other.Original.FullPath,
                            other.Preview.FullPath,
                            StringComparison.Ordinal);
            if (!otherPathChanges)
                return false;

            return PathComparers.Os.Equals(subject.Preview.FullPath, other.Original.FullPath);
        }

        private static RenameItem? _PickReadyItem(
            HashSet<RenameItem> remaining,
            Dictionary<RenameItem, HashSet<RenameItem>> dependsOn)
        {
            return remaining.FirstOrDefault(
                item => !dependsOn[item].Any(dependency => remaining.Contains(dependency)));
        }

        private static bool _TryHandleCycle(
            HashSet<RenameItem> remaining,
            Dictionary<RenameItem, HashSet<RenameItem>> dependsOn,
            IReadOnlyList<RenameItem> participants,
            Dictionary<RenameItem, string> stashedTempPaths,
            List<CommitStep> steps,
            List<RenameItem> unresolvable)
        {
            var cycle = _FindCycleNodes(remaining, dependsOn);
            if (cycle.Count == 0)
                return false;

            // Stash any one cycle member; its destination is freed for others.
            var stashItem = cycle[0];
            var tempPath = RenameItemMover.AllocateTempPath(stashItem.Original.FullPath);
            steps.Add(new StashStep(stashItem, tempPath));
            stashedTempPaths[stashItem] = tempPath;

            // Commit the other cycle members in topological order (the stash broke the cycle for them).
            var otherCycleMembers = new HashSet<RenameItem>(
                cycle.Where(item => !ReferenceEquals(item, stashItem)),
                ReferenceEqualityComparer.Instance);
            while (otherCycleMembers.Count > 0)
            {
                var ready = _PickCycleReadyItem(
                    candidates: otherCycleMembers,
                    dependsOn: dependsOn,
                    stashItem: stashItem);
                if (ready is null)
                {
                    foreach (var stuck in otherCycleMembers)
                    {
                        unresolvable.Add(stuck);
                    }

                    foreach (var stuck in otherCycleMembers)
                    {
                        remaining.Remove(stuck);
                    }

                    unresolvable.Add(stashItem);
                    remaining.Remove(stashItem);
                    return true;
                }

                var actualSourcePath = _ResolveActualSourcePath(
                    item: ready,
                    participants: participants,
                    stashedTempPaths: stashedTempPaths);
                steps.Add(new FinalizeStep(ready, actualSourcePath));
                otherCycleMembers.Remove(ready);
                remaining.Remove(ready);
            }

            // Finalize the stashed item now that the cycle has been resolved around it.
            steps.Add(new FinalizeStep(stashItem, tempPath));
            remaining.Remove(stashItem);
            return true;
        }

        private static List<RenameItem> _FindCycleNodes(
            HashSet<RenameItem> remaining,
            Dictionary<RenameItem, HashSet<RenameItem>> dependsOn)
        {
            // Walk dependencies until we revisit a node. The portion of the path between visits forms a cycle.
            foreach (var startNode in remaining)
            {
                var pathOrder = new List<RenameItem>();
                var pathSet = new HashSet<RenameItem>(ReferenceEqualityComparer.Instance);
                var current = startNode;
                while (current is not null && remaining.Contains(current) && !pathSet.Contains(current))
                {
                    pathOrder.Add(current);
                    pathSet.Add(current);
                    current = _PickAnyRemainingDependency(current, dependsOn, remaining);
                }

                if (current is null)
                    continue;

                if (!pathSet.Contains(current))
                    continue;

                var cycleStartIndex = pathOrder.IndexOf(current);
                if (cycleStartIndex < 0)
                    continue;

                var cycle = pathOrder.GetRange(cycleStartIndex, pathOrder.Count - cycleStartIndex);
                return cycle;
            }

            return [];
        }

        private static RenameItem? _PickAnyRemainingDependency(
            RenameItem item,
            Dictionary<RenameItem, HashSet<RenameItem>> dependsOn,
            HashSet<RenameItem> remaining)
        {
            return dependsOn[item].FirstOrDefault(remaining.Contains);
        }

        private static RenameItem? _PickCycleReadyItem(
            HashSet<RenameItem> candidates,
            Dictionary<RenameItem, HashSet<RenameItem>> dependsOn,
            RenameItem stashItem)
        {
            foreach (var item in candidates)
            {
                var blocking = 0;
                foreach (var dependency in dependsOn[item])
                {
                    if (ReferenceEquals(dependency, stashItem))
                    {
                        // The stash freed this dependency.
                        continue;
                    }

                    if (candidates.Contains(dependency))
                        blocking++;

                }

                if (blocking == 0)
                    return item;

            }

            return null;
        }

        private static string _ResolveActualSourcePath(
            RenameItem item,
            IReadOnlyList<RenameItem> participants,
            Dictionary<RenameItem, string> stashedTempPaths)
        {
            if (stashedTempPaths.TryGetValue(item, out var stashedTempPath))
                return stashedTempPath;

            // Apply ancestor renames innermost-first so chained ancestors compose correctly.
            var ancestors = participants
                .Where(other => !ReferenceEquals(other, item))
                .Where(other => _IsAncestorRenameOf(ancestor: other, descendant: item))
                .OrderByDescending(other => other.Original.FullPath.Length);

            var actualSourcePath = item.Original.FullPath;
            foreach (var ancestor in ancestors)
            {
                actualSourcePath = PathRelations.ReplaceAncestor(
                    fullPath: actualSourcePath,
                    oldAncestor: ancestor.Original.FullPath,
                    newAncestor: ancestor.Preview.FullPath);
            }

            return actualSourcePath;
        }
    }
}
