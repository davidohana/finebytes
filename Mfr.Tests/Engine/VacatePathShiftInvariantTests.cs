using Mfr.Utils;

namespace Mfr.Tests.Engine
{
    /// <summary>
    /// Cross-checks conflict vacate policy against CommitPlanner path-shift / containment / folder-vacate edges.
    /// </summary>
    /// <remarks>
    /// Locks preview-ok ↔ commit-order agreement so a destination treated as vacated at conflict time
    /// is always ordered after its vacating participant(s) in the commit plan.
    /// </remarks>
    public sealed class VacatePathShiftInvariantTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <summary>
        /// Disposes temporary test resources created for this test method.
        /// </summary>
        public void Dispose()
        {
            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Exact path-shift: claimer destination equals vacator's original — no conflict, vacator finalizes first.
        /// </summary>
        [Fact]
        public void Exact_path_shift_vacate_matches_planner_order()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var vacatorPath = dir.CombinePath("b.txt");
            var claimerPath = dir.CombinePath("a.txt");
            File.WriteAllText(vacatorPath, "b");
            File.WriteAllText(claimerPath, "a");

            var vacator = _CreateItemFromExistingFile(vacatorPath);
            var claimer = _CreateItemFromExistingFile(claimerPath);
            _RetargetPreview(vacator, dir, "c.txt");
            _RetargetPreview(claimer, dir, "b.txt");

            // Claimer listed first so planner cannot accidentally pick the right order by list order alone.
            var items = new List<RenameItem> { claimer, vacator };
            PreviewConflictDetector.MarkConflicts(items);
            var plan = CommitPlanner.Build(items);

            Assert.All(items, item => Assert.Equal(RenameStatus.PreviewOk, item.Status));
            Assert.Empty(plan.UnresolvableCycleItems);
            _AssertFinalizesBefore(plan, earlier: vacator, later: claimer);
        }

        /// <summary>
        /// Swap cycle: both destinations vacated by the other — no conflict, stash breaks the cycle.
        /// </summary>
        [Fact]
        public void Swap_cycle_vacate_matches_planner_stash()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var pathA = dir.CombinePath("a.txt");
            var pathB = dir.CombinePath("b.txt");
            File.WriteAllText(pathA, "a");
            File.WriteAllText(pathB, "b");

            var itemA = _CreateItemFromExistingFile(pathA);
            var itemB = _CreateItemFromExistingFile(pathB);
            _RetargetPreview(itemA, dir, "b.txt");
            _RetargetPreview(itemB, dir, "a.txt");

            var items = new List<RenameItem> { itemA, itemB };
            PreviewConflictDetector.MarkConflicts(items);
            var plan = CommitPlanner.Build(items);

            Assert.All(items, item => Assert.Equal(RenameStatus.PreviewOk, item.Status));
            Assert.Empty(plan.UnresolvableCycleItems);
            Assert.Contains(plan.Steps, step => step is StashStep);
            Assert.Equal(2, plan.Steps.Count(step => step is FinalizeStep));
        }

        /// <summary>
        /// Destination under a renamed folder is vacated — no conflict, folder finalizes before claimer.
        /// </summary>
        [Fact]
        public void Folder_destination_vacate_matches_planner_order()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var oldFolder = dir.CombinePath("Album");
            Directory.CreateDirectory(oldFolder);
            var nestedOnDisk = oldFolder.CombinePath("nested.txt");
            File.WriteAllText(nestedOnDisk, "nested");

            var outsidePath = dir.CombinePath("outside.txt");
            File.WriteAllText(outsidePath, "out");

            var folderItem = _CreateDirectoryItem(dir, "Album");
            _RetargetPreview(folderItem, dir, "AlbumRenamed");

            var claimer = _CreateItemFromExistingFile(outsidePath);
            // Claim a path that currently exists under the folder being renamed away.
            claimer.Preview.DirectoryPath = oldFolder;
            claimer.Preview.Prefix = "nested";
            claimer.Preview.Extension = "txt";

            // Claimer first so order is forced by the vacate edge, not encounter order.
            var items = new List<RenameItem> { claimer, folderItem };
            PreviewConflictDetector.MarkConflicts(items);
            var plan = CommitPlanner.Build(items);

            Assert.All(items, item => Assert.Equal(RenameStatus.PreviewOk, item.Status));
            Assert.Empty(plan.UnresolvableCycleItems);
            _AssertFinalizesBefore(plan, earlier: folderItem, later: claimer);
        }

        /// <summary>
        /// Containment: descendant source under renamed folder — no conflict, folder finalizes before descendant.
        /// </summary>
        [Fact]
        public void Containment_source_under_folder_matches_planner_order()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var oldFolder = dir.CombinePath("Album");
            var newFolder = dir.CombinePath("AlbumRenamed");
            Directory.CreateDirectory(oldFolder);
            var nestedFilePath = oldFolder.CombinePath("track.mp3");
            File.WriteAllText(nestedFilePath, "x");

            var folderItem = _CreateDirectoryItem(dir, "Album");
            _RetargetPreview(folderItem, dir, "AlbumRenamed");

            var nestedItem = _CreateItemFromExistingFile(nestedFilePath);
            nestedItem.Preview.DirectoryPath = newFolder;
            nestedItem.Preview.Prefix = "track-renamed";

            var items = new List<RenameItem> { nestedItem, folderItem };
            PreviewConflictDetector.MarkConflicts(items);
            var plan = CommitPlanner.Build(items);

            Assert.All(items, item => Assert.Equal(RenameStatus.PreviewOk, item.Status));
            Assert.Empty(plan.UnresolvableCycleItems);
            _AssertFinalizesBefore(plan, earlier: folderItem, later: nestedItem);
        }

        /// <summary>
        /// Shared vacate helper: exact moving source and folder-descendant agree with policy predicates.
        /// </summary>
        [Fact]
        public void BatchDestinationVacate_exact_and_folder_descendant_predicates()
        {
            var root = TestPaths.Absolute("batch", "vacate");
            var folderOriginal = Path.Combine(root, "Album");
            var underFolder = Path.Combine(folderOriginal, "track.txt");
            var sibling = Path.Combine(root, "sibling.txt");

            var folderItem = _CreateDirectoryItem(root, "Album");
            folderItem.Preview.DirectoryPath = root;
            folderItem.Preview.Prefix = "AlbumRenamed";

            var movingSources = new HashSet<string>(PathComparers.Os) { sibling };
            var folderRenames = new List<RenameItem> { folderItem };

            Assert.True(BatchDestinationVacate.WillBeVacated(sibling, movingSources, folderRenames));
            Assert.True(BatchDestinationVacate.WillBeVacated(underFolder, movingSources, folderRenames));
            Assert.False(
                BatchDestinationVacate.WillBeVacated(Path.Combine(root, "other.txt"), movingSources, folderRenames)
            );
            Assert.False(BatchDestinationVacate.IsVacatedByFolderRename(folderOriginal, folderRenames));
            Assert.Same(
                folderItem,
                Assert.Single(BatchDestinationVacate.FoldersThatVacate(underFolder, folderRenames))
            );
        }

        private static void _AssertFinalizesBefore(CommitPlan plan, RenameItem earlier, RenameItem later)
        {
            var finalizeOrder = plan.Steps.OfType<FinalizeStep>().Select(step => step.Item).ToList();
            var earlierIndex = finalizeOrder.FindIndex(item => ReferenceEquals(item, earlier));
            var laterIndex = finalizeOrder.FindIndex(item => ReferenceEquals(item, later));
            Assert.True(earlierIndex >= 0, "Expected earlier item in finalize steps.");
            Assert.True(laterIndex >= 0, "Expected later item in finalize steps.");
            Assert.True(earlierIndex < laterIndex, "Expected vacating item to finalize before claimer.");
        }

        private static RenameItem _CreateDirectoryItem(string directoryPath, string folderName)
        {
            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: directoryPath,
                prefix: folderName,
                extension: string.Empty,
                attributes: FileAttributes.Directory
            );
            return new RenameItem(meta) { Status = RenameStatus.PreviewOk };
        }

        private static RenameItem _CreateItemFromExistingFile(string sourcePath)
        {
            var directoryPath = Path.GetDirectoryName(sourcePath)!;
            var prefix = Path.GetFileNameWithoutExtension(sourcePath);
            var extension = FileMeta.ExtensionWithoutDot(sourcePath);
            var attributes = File.Exists(sourcePath) ? FileAttributes.Normal : FileAttributes.Directory;
            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: directoryPath,
                prefix: prefix,
                extension: extension,
                attributes: attributes
            );
            return new RenameItem(meta) { Status = RenameStatus.PreviewOk };
        }

        private static void _RetargetPreview(RenameItem item, string directoryPath, string fileName)
        {
            item.Preview.DirectoryPath = directoryPath;
            item.Preview.Prefix = Path.GetFileNameWithoutExtension(fileName);
            item.Preview.Extension = FileMeta.ExtensionWithoutDot(fileName);
        }
    }
}
