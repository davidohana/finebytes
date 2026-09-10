using Mfr.Utils;

namespace Mfr.Tests.Engine
{
    /// <summary>
    /// Locks innermost-ancestor rewrite compose order and call-site match dialects shared by
    /// preview rebase and commit planning.
    /// </summary>
    public sealed class InnermostAncestorRewritesTests
    {
        /// <summary>
        /// Nested folder renames compose innermost-first for both the inclusive (directory) and
        /// strict-descendant (full path) dialects used by rebase vs planner.
        /// </summary>
        [Fact]
        public void Nested_ancestors_compose_innermost_first_for_both_dialects()
        {
            var outerOriginal = TestPaths.Absolute("A");
            var outerPreview = TestPaths.Absolute("A2");
            var innerOriginal = TestPaths.Absolute("A", "B");
            var innerPreview = TestPaths.Absolute("A2", "B2");

            var outerFolder = _CreateDirectoryItem(fullPath: outerOriginal, previewFullPath: outerPreview);
            var innerFolder = _CreateDirectoryItem(fullPath: innerOriginal, previewFullPath: innerPreview);
            var folderRenames = new List<RenameItem> { outerFolder, innerFolder };

            var previewDirectory = TestPaths.Absolute("A", "B", "sub");
            var originalFullPath = TestPaths.Absolute("A", "B", "sub", "track.txt");
            var skipItem = _CreateFileItem(
                fullPath: originalFullPath,
                previewDirectoryPath: previewDirectory,
                previewFileName: "song.txt"
            );

            var rebasedDirectory = InnermostAncestorRewrites.Apply(
                path: previewDirectory,
                folderRenames: folderRenames,
                skipItem: skipItem,
                matchesAncestor: _MatchesInclusiveDirectory
            );
            var plannedSource = InnermostAncestorRewrites.Apply(
                path: originalFullPath,
                folderRenames: folderRenames,
                skipItem: skipItem,
                matchesAncestor: _MatchesStrictDescendant
            );

            Assert.Equal(TestPaths.Absolute("A2", "B2", "sub"), rebasedDirectory);
            Assert.Equal(TestPaths.Absolute("A2", "B2", "sub", "track.txt"), plannedSource);
            Assert.Equal(rebasedDirectory, Path.GetDirectoryName(plannedSource));
        }

        /// <summary>
        /// Inclusive dialect rewrites when the path equals the renamed folder; strict does not.
        /// </summary>
        [Fact]
        public void Inclusive_matches_folder_itself_strict_descendant_does_not()
        {
            var folderOriginal = TestPaths.Absolute("Album");
            var folderPreview = TestPaths.Absolute("AlbumRenamed");
            var folderItem = _CreateDirectoryItem(fullPath: folderOriginal, previewFullPath: folderPreview);
            var folderRenames = new List<RenameItem> { folderItem };
            var skipItem = _CreateFileItem(
                fullPath: TestPaths.Absolute("Album", "track.txt"),
                previewDirectoryPath: folderOriginal,
                previewFileName: "song.txt"
            );

            var inclusive = InnermostAncestorRewrites.Apply(
                path: folderOriginal,
                folderRenames: folderRenames,
                skipItem: skipItem,
                matchesAncestor: _MatchesInclusiveDirectory
            );
            var strict = InnermostAncestorRewrites.Apply(
                path: folderOriginal,
                folderRenames: folderRenames,
                skipItem: skipItem,
                matchesAncestor: _MatchesStrictDescendant
            );

            Assert.Equal(folderPreview, inclusive);
            Assert.Equal(folderOriginal, strict);
        }

        /// <summary>
        /// End-to-end: rebaser directory and planner ActualSourcePath stay on the same composed prefix.
        /// </summary>
        [Fact]
        public void Rebase_and_planner_agree_on_composed_prefix_for_nested_file()
        {
            var outerOriginal = TestPaths.Absolute("Outer");
            var outerPreview = TestPaths.Absolute("OuterRenamed");
            var innerOriginal = TestPaths.Absolute("Outer", "Inner");
            var innerPreview = TestPaths.Absolute("OuterRenamed", "InnerRenamed");
            var fileOriginal = TestPaths.Absolute("Outer", "Inner", "track.txt");

            var outerFolder = _CreateDirectoryItem(fullPath: outerOriginal, previewFullPath: outerPreview);
            var innerFolder = _CreateDirectoryItem(fullPath: innerOriginal, previewFullPath: innerPreview);
            var fileItem = _CreateFileItem(
                fullPath: fileOriginal,
                previewDirectoryPath: innerOriginal,
                previewFileName: "track.txt"
            );

            var items = new List<RenameItem> { fileItem, innerFolder, outerFolder };
            RenamePreviewFolderRebaser.RebaseDescendants(items);
            var plan = CommitPlanner.Build(items);

            var finalizeFile = Assert.Single(
                plan.Steps.OfType<FinalizeStep>(),
                step => ReferenceEquals(step.Item, fileItem)
            );
            Assert.Equal(innerPreview, fileItem.Preview.DirectoryPath);
            Assert.Equal(
                TestPaths.Absolute("OuterRenamed", "InnerRenamed", "track.txt"),
                finalizeFile.ActualSourcePath
            );
            Assert.Equal(fileItem.Preview.DirectoryPath, Path.GetDirectoryName(finalizeFile.ActualSourcePath));
        }

        private static bool _MatchesInclusiveDirectory(string path, string ancestorOriginalPath)
        {
            return PathRelations.IsSamePath(first: path, second: ancestorOriginalPath)
                || PathRelations.IsDescendantOf(candidate: path, ancestor: ancestorOriginalPath);
        }

        private static bool _MatchesStrictDescendant(string path, string ancestorOriginalPath)
        {
            return PathRelations.IsDescendantOf(candidate: path, ancestor: ancestorOriginalPath);
        }

        private static RenameItem _CreateDirectoryItem(string fullPath, string previewFullPath)
        {
            var item = _CreateItem(fullPath: fullPath, attributes: FileAttributes.Directory);
            item.Preview.DirectoryPath = Path.GetDirectoryName(previewFullPath)!;
            item.Preview.Prefix = Path.GetFileNameWithoutExtension(previewFullPath);
            item.Preview.Extension = FileMeta.ExtensionWithoutDot(previewFullPath);
            item.Status = RenameStatus.PreviewOk;
            return item;
        }

        private static RenameItem _CreateFileItem(string fullPath, string previewDirectoryPath, string previewFileName)
        {
            var item = _CreateItem(fullPath: fullPath, attributes: FileAttributes.Normal);
            item.Preview.DirectoryPath = previewDirectoryPath;
            item.Preview.Prefix = Path.GetFileNameWithoutExtension(previewFileName);
            item.Preview.Extension = FileMeta.ExtensionWithoutDot(previewFileName);
            item.Status = RenameStatus.PreviewOk;
            return item;
        }

        private static RenameItem _CreateItem(string fullPath, FileAttributes attributes)
        {
            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: Path.GetDirectoryName(fullPath)!,
                prefix: Path.GetFileNameWithoutExtension(fullPath),
                extension: FileMeta.ExtensionWithoutDot(fullPath),
                attributes: attributes
            );
            return new RenameItem(meta);
        }
    }
}
