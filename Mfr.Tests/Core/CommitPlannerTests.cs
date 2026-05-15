using Mfr.Core;
using Mfr.Models;
using Mfr.Utils;

namespace Mfr.Tests.Core
{
    /// <summary>
    /// Tests for <see cref="CommitPlanner.Build"/>.
    /// </summary>
    public sealed class CommitPlannerTests : IDisposable
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
        /// Verifies the planner returns an empty plan when no items have changes.
        /// </summary>
        [Fact]
        public void Build_skips_items_with_no_changes()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var item = _CreateFileItem(dir, "file.txt");
            // No preview retarget; preview equals original.

            var plan = CommitPlanner.Build([item]);

            Assert.Empty(plan.Steps);
            Assert.Empty(plan.UnresolvableCycleItems);
        }

        /// <summary>
        /// Verifies a simple non-overlapping rename produces a single FinalizeStep with the original source.
        /// </summary>
        [Fact]
        public void Build_simple_rename_emits_finalize_with_original_source()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var item = _CreateFileItem(dir, "old.txt");
            _RetargetPreview(item, dir, "new.txt");

            var plan = CommitPlanner.Build([item]);

            var step = Assert.Single(plan.Steps);
            var finalize = Assert.IsType<FinalizeStep>(step);
            Assert.Same(item, finalize.Item);
            Assert.Equal(item.Original.FullPath, finalize.ActualSourcePath);
        }

        /// <summary>
        /// Verifies a folder rename is ordered before any descendant file rename.
        /// </summary>
        [Fact]
        public void Build_orders_folder_rename_before_descendant_file_rename()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var oldFolder = dir.CombinePath("Album");
            var newFolder = dir.CombinePath("AlbumRenamed");

            var folderItem = _CreateDirectoryItem(dir, "Album");
            _RetargetPreview(folderItem, dir, "AlbumRenamed");

            var fileItem = _CreateFileItem(oldFolder, "track.mp3");
            // The descendant's preview is already rebased onto the renamed folder.
            fileItem.Preview.DirectoryPath = newFolder;
            fileItem.Preview.Prefix = "track-renamed";

            var plan = CommitPlanner.Build([fileItem, folderItem]);

            Assert.Equal(2, plan.Steps.Count);
            var first = Assert.IsType<FinalizeStep>(plan.Steps[0]);
            var second = Assert.IsType<FinalizeStep>(plan.Steps[1]);
            Assert.Same(folderItem, first.Item);
            Assert.Same(fileItem, second.Item);
            // Folder rename uses its own original path as source.
            Assert.Equal(oldFolder, first.ActualSourcePath);
            // File rename's actual source is rebased onto the new folder path.
            Assert.Equal(newFolder.CombinePath("track.mp3"), second.ActualSourcePath);
        }

        /// <summary>
        /// Verifies a path-shift chain (rename A -> B with separately renamed B -> C) commits B first.
        /// </summary>
        [Fact]
        public void Build_orders_path_shift_chain_so_vacating_item_runs_first()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var itemB = _CreateFileItem(dir, "b.txt");
            _RetargetPreview(itemB, dir, "c.txt");

            var itemA = _CreateFileItem(dir, "a.txt");
            _RetargetPreview(itemA, dir, "b.txt");

            var plan = CommitPlanner.Build([itemA, itemB]);

            Assert.Equal(2, plan.Steps.Count);
            var first = Assert.IsType<FinalizeStep>(plan.Steps[0]);
            var second = Assert.IsType<FinalizeStep>(plan.Steps[1]);
            Assert.Same(itemB, first.Item);
            Assert.Same(itemA, second.Item);
            Assert.Empty(plan.UnresolvableCycleItems);
        }

        /// <summary>
        /// Verifies a two-item swap (A&lt;-&gt;B) is resolved by stashing one and finalizing both.
        /// </summary>
        [Fact]
        public void Build_two_item_swap_uses_stash_and_finalize()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var itemA = _CreateFileItem(dir, "a.txt");
            var itemB = _CreateFileItem(dir, "b.txt");
            _RetargetPreview(itemA, dir, "b.txt");
            _RetargetPreview(itemB, dir, "a.txt");

            var plan = CommitPlanner.Build([itemA, itemB]);

            Assert.Empty(plan.UnresolvableCycleItems);
            Assert.Equal(3, plan.Steps.Count);

            var stash = Assert.IsType<StashStep>(plan.Steps[0]);
            var finalizeOther = Assert.IsType<FinalizeStep>(plan.Steps[1]);
            var finalizeStashed = Assert.IsType<FinalizeStep>(plan.Steps[2]);

            // The stashed item is finalized last from its stash temp path.
            Assert.Same(stash.Item, finalizeStashed.Item);
            Assert.Equal(stash.TempPath, finalizeStashed.ActualSourcePath);
            // The non-stashed swap counterpart finalizes from its own original path.
            Assert.NotSame(stash.Item, finalizeOther.Item);
            Assert.Equal(finalizeOther.Item.Original.FullPath, finalizeOther.ActualSourcePath);
        }

        /// <summary>
        /// Verifies a chained ancestor rename rebases descendant source paths innermost-first.
        /// </summary>
        [Fact]
        public void Build_chained_ancestor_renames_compose_for_descendant_source()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var oldOuter = dir.CombinePath("Outer");
            var newOuter = dir.CombinePath("OuterRenamed");
            var oldInner = oldOuter.CombinePath("Inner");
            var newInner = newOuter.CombinePath("InnerRenamed");

            var outerItem = _CreateDirectoryItem(dir, "Outer");
            _RetargetPreview(outerItem, dir, "OuterRenamed");

            var innerItem = _CreateDirectoryItem(oldOuter, "Inner");
            innerItem.Preview.DirectoryPath = newOuter;
            innerItem.Preview.Prefix = "InnerRenamed";

            var fileItem = _CreateFileItem(oldInner, "track.txt");
            fileItem.Preview.DirectoryPath = newInner;

            var plan = CommitPlanner.Build([fileItem, innerItem, outerItem]);

            Assert.Empty(plan.UnresolvableCycleItems);
            Assert.Equal(3, plan.Steps.Count);

            // Order should be: Outer first, then Inner, then file.
            var firstStep = Assert.IsType<FinalizeStep>(plan.Steps[0]);
            var secondStep = Assert.IsType<FinalizeStep>(plan.Steps[1]);
            var thirdStep = Assert.IsType<FinalizeStep>(plan.Steps[2]);
            Assert.Same(outerItem, firstStep.Item);
            Assert.Same(innerItem, secondStep.Item);
            Assert.Same(fileItem, thirdStep.Item);

            // Inner's actual source is rebased onto the new outer path (outer has already committed).
            Assert.Equal(newOuter.CombinePath("Inner"), secondStep.ActualSourcePath);
            // File's actual source is rebased through both ancestor renames since both commit before the file.
            Assert.Equal(newInner.CombinePath("track.txt"), thirdStep.ActualSourcePath);
        }

        private static RenameItem _CreateFileItem(string directoryPath, string fileName)
        {
            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: directoryPath,
                prefix: Path.GetFileNameWithoutExtension(fileName),
                extension: Path.GetExtension(fileName),
                attributes: FileAttributes.Normal);
            return new RenameItem(meta) { Status = RenameStatus.PreviewOk };
        }

        private static RenameItem _CreateDirectoryItem(string directoryPath, string folderName)
        {
            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: directoryPath,
                prefix: folderName,
                extension: string.Empty,
                attributes: FileAttributes.Directory);
            return new RenameItem(meta) { Status = RenameStatus.PreviewOk };
        }

        private static void _RetargetPreview(RenameItem item, string directoryPath, string fileName)
        {
            item.Preview.DirectoryPath = directoryPath;
            item.Preview.Prefix = Path.GetFileNameWithoutExtension(fileName);
            item.Preview.Extension = Path.GetExtension(fileName);
        }
    }
}
