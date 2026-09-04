using Mfr.Utils;

namespace Mfr.Engine.Commit
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
    /// Pass the instance returned from <see cref="RenameList.RenameList.Preview"/> into
    /// <see cref="RenameList.RenameList.Commit"/> on the same
    /// <see cref="RenameList.RenameList"/>; steps reference that list's <see cref="RenameItem"/> instances.
    /// </para>
    /// </remarks>
    /// <param name="Steps">Commit steps to apply in order.</param>
    /// <param name="UnresolvableCycleItems">Items that participate in a cycle the planner could not break with a single stash.</param>
    /// <param name="ChangedCount">
    /// Items in <see cref="RenameStatus.PreviewOk"/> with preview changes (status-bar Changes).
    /// </param>
    /// <param name="UnchangedCount">
    /// Items in <see cref="RenameStatus.PreviewOk"/> with no preview changes.
    /// </param>
    /// <param name="ErrorCount">Items in <see cref="RenameStatus.PreviewError"/>.</param>
    public sealed record CommitPlan(
        IReadOnlyList<CommitStep> Steps,
        IReadOnlyList<RenameItem> UnresolvableCycleItems,
        int ChangedCount = 0,
        int UnchangedCount = 0,
        int ErrorCount = 0
    )
    {
        /// <summary>
        /// Counts preview outcome buckets for status / CLI summary after a full preview pass.
        /// </summary>
        /// <param name="items">All rename items after preview status is final.</param>
        /// <returns>Changed, unchanged, and error counts.</returns>
        public static (int Changed, int Unchanged, int Errors) CountOutcomes(IEnumerable<RenameItem> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            var changed = 0;
            var unchanged = 0;
            var errors = 0;
            foreach (var item in items)
            {
                if (item.Status == RenameStatus.PreviewError)
                {
                    errors++;
                    continue;
                }

                if (item.Status != RenameStatus.PreviewOk)
                {
                    continue;
                }

                if (item.HasPreviewChanges())
                {
                    changed++;
                }
                else
                {
                    unchanged++;
                }
            }

            return (changed, unchanged, errors);
        }
    }

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
            {
                return new CommitPlan(Steps: [], UnresolvableCycleItems: []);
            }

            var folderRenames = _CollectFolderRenames(participants);
            var dependsOn = _BuildDependencyEdges(participants, folderRenames);
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
                        folderRenames: folderRenames,
                        stashedTempPaths: stashedTempPaths
                    );
                    steps.Add(new FinalizeStep(readyItem, actualSourcePath));
                    remaining.Remove(readyItem);
                    continue;
                }

                var cycleHandled = _TryHandleCycle(
                    remaining: remaining,
                    dependsOn: dependsOn,
                    folderRenames: folderRenames,
                    stashedTempPaths: stashedTempPaths,
                    steps: steps,
                    unresolvable: unresolvable
                );
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
        /// <param name="folderRenames">Directory participants whose preview path changed.</param>
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
        /// <para>
        /// Edges are built with original-path lookup and a renamed-folder pass, not an all-pairs scan,
        /// so preview of tens of thousands of independent file renames stays linear.
        /// </para>
        /// </remarks>
        private static Dictionary<RenameItem, HashSet<RenameItem>> _BuildDependencyEdges(
            IReadOnlyList<RenameItem> participants,
            IReadOnlyList<RenameItem> folderRenames
        )
        {
            var dependsOn = new Dictionary<RenameItem, HashSet<RenameItem>>(ReferenceEqualityComparer.Instance);
            foreach (var item in participants)
            {
                dependsOn[item] = new HashSet<RenameItem>(ReferenceEqualityComparer.Instance);
            }

            _AddPathShiftEdges(participants, dependsOn);
            _AddContainmentEdges(participants, folderRenames, dependsOn);
            return dependsOn;
        }

        /// <summary>
        /// Adds path-shift edges via original-path lookup (O(n)), not an all-pairs scan.
        /// </summary>
        private static void _AddPathShiftEdges(
            IReadOnlyList<RenameItem> participants,
            Dictionary<RenameItem, HashSet<RenameItem>> dependsOn
        )
        {
            var originalPathToItem = new Dictionary<string, RenameItem>(PathComparers.Os);
            foreach (var item in participants)
            {
                originalPathToItem.TryAdd(item.Original.FullPath, item);
            }

            foreach (var subject in participants)
            {
                if (!_ItemPathChanges(subject))
                {
                    continue;
                }

                if (
                    !originalPathToItem.TryGetValue(subject.Preview.FullPath, out var other)
                    || ReferenceEquals(subject, other)
                    || !_ItemPathChanges(other)
                )
                {
                    continue;
                }

                dependsOn[subject].Add(other);
            }
        }

        /// <summary>
        /// Adds folder-before-descendant edges by walking renamed folders against the list, not n² pairs.
        /// </summary>
        private static void _AddContainmentEdges(
            IReadOnlyList<RenameItem> participants,
            IReadOnlyList<RenameItem> folderRenames,
            Dictionary<RenameItem, HashSet<RenameItem>> dependsOn
        )
        {
            if (folderRenames.Count == 0)
            {
                return;
            }

            foreach (var ancestor in folderRenames)
            {
                foreach (var descendant in participants)
                {
                    if (ReferenceEquals(ancestor, descendant))
                    {
                        continue;
                    }

                    if (
                        !PathRelations.IsDescendantOf(
                            candidate: descendant.Original.FullPath,
                            ancestor: ancestor.Original.FullPath
                        )
                    )
                    {
                        continue;
                    }

                    dependsOn[descendant].Add(ancestor);
                }
            }
        }

        /// <summary>
        /// Folder items in this batch whose preview path differs from the original.
        /// </summary>
        private static List<RenameItem> _CollectFolderRenames(IReadOnlyList<RenameItem> participants)
        {
            var folderRenames = new List<RenameItem>();
            foreach (var item in participants)
            {
                if (item.Original.Attributes.IsDirectory() && _ItemPathChanges(item))
                {
                    folderRenames.Add(item);
                }
            }

            return folderRenames;
        }

        private static bool _ItemPathChanges(RenameItem item)
        {
            return !string.Equals(item.Original.FullPath, item.Preview.FullPath, StringComparison.Ordinal);
        }

        private static RenameItem? _PickReadyItem(
            HashSet<RenameItem> remaining,
            Dictionary<RenameItem, HashSet<RenameItem>> dependsOn
        )
        {
            return remaining.FirstOrDefault(item => !dependsOn[item].Any(dependency => remaining.Contains(dependency)));
        }

        private static bool _TryHandleCycle(
            HashSet<RenameItem> remaining,
            Dictionary<RenameItem, HashSet<RenameItem>> dependsOn,
            IReadOnlyList<RenameItem> folderRenames,
            Dictionary<RenameItem, string> stashedTempPaths,
            List<CommitStep> steps,
            List<RenameItem> unresolvable
        )
        {
            var cycle = _FindCycleNodes(remaining, dependsOn);
            if (cycle.Count == 0)
            {
                return false;
            }

            // Stash any one cycle member; its destination is freed for others.
            var stashItem = cycle[0];
            var tempPath = RenameItemMover.AllocateTempPath(stashItem.Original.FullPath);
            steps.Add(new StashStep(stashItem, tempPath));
            stashedTempPaths[stashItem] = tempPath;

            // Commit the other cycle members in topological order (the stash broke the cycle for them).
            var otherCycleMembers = new HashSet<RenameItem>(
                cycle.Where(item => !ReferenceEquals(item, stashItem)),
                ReferenceEqualityComparer.Instance
            );
            while (otherCycleMembers.Count > 0)
            {
                var ready = _PickCycleReadyItem(
                    candidates: otherCycleMembers,
                    dependsOn: dependsOn,
                    stashItem: stashItem
                );
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
                    folderRenames: folderRenames,
                    stashedTempPaths: stashedTempPaths
                );
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
            Dictionary<RenameItem, HashSet<RenameItem>> dependsOn
        )
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
                {
                    continue;
                }

                if (!pathSet.Contains(current))
                {
                    continue;
                }

                var cycleStartIndex = pathOrder.IndexOf(current);
                if (cycleStartIndex < 0)
                {
                    continue;
                }

                var cycle = pathOrder.GetRange(cycleStartIndex, pathOrder.Count - cycleStartIndex);
                return cycle;
            }

            return [];
        }

        private static RenameItem? _PickAnyRemainingDependency(
            RenameItem item,
            Dictionary<RenameItem, HashSet<RenameItem>> dependsOn,
            HashSet<RenameItem> remaining
        )
        {
            return dependsOn[item].FirstOrDefault(remaining.Contains);
        }

        private static RenameItem? _PickCycleReadyItem(
            HashSet<RenameItem> candidates,
            Dictionary<RenameItem, HashSet<RenameItem>> dependsOn,
            RenameItem stashItem
        )
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
                    {
                        blocking++;
                    }
                }

                if (blocking == 0)
                {
                    return item;
                }
            }

            return null;
        }

        private static string _ResolveActualSourcePath(
            RenameItem item,
            IReadOnlyList<RenameItem> folderRenames,
            Dictionary<RenameItem, string> stashedTempPaths
        )
        {
            if (stashedTempPaths.TryGetValue(item, out var stashedTempPath))
            {
                return stashedTempPath;
            }

            if (folderRenames.Count == 0)
            {
                return item.Original.FullPath;
            }

            // Apply ancestor renames innermost-first so chained ancestors compose correctly.
            var ancestors = folderRenames
                .Where(other => !ReferenceEquals(other, item))
                .Where(other =>
                    PathRelations.IsDescendantOf(candidate: item.Original.FullPath, ancestor: other.Original.FullPath)
                )
                .OrderByDescending(other => other.Original.FullPath.Length);

            var actualSourcePath = item.Original.FullPath;
            foreach (var ancestor in ancestors)
            {
                actualSourcePath = PathRelations.ReplaceAncestor(
                    fullPath: actualSourcePath,
                    oldAncestor: ancestor.Original.FullPath,
                    newAncestor: ancestor.Preview.FullPath
                );
            }

            return actualSourcePath;
        }
    }
}
