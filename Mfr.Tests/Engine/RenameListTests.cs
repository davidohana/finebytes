using Mfr.Filters.Audio;
using Mfr.Filters.Formatting;
using Mfr.Models.Tags;
using Mfr.Utils;
using FormatterFilter = Mfr.Filters.Formatting.FormatterFilter;

namespace Mfr.Tests.Engine
{
    /// <summary>
    /// Tests source and file resolution behavior in <see cref="RenameList"/>.
    /// </summary>
    public class RenameListTests : IDisposable
    {
        private readonly string _tempRoot;

        /// <summary>
        /// Initializes a new test instance with an isolated temporary directory under the current workspace.
        /// </summary>
        public RenameListTests()
        {
            _tempRoot = Directory
                .GetCurrentDirectory()
                .CombinePath("mfr_renamelist_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempRoot);
        }

        /// <summary>
        /// Removes files and folders created by this test class.
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (!Directory.Exists(_tempRoot))
                {
                    return;
                }

                foreach (var file in Directory.EnumerateFiles(_tempRoot, "*", SearchOption.AllDirectories))
                {
                    var attrs = File.GetAttributes(file);
                    if (attrs.HasFlag(FileAttributes.Hidden))
                    {
                        File.SetAttributes(file, attrs & ~FileAttributes.Hidden);
                    }
                }

                Directory.Delete(_tempRoot, recursive: true);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        [Fact]
        /// <summary>
        /// Verifies that mixed source types are expanded in source order and deduplicated.
        /// </summary>
        public void AddSources_Expands_Mixed_Sources_And_Preserves_Source_Order()
        {
            var (alphaPath, betaPath, gammaPath) = TestHelpers.CreateFiles(
                _tempRoot,
                "alpha.txt",
                "beta.log",
                "gamma.txt"
            );

            var sources = new[] { betaPath, _tempRoot.CombinePath("*.txt") };

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources(sources: sources, includeFiles: true, includeFolders: false);
            var entries = renameList.RenameItems;

            Assert.Equal(3, entries.Count);
            Assert.Equal([betaPath, alphaPath, gammaPath], entries.Select(e => e.Original.FullPath));
            Assert.Equal([0, 1, 2], entries.Select(e => e.Original.RenameListIndex));
            Assert.Equal([0, 1, 2], entries.Select(e => e.Original.InFolderIndex));
            Assert.Equal(["beta", "alpha", "gamma"], entries.Select(e => e.Original.Prefix));
            Assert.Equal([".log", ".txt", ".txt"], entries.Select(e => e.Original.Extension));
            Assert.Equal([_tempRoot, _tempRoot, _tempRoot], entries.Select(e => e.Original.DirectoryPath));
        }

        [Fact]
        /// <summary>
        /// Verifies that duplicate source additions are allowed, while resolved items stay distinct.
        /// </summary>
        public void AddSources_Allows_Duplicate_Sources_But_ResolvedItems_Are_Deduplicated()
        {
            var source = TestHelpers.CreateFile(_tempRoot, "alpha.txt");

            var renameList = new RenameList(includeHidden: true);
            var beforeCount = renameList.RenameItems.Count;
            renameList.AddSources([source]);
            Assert.Equal(1, renameList.RenameItems.Count - beforeCount);
            beforeCount = renameList.RenameItems.Count;
            renameList.AddSources([source]);
            Assert.Equal(0, renameList.RenameItems.Count - beforeCount);

            Assert.Single(renameList.RenameItems);
            Assert.Equal(source, renameList.RenameItems[0].Original.FullPath);
        }

        [Fact]
        /// <summary>
        /// Verifies that removing an item reindexes remaining entries.
        /// </summary>
        public void Remove_Reindexes_Remaining_Items()
        {
            var (alphaPath, betaPath, gammaPath) = TestHelpers.CreateFiles(
                _tempRoot,
                "alpha.txt",
                "beta.log",
                "gamma.txt"
            );

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([alphaPath, betaPath, gammaPath]);
            var betaItem = renameList.RenameItems[1];
            Assert.Equal(1, renameList.Remove(betaItem));

            var entries = renameList.RenameItems;
            Assert.Equal(2, entries.Count);
            Assert.Equal([alphaPath, gammaPath], entries.Select(entry => entry.Original.FullPath));
            Assert.Equal([0, 1], entries.Select(entry => entry.Original.RenameListIndex));
            Assert.Equal([0, 1], entries.Select(entry => entry.Original.InFolderIndex));
        }

        [Fact]
        /// <summary>
        /// Verifies AddSources insertAtIndex inserts before that row and reindexes.
        /// </summary>
        public void AddSources_InsertAt_Inserts_Before_Index_And_Reindexes()
        {
            var (alphaPath, betaPath, gammaPath) = TestHelpers.CreateFiles(
                _tempRoot,
                "alpha.txt",
                "beta.log",
                "gamma.txt"
            );
            var deltaPath = TestHelpers.CreateFile(_tempRoot, "delta.txt");

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([alphaPath, betaPath, gammaPath]);
            renameList.AddSources([deltaPath], insertAtIndex: 1);

            var entries = renameList.RenameItems;
            Assert.Equal([alphaPath, deltaPath, betaPath, gammaPath], entries.Select(entry => entry.Original.FullPath));
            Assert.Equal([0, 1, 2, 3], entries.Select(entry => entry.Original.RenameListIndex));
            Assert.Equal([0, 1, 2, 3], entries.Select(entry => entry.Original.InFolderIndex));
        }

        [Fact]
        /// <summary>
        /// Verifies AddSources with insertAtIndex 0 inserts at the start.
        /// </summary>
        public void AddSources_InsertAt_Zero_Inserts_At_Start()
        {
            var (alphaPath, betaPath) = TestHelpers.CreateFiles(_tempRoot, "alpha.txt", "beta.txt");

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([alphaPath]);
            renameList.AddSources([betaPath], insertAtIndex: 0);

            Assert.Equal([betaPath, alphaPath], renameList.RenameItems.Select(entry => entry.Original.FullPath));
            Assert.Equal([0, 1], renameList.RenameItems.Select(entry => entry.Original.RenameListIndex));
        }

        [Fact]
        /// <summary>
        /// Verifies MoveUp shifts a contiguous selection as a block and reindexes.
        /// </summary>
        public void MoveUp_Moves_Contiguous_Selection_As_Block()
        {
            var (alphaPath, betaPath, gammaPath) = TestHelpers.CreateFiles(
                _tempRoot,
                "alpha.txt",
                "beta.log",
                "gamma.txt"
            );

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([alphaPath, betaPath, gammaPath]);
            var items = renameList.RenameItems;

            Assert.True(renameList.MoveSelected([items[1], items[2]], offset: -1));

            Assert.Equal([betaPath, gammaPath, alphaPath], items.Select(entry => entry.Original.FullPath));
            Assert.Equal([0, 1, 2], items.Select(entry => entry.Original.RenameListIndex));
        }

        [Fact]
        /// <summary>
        /// Verifies MoveUp is a no-op when the selection already starts at the top.
        /// </summary>
        public void MoveUp_At_Top_Is_NoOp()
        {
            var (alphaPath, betaPath) = TestHelpers.CreateFiles(_tempRoot, "alpha.txt", "beta.txt");

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([alphaPath, betaPath]);
            var items = renameList.RenameItems;

            Assert.False(renameList.MoveSelected([items[0]], offset: -1));
            Assert.Equal([alphaPath, betaPath], items.Select(entry => entry.Original.FullPath));
        }

        [Fact]
        /// <summary>
        /// Verifies MoveDown shifts a contiguous selection as a block and reindexes.
        /// </summary>
        public void MoveDown_Moves_Contiguous_Selection_As_Block()
        {
            var (alphaPath, betaPath, gammaPath) = TestHelpers.CreateFiles(
                _tempRoot,
                "alpha.txt",
                "beta.log",
                "gamma.txt"
            );

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([alphaPath, betaPath, gammaPath]);
            var items = renameList.RenameItems;

            Assert.True(renameList.MoveSelected([items[0], items[1]], offset: 1));

            Assert.Equal([gammaPath, alphaPath, betaPath], items.Select(entry => entry.Original.FullPath));
            Assert.Equal([0, 1, 2], items.Select(entry => entry.Original.RenameListIndex));
        }

        [Fact]
        /// <summary>
        /// Verifies non-contiguous MoveUp only advances items that have a free slot above.
        /// </summary>
        public void MoveUp_NonContiguous_Moves_Items_Independently()
        {
            var (alphaPath, betaPath, gammaPath) = TestHelpers.CreateFiles(
                _tempRoot,
                "alpha.txt",
                "beta.log",
                "gamma.txt"
            );

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([alphaPath, betaPath, gammaPath]);
            var items = renameList.RenameItems;

            Assert.True(renameList.MoveSelected([items[0], items[2]], offset: -1));

            Assert.Equal([alphaPath, gammaPath, betaPath], items.Select(entry => entry.Original.FullPath));
        }

        [Fact]
        /// <summary>
        /// Verifies MoveSelectedBefore inserts the selection before the target and reindexes.
        /// </summary>
        public void MoveSelectedBefore_Reorders_Block_Before_Target()
        {
            var (alphaPath, betaPath, gammaPath) = TestHelpers.CreateFiles(
                _tempRoot,
                "alpha.txt",
                "beta.log",
                "gamma.txt"
            );
            var deltaPath = TestHelpers.CreateFile(_tempRoot, "delta.txt");

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([alphaPath, betaPath, gammaPath, deltaPath]);
            var items = renameList.RenameItems;

            Assert.True(renameList.MoveSelectedBefore([items[0], items[1]], beforeItem: items[3]));

            Assert.Equal([gammaPath, alphaPath, betaPath, deltaPath], items.Select(entry => entry.Original.FullPath));
            Assert.Equal([0, 1, 2, 3], items.Select(entry => entry.Original.RenameListIndex));
        }

        [Fact]
        /// <summary>
        /// Verifies MoveSelectedBefore with a null target appends the selection.
        /// </summary>
        public void MoveSelectedBefore_Null_Target_Appends()
        {
            var (alphaPath, betaPath, gammaPath) = TestHelpers.CreateFiles(
                _tempRoot,
                "alpha.txt",
                "beta.log",
                "gamma.txt"
            );

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([alphaPath, betaPath, gammaPath]);
            var items = renameList.RenameItems;

            Assert.True(renameList.MoveSelectedBefore([items[0]], beforeItem: null));

            Assert.Equal([betaPath, gammaPath, alphaPath], items.Select(entry => entry.Original.FullPath));
        }

        [Fact]
        /// <summary>
        /// Verifies MoveSelectedBefore is a no-op when the drop target is in the selection.
        /// </summary>
        public void MoveSelectedBefore_Target_In_Selection_Is_NoOp()
        {
            var (alphaPath, betaPath, gammaPath) = TestHelpers.CreateFiles(
                _tempRoot,
                "alpha.txt",
                "beta.log",
                "gamma.txt"
            );

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([alphaPath, betaPath, gammaPath]);
            var items = renameList.RenameItems;

            Assert.False(renameList.MoveSelectedBefore([items[0], items[1]], beforeItem: items[1]));
            Assert.Equal([alphaPath, betaPath, gammaPath], items.Select(entry => entry.Original.FullPath));
        }

        /// <summary>
        /// Verifies Sort by File/Folder then Full Path orders files before folders (ascending labels).
        /// </summary>
        [Fact]
        public void Sort_FileFolder_Then_FullPath_Orders_Files_Before_Folders()
        {
            var filePath = TestHelpers.CreateFile(_tempRoot, "zeta.txt");
            var folderPath = Path.Combine(_tempRoot, "alpha-folder");
            Directory.CreateDirectory(folderPath);
            var earlyFile = TestHelpers.CreateFile(_tempRoot, "alpha.txt");

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([filePath, folderPath, earlyFile], includeFolders: true, includeFiles: true);

            Assert.True(
                renameList.Sort([
                    new RenameListSortKey(RenameListSortColumn.FileFolder),
                    new RenameListSortKey(RenameListSortColumn.FullPath),
                ])
            );

            Assert.Equal(
                [earlyFile, filePath, folderPath],
                renameList.RenameItems.Select(item => item.Original.FullPath)
            );
        }

        /// <summary>
        /// Verifies Sort descending by Full File Name reverses name order.
        /// </summary>
        [Fact]
        public void Sort_FullFileName_Descending()
        {
            var (alphaPath, betaPath, gammaPath) = TestHelpers.CreateFiles(
                _tempRoot,
                "alpha.txt",
                "beta.log",
                "gamma.txt"
            );

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([alphaPath, betaPath, gammaPath]);

            Assert.True(renameList.Sort([new RenameListSortKey(RenameListSortColumn.FullFileName, Descending: true)]));

            Assert.Equal(
                [gammaPath, betaPath, alphaPath],
                renameList.RenameItems.Select(item => item.Original.FullPath)
            );
        }

        [Fact]
        /// <summary>
        /// Verifies that removing by item reference drops the item from the list.
        /// </summary>
        public void Remove_ByItem_Removes_From_List()
        {
            var path = TestHelpers.CreateFile(_tempRoot, "alpha.txt");

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([path]);
            var item = renameList.RenameItems[0];

            Assert.Equal(1, renameList.Remove(item));
            Assert.Empty(renameList.RenameItems);
        }

        [Fact]
        /// <summary>
        /// Verifies that removing multiple items reindexes list and per-folder indices.
        /// </summary>
        public void Remove_MultipleItems_Reindexes_List_And_InFolderIndices()
        {
            var folderAPath = Directory.CreateDirectory(_tempRoot.CombinePath("A")).FullName;
            var folderBPath = Directory.CreateDirectory(_tempRoot.CombinePath("B")).FullName;
            var aFirstPath = TestHelpers.CreateFile(folderAPath, "a1.txt");
            var aSecondPath = TestHelpers.CreateFile(folderAPath, "a2.txt");
            var bFirstPath = TestHelpers.CreateFile(folderBPath, "b1.txt");

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([aFirstPath, aSecondPath, bFirstPath]);
            var items = renameList.RenameItems;

            Assert.Equal(2, renameList.Remove([items[0], items[2]]));

            var entry = Assert.Single(renameList.RenameItems);
            Assert.Equal(aSecondPath, entry.Original.FullPath);
            Assert.Equal(0, entry.Original.RenameListIndex);
            Assert.Equal(0, entry.Original.InFolderIndex);
        }

        [Fact]
        /// <summary>
        /// Verifies that removing an item not in the list is a no-op.
        /// </summary>
        public void Remove_ItemNotInList_Returns_Zero()
        {
            var path = TestHelpers.CreateFile(_tempRoot, "alpha.txt");

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([path]);
            var item = renameList.RenameItems[0];
            renameList.Clear();

            Assert.Equal(0, renameList.Remove(item));
        }

        [Fact]
        /// <summary>
        /// Verifies that clear empties the list and resets deduplication so paths can be added again.
        /// </summary>
        public void Clear_Empties_List_And_Allows_Readd()
        {
            var (alphaPath, betaPath) = TestHelpers.CreateFiles(_tempRoot, "alpha.txt", "beta.txt");

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([alphaPath]);
            renameList.AddSources([betaPath]);
            renameList.Clear();

            Assert.Empty(renameList.RenameItems);
            var beforeCount = renameList.RenameItems.Count;
            renameList.AddSources([alphaPath]);
            Assert.Equal(1, renameList.RenameItems.Count - beforeCount);
            beforeCount = renameList.RenameItems.Count;
            renameList.AddSources([betaPath]);
            Assert.Equal(1, renameList.RenameItems.Count - beforeCount);
            Assert.Equal(2, renameList.RenameItems.Count);
            Assert.Equal([0, 1], renameList.RenameItems.Select(entry => entry.Original.RenameListIndex));
            Assert.Equal([0, 1], renameList.RenameItems.Select(entry => entry.Original.InFolderIndex));
        }

        [Fact]
        /// <summary>
        /// Verifies that a removed path can be added again without dedupe blocking it.
        /// </summary>
        public void Remove_Allows_Readding_Same_Path()
        {
            var path = TestHelpers.CreateFile(_tempRoot, "alpha.txt");

            var renameList = new RenameList(includeHidden: true);
            var beforeCount = renameList.RenameItems.Count;
            renameList.AddSources([path]);
            Assert.Equal(1, renameList.RenameItems.Count - beforeCount);
            var item = renameList.RenameItems[0];
            Assert.Equal(1, renameList.Remove(item));
            Assert.Empty(renameList.RenameItems);
            beforeCount = renameList.RenameItems.Count;
            renameList.AddSources([path]);
            Assert.Equal(1, renameList.RenameItems.Count - beforeCount);

            var entry = Assert.Single(renameList.RenameItems);
            Assert.Equal(path, entry.Original.FullPath);
            Assert.Equal(0, entry.Original.RenameListIndex);
            Assert.Equal(0, entry.Original.InFolderIndex);
        }

        [Fact]
        /// <summary>
        /// Verifies that an explicit root path is skipped without aborting the add batch.
        /// </summary>
        public void AddSources_SingleRootPath_IsSkipped()
        {
            var rootPath = Path.GetPathRoot(Directory.GetCurrentDirectory())!;
            var renameList = new RenameList(includeHidden: false);

            var summary = renameList.AddSources([rootPath]);

            Assert.Equal(1, summary.SkippedSourceCount);
            Assert.Empty(renameList.RenameItems);
        }

        [Fact]
        /// <summary>
        /// Verifies that root paths in a mixed batch are skipped while other sources still add.
        /// </summary>
        public void AddSources_Skips_Root_Path_In_Mixed_Batch()
        {
            var filePath = TestHelpers.CreateFile(_tempRoot, "alpha.txt");
            var rootPath = Path.GetPathRoot(Directory.GetCurrentDirectory())!;
            var renameList = new RenameList(includeHidden: false);

            var summary = renameList.AddSources([filePath, rootPath]);

            Assert.Equal(1, summary.SkippedSourceCount);
            Assert.Single(renameList.RenameItems);
            Assert.Equal(filePath, renameList.RenameItems[0].Original.FullPath);
        }

        [Fact]
        /// <summary>
        /// Verifies that hidden files are skipped unless hidden inclusion is enabled.
        /// </summary>
        public void AddSources_Filters_Hidden_When_Disabled()
        {
            var hiddenFileName = OperatingSystem.IsWindows() ? "hidden.txt" : ".hidden.txt";
            var (visiblePath, hiddenPath) = TestHelpers.CreateFiles(_tempRoot, "visible.txt", hiddenFileName);
            if (OperatingSystem.IsWindows())
            {
                var hiddenAttrs = File.GetAttributes(hiddenPath);
                File.SetAttributes(hiddenPath, hiddenAttrs | FileAttributes.Hidden);
            }

            var excludeHiddenList = new RenameList(includeHidden: false);
            excludeHiddenList.AddSources([hiddenPath]);
            excludeHiddenList.AddSources([visiblePath]);
            var excludedHidden = excludeHiddenList.RenameItems.ToList();

            var includeHiddenList = new RenameList(includeHidden: true);
            includeHiddenList.AddSources([hiddenPath]);
            includeHiddenList.AddSources([visiblePath]);
            var includedHidden = includeHiddenList.RenameItems.ToList();

            Assert.Single(excludedHidden);
            Assert.Equal(visiblePath, excludedHidden[0].Original.FullPath);
            Assert.Equal(0, excludedHidden[0].Original.RenameListIndex);
            Assert.Equal(0, excludedHidden[0].Original.InFolderIndex);

            Assert.Equal(2, includedHidden.Count);
            Assert.Equal([hiddenPath, visiblePath], includedHidden.Select(x => x.Original.FullPath));
            Assert.Equal([0, 1], includedHidden.Select(x => x.Original.RenameListIndex));
            Assert.Equal([0, 1], includedHidden.Select(x => x.Original.InFolderIndex));
        }

        [Fact]
        /// <summary>
        /// Verifies that file entries are excluded when file inclusion is disabled.
        /// </summary>
        public void AddSources_Excludes_Files_When_File_Inclusion_Is_Disabled()
        {
            var filePath = TestHelpers.CreateFile(_tempRoot, "alpha.txt");
            var folderPath = Directory.CreateDirectory(_tempRoot.CombinePath("Album")).FullName;

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources(sources: [filePath, folderPath], includeFiles: false, includeFolders: true);

            var entry = Assert.Single(renameList.RenameItems);
            Assert.Equal(folderPath, entry.Original.FullPath);
        }

        [Fact]
        /// <summary>
        /// Verifies that a folder whose name contains a dot is not split into pseudo prefix and extension like a file.
        /// </summary>
        public void AddSources_Folder_WithDotInName_UsesFullSegmentAsPrefix()
        {
            var folderPath = Directory.CreateDirectory(_tempRoot.CombinePath("release.v2")).FullName;

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([folderPath], includeFiles: false, includeFolders: true);

            var entry = Assert.Single(renameList.RenameItems);
            Assert.Equal(folderPath, entry.Original.FullPath);
            Assert.Equal("release.v2", entry.Original.Prefix);
            Assert.Empty(entry.Original.Extension);
            Assert.Equal(_tempRoot, entry.Original.DirectoryPath);
        }

        [Fact]
        /// <summary>
        /// Verifies that folder entries are excluded when folder inclusion is disabled.
        /// </summary>
        public void AddSources_Excludes_Folders_When_Folder_Inclusion_Is_Disabled()
        {
            var filePath = TestHelpers.CreateFile(_tempRoot, "alpha.txt");
            var folderPath = Directory.CreateDirectory(_tempRoot.CombinePath("Album")).FullName;
            var folderFilePath = TestHelpers.CreateFile(folderPath, "inside.txt");
            TestHelpers.CreateFile(folderPath.CombinePath("Sub"), "nested.txt");

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources(sources: [filePath, folderPath], includeFiles: true, includeFolders: false);

            Assert.Equal([filePath, folderFilePath], renameList.RenameItems.Select(entry => entry.Original.FullPath));
        }

        [Fact]
        /// <summary>
        /// Verifies that a directory source expands to top-level files only when folder inclusion is disabled.
        /// </summary>
        public void AddSources_DirectorySource_WithFoldersDisabled_AddsTopLevelFilesOnly()
        {
            var folderPath = Directory.CreateDirectory(_tempRoot.CombinePath("Album")).FullName;
            var topLevelFirstPath = TestHelpers.CreateFile(folderPath, "first.txt");
            var topLevelSecondPath = TestHelpers.CreateFile(folderPath, "second.log");
            var nestedFilePath = TestHelpers.CreateFile(folderPath.CombinePath("Sub"), "nested.txt");

            var renameList = new RenameList(includeHidden: true);
            var beforeCount = renameList.RenameItems.Count;
            renameList.AddSources(sources: [folderPath], includeFiles: true, includeFolders: false);
            var addedCount = renameList.RenameItems.Count - beforeCount;

            Assert.Equal(2, addedCount);
            Assert.Equal(
                [topLevelFirstPath, topLevelSecondPath],
                renameList.RenameItems.Select(entry => entry.Original.FullPath)
            );
            Assert.DoesNotContain(nestedFilePath, renameList.RenameItems.Select(entry => entry.Original.FullPath));
        }

        [Fact]
        /// <summary>
        /// Verifies that a directory source expands recursively when folder inclusion is disabled and recursive add is enabled.
        /// </summary>
        public void AddSources_DirectorySource_WithFoldersDisabled_AndRecursiveEnabled_AddsNestedFiles()
        {
            var folderPath = Directory.CreateDirectory(_tempRoot.CombinePath("Album")).FullName;
            var topLevelPath = TestHelpers.CreateFile(folderPath, "first.txt");
            var nestedPath = TestHelpers.CreateFile(folderPath.CombinePath("Sub"), "nested.txt");
            var deeperPath = TestHelpers.CreateFile(folderPath.CombinePath("Sub", "Deep"), "deep.log");

            var renameList = new RenameList(includeHidden: true);
            var beforeCount = renameList.RenameItems.Count;
            renameList.AddSources(
                sources: [folderPath],
                includeFiles: true,
                includeFolders: false,
                includeSubdirs: true
            );
            var addedCount = renameList.RenameItems.Count - beforeCount;

            Assert.Equal(3, addedCount);
            Assert.Equal(
                [topLevelPath, nestedPath, deeperPath],
                renameList.RenameItems.Select(entry => entry.Original.FullPath)
            );
        }

        [Fact]
        /// <summary>
        /// Verifies that a directory source with files and folders expands one level and honors masks.
        /// </summary>
        public void AddSources_DirectorySource_WithFilesAndFolders_OneLevel_AddsMatchingImmediateEntries()
        {
            var folderPath = Directory.CreateDirectory(_tempRoot.CombinePath("Album")).FullName;
            TestHelpers.CreateFile(folderPath, "keep.mp3");
            var skipFile = TestHelpers.CreateFile(folderPath, "skip.mp3");
            var childFolder = Directory.CreateDirectory(folderPath.CombinePath("Disc1")).FullName;
            TestHelpers.CreateFile(childFolder, "nested.mp3");

            var renameList = new RenameList(includeHidden: true);
            var beforeCount = renameList.RenameItems.Count;
            renameList.AddSources(
                sources: [folderPath.CombinePath("*.mp3")],
                includeFiles: true,
                includeFolders: true,
                includeSubdirs: false,
                excludeMasks: ["keep.*"]
            );
            var addedCount = renameList.RenameItems.Count - beforeCount;

            Assert.Equal(2, addedCount);
            Assert.Equal([folderPath, skipFile], renameList.RenameItems.Select(entry => entry.Original.FullPath));
        }

        [Fact]
        /// <summary>
        /// Verifies that a directory source with files, folders, and recursion adds nested folder rows.
        /// </summary>
        public void AddSources_DirectorySource_WithFilesFoldersAndRecursion_AddsNestedFolders()
        {
            var folderPath = Directory.CreateDirectory(_tempRoot.CombinePath("Album")).FullName;
            var topFile = TestHelpers.CreateFile(folderPath, "readme.txt");
            var childFolder = Directory.CreateDirectory(folderPath.CombinePath("Disc1")).FullName;
            var nestedFile = TestHelpers.CreateFile(childFolder, "track.mp3");

            var renameList = new RenameList(includeHidden: true);
            var beforeCount = renameList.RenameItems.Count;
            renameList.AddSources(
                sources: [folderPath],
                includeFiles: true,
                includeFolders: true,
                includeSubdirs: true
            );
            var addedCount = renameList.RenameItems.Count - beforeCount;

            Assert.Equal(4, addedCount);
            Assert.Equal(folderPath, renameList.RenameItems[0].Original.FullPath);
            Assert.Equal(
                new[] { folderPath, topFile, childFolder, nestedFile }.OrderBy(
                    static path => path,
                    StringComparer.OrdinalIgnoreCase
                ),
                renameList
                    .RenameItems.Select(entry => entry.Original.FullPath)
                    .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            );
        }

        [Fact]
        /// <summary>
        /// Verifies that a last-segment mask with folders disabled resolves matching top-level files only.
        /// </summary>
        public void AddSources_DirectoryMask_FoldersDisabled_AddsTopLevelMatchesOnly()
        {
            var (topLevelMatch, _) = TestHelpers.CreateFiles(_tempRoot, "top.txt", "nested/nested.txt");

            var renameList = new RenameList(includeHidden: true);
            var beforeCount = renameList.RenameItems.Count;
            renameList.AddSources(sources: [_tempRoot.CombinePath("*.txt")], includeFiles: true, includeFolders: false);
            var addedCount = renameList.RenameItems.Count - beforeCount;

            Assert.Equal(1, addedCount);
            var entry = Assert.Single(renameList.RenameItems);
            Assert.Equal(topLevelMatch, entry.Original.FullPath);
        }

        [Fact]
        /// <summary>
        /// Verifies that a last-segment mask and an exact-file source deduplicate to a single resolved item.
        /// </summary>
        public void AddSources_DirectoryMaskAndExactFile_Deduplicates_ResolvedItem()
        {
            var alphaPath = TestHelpers.CreateFile(_tempRoot, "alpha.txt");

            var renameList = new RenameList(includeHidden: true);
            var beforeCount = renameList.RenameItems.Count;
            renameList.AddSources(sources: [_tempRoot.CombinePath("*.txt")], includeFiles: true, includeFolders: false);
            Assert.Equal(1, renameList.RenameItems.Count - beforeCount);
            beforeCount = renameList.RenameItems.Count;
            renameList.AddSources([alphaPath]);
            Assert.Equal(0, renameList.RenameItems.Count - beforeCount);

            var entry = Assert.Single(renameList.RenameItems);
            Assert.Equal(alphaPath, entry.Original.FullPath);
        }

        [Fact]
        /// <summary>
        /// Verifies that a last-segment mask with recursion resolves matching files from nested folders.
        /// </summary>
        public void AddSources_DirectoryMask_Recursive_AddsNestedMatches()
        {
            var (topLevelMatch, nestedMatch, deeperMatch) = TestHelpers.CreateFiles(
                _tempRoot,
                "top.txt",
                "nested/nested.txt",
                "nested/deeper/deeper.txt"
            );

            var renameList = new RenameList(includeHidden: true);
            var beforeCount = renameList.RenameItems.Count;
            renameList.AddSources(
                sources: [_tempRoot.CombinePath("*.txt")],
                includeFiles: true,
                includeFolders: false,
                includeSubdirs: true
            );
            var addedCount = renameList.RenameItems.Count - beforeCount;

            Assert.Equal(3, addedCount);
            Assert.Equal(
                [topLevelMatch, nestedMatch, deeperMatch],
                renameList.RenameItems.Select(entry => entry.Original.FullPath)
            );
        }

        [Fact]
        /// <summary>
        /// Verifies add progress reports scanned and added counts for a directory walk.
        /// </summary>
        public void AddSources_Reports_Progress_With_Scanned_And_Added()
        {
            TestHelpers.CreateFiles(_tempRoot, "a.txt", "b.txt", "nested/c.txt");

            var reports = new List<RenameListAddProgress>();
            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources(
                sources: [_tempRoot.CombinePath("*.txt")],
                includeFiles: true,
                includeFolders: false,
                includeSubdirs: true,
                progress: new Progress<RenameListAddProgress>(reports.Add)
            );

            Assert.Equal(3, renameList.RenameItems.Count);
            Assert.NotEmpty(reports);
            var last = reports[^1];
            Assert.True(last.ScannedCount >= 3);
            Assert.Equal(3, last.AddedCount);
            Assert.False(string.IsNullOrWhiteSpace(last.LastPath));
        }

        [Fact]
        /// <summary>
        /// Verifies an already-canceled token adds nothing and does not throw.
        /// </summary>
        public void AddSources_PreCanceled_Adds_Nothing()
        {
            TestHelpers.CreateFiles(_tempRoot, "a.txt", "b.txt");

            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources(
                sources: [_tempRoot.CombinePath("*.txt")],
                includeFiles: true,
                includeFolders: false,
                cancellationToken: cts.Token
            );

            Assert.Empty(renameList.RenameItems);
        }

        [Fact]
        /// <summary>
        /// Verifies canceling mid-walk returns without throwing and does not keep a partial batch.
        /// </summary>
        public void AddSources_Cancel_Stops_Without_Throwing()
        {
            var keepPath = TestHelpers.CreateFile(_tempRoot, "keep.txt");
            for (var i = 0; i < 500; i++)
            {
                var nested = _tempRoot.CombinePath($"d{i:D3}");
                Directory.CreateDirectory(nested);
                File.WriteAllText(nested.CombinePath($"f{i:D3}.txt"), "x");
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(5));
            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([keepPath]);

            renameList.AddSources(
                sources: [_tempRoot.CombinePath("*.txt")],
                includeFiles: true,
                includeFolders: false,
                includeSubdirs: true,
                cancellationToken: cts.Token
            );

            Assert.True(cts.IsCancellationRequested);
            Assert.Equal([keepPath], renameList.RenameItems.Select(entry => entry.Original.FullPath));
        }

        [Fact]
        /// <summary>
        /// Verifies ingest does not populate audio-tag overlays until a preview uses formatter audio tokens.
        /// </summary>
        public void AddSources_FileEntry_Does_Not_Load_AudioTags_Before_Format_Preview()
        {
            var path = Path.Combine(_tempRoot, $"tagged_{Guid.NewGuid():N}.wav");
            TaggedMinimalWav.WriteTagged(path, title: "ListIngestTitle", album: "ListIngestAlbum");

            var renameList = new RenameList(includeHidden: true);
            var beforeCount = renameList.RenameItems.Count;
            renameList.AddSources([path]);
            Assert.Equal(1, renameList.RenameItems.Count - beforeCount);

            var item = Assert.Single(renameList.RenameItems);
            Assert.Equal(new AudioTagOverlay(), item.Original.AudioTagOverlay);
        }

        [Fact]
        /// <summary>
        /// Verifies preview with audio formatter tokens fills tag overlays once from TagLib-backed disk read.
        /// </summary>
        public void Preview_AudioFormatter_Loads_AudioTags_From_Disk()
        {
            var path = Path.Combine(_tempRoot, $"tagged_{Guid.NewGuid():N}.wav");
            TaggedMinimalWav.WriteTagged(path, title: "ListIngestTitle", album: "ListIngestAlbum");

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([path]);
            var item = Assert.Single(renameList.RenameItems);

            _ = _SetupPreview(renameList, preset: _CreateAudioTitleAlbumPreset());

            Assert.Equal(RenameStatus.PreviewOk, item.Status);
            Assert.Equal("ListIngestTitle", item.Original.AudioTagOverlay.Semantic().Title);
            Assert.Equal("ListIngestAlbum", item.Original.AudioTagOverlay.Semantic().Album);
        }

        [Fact]
        /// <summary>
        /// Verifies cached tag overlays reset after commit so later preview reloads embedded tags from disk.
        /// </summary>
        public void Commit_Clears_AudioTag_Cache_For_Repreview()
        {
            var path = Path.Combine(_tempRoot, $"tagged_{Guid.NewGuid():N}.wav");
            TaggedMinimalWav.WriteTagged(path, title: "RoundOneTitle", album: null);

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([path]);
            var item = Assert.Single(renameList.RenameItems);

            var preset = _CreateAudioTitleAlbumPreset();
            var plan = _SetupPreview(renameList, preset);
            Assert.Equal("RoundOneTitle", item.Original.AudioTagOverlay.Semantic().Title);

            _ = renameList.Commit(plan, failFast: false, dryRun: true);
            Assert.Equal(new AudioTagOverlay(), item.Original.AudioTagOverlay);

            TaggedMinimalWav.WriteTagged(path, title: "RoundTwoTitle", album: null);

            _ = _SetupPreview(renameList, preset);
            Assert.Equal("RoundTwoTitle", item.Original.AudioTagOverlay.Semantic().Title);
        }

        [Fact]
        /// <summary>
        /// Verifies directories never load embedded audio-tag metadata.
        /// </summary>
        public void AddSources_DirectoryEntry_LeavesAudioTagsEmpty()
        {
            var folder = Path.Combine(_tempRoot, "folder");
            Directory.CreateDirectory(folder);

            var renameList = new RenameList(includeHidden: true);
            var beforeCount = renameList.RenameItems.Count;
            renameList.AddSources([folder], includeFiles: false, includeFolders: true);
            Assert.Equal(1, renameList.RenameItems.Count - beforeCount);

            var item = Assert.Single(renameList.RenameItems);
            Assert.Equal(new AudioTagOverlay(), item.Original.AudioTagOverlay);
        }

        [Fact]
        /// <summary>
        /// Verifies formatter audio tokens on a folder row surfaces preview failure instead of silently empty overlays.
        /// </summary>
        public void Preview_AudioFormatter_OnDirectory_HasPreviewError()
        {
            var folder = Path.Combine(_tempRoot, "folder");
            Directory.CreateDirectory(folder);

            var renameList = new RenameList(includeHidden: true);
            var beforeCount = renameList.RenameItems.Count;
            renameList.AddSources([folder], includeFiles: false, includeFolders: true);
            Assert.Equal(1, renameList.RenameItems.Count - beforeCount);

            var item = Assert.Single(renameList.RenameItems);
            var preset = _CreateAudioTitleAlbumPreset();
            _ = _SetupPreview(renameList, preset);

            Assert.Equal(RenameStatus.PreviewError, item.Status);
            Assert.NotNull(item.PreviewError);
            Assert.Contains("directory", item.PreviewError.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        /// <summary>
        /// Verifies audio overlay field filters on a folder row surface preview failure.
        /// </summary>
        public void Preview_SemanticAudioFieldTarget_OnDirectory_HasPreviewError()
        {
            var folder = Path.Combine(_tempRoot, "folder");
            Directory.CreateDirectory(folder);

            var renameList = new RenameList(includeHidden: true);
            var beforeCount = renameList.RenameItems.Count;
            renameList.AddSources([folder], includeFiles: false, includeFolders: true);
            Assert.Equal(1, renameList.RenameItems.Count - beforeCount);

            var item = Assert.Single(renameList.RenameItems);
            var preset = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "overlay-on-dir",
                Description = null,
                Chain = FilterChain.CreateAllEnabled([
                    new FormatterFilter(
                        Target: new SemanticAudioFieldTarget(SemanticAudioField.Title),
                        Options: new FormatterOptions("x")
                    ),
                ]),
            };
            _ = _SetupPreview(renameList, preset);

            Assert.Equal(RenameStatus.PreviewError, item.Status);
            Assert.NotNull(item.PreviewError);
            Assert.Contains("directory", item.PreviewError.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        /// <summary>
        /// Verifies a non-container text file yields preview failure when audio overlay target resolves TagLib-backed tags.
        /// </summary>
        public void Preview_AudioFormatter_OnNonAudioTextFile_HasPreviewError()
        {
            var path = TestHelpers.CreateFile(_tempRoot, "note.txt");
            File.WriteAllText(path, "plain text payload");

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([path]);
            var item = Assert.Single(renameList.RenameItems);

            var preset = _CreateAudioTitleAlbumPreset();
            _ = _SetupPreview(renameList, preset);

            Assert.Equal(RenameStatus.PreviewError, item.Status);
            Assert.NotNull(item.PreviewError);
            Assert.NotNull(item.PreviewError.Cause);
        }

        /// <summary>
        /// Verifies <see cref="AudioTagSetterFilter"/> invalid year text surfaces as a row preview error.
        /// </summary>
        [Fact]
        public void Preview_AudioTagSetter_InvalidYear_SetsPreviewError()
        {
            var path = Path.Combine(_tempRoot, "track.wav");
            MinimalWavFixture.CopyScratchTo(path);

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([path]);
            var item = Assert.Single(renameList.RenameItems);

            var preset = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "bad-year",
                Description = null,
                Chain = FilterChain.CreateAllEnabled([
                    new AudioTagSetterFilter(
                        new AudioTagSetterOptions(Year: new AudioTagStringFieldOptions(Text: "nope"))
                    ),
                ]),
            };

            _ = _SetupPreview(renameList, preset);

            Assert.Equal(RenameStatus.PreviewError, item.Status);
            Assert.NotNull(item.PreviewError);
            Assert.Contains("1-9999", item.PreviewError.Message, StringComparison.Ordinal);
        }

        [Fact]
        /// <summary>
        /// Verifies an inaccessible folder source is skipped without aborting the batch.
        /// </summary>
        public void AddSources_Skips_Inaccessible_Folder()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var deniedFolder = Directory.CreateDirectory(_tempRoot.CombinePath("Denied")).FullName;
            _DenyDirectoryTraverse(deniedFolder);

            try
            {
                var renameList = new RenameList(includeHidden: false);
                var summary = renameList.AddSources([deniedFolder.CombinePath("*")]);

                Assert.Equal(1, summary.SkippedSourceCount);
                Assert.Empty(renameList.RenameItems);
            }
            finally
            {
                _AllowDirectoryTraverse(deniedFolder);
            }
        }

        [Fact]
        /// <summary>
        /// Verifies Add All-style mixed sources keep readable entries and skip inaccessible folders.
        /// </summary>
        public void AddSources_Mixed_Accessible_And_Inaccessible_Sources()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var goodFile = TestHelpers.CreateFile(_tempRoot, "good.txt");
            var deniedFolder = Directory.CreateDirectory(_tempRoot.CombinePath("Denied")).FullName;
            _DenyDirectoryTraverse(deniedFolder);

            try
            {
                var renameList = new RenameList(includeHidden: false);
                var summary = renameList.AddSources([goodFile, deniedFolder.CombinePath("*")]);

                Assert.Equal(1, summary.SkippedSourceCount);
                Assert.Single(renameList.RenameItems);
                Assert.Equal(goodFile, renameList.RenameItems[0].Original.FullPath);
            }
            finally
            {
                _AllowDirectoryTraverse(deniedFolder);
            }
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private static void _DenyDirectoryTraverse(string directoryPath)
        {
            var directoryInfo = new DirectoryInfo(directoryPath);
            var security = directoryInfo.GetAccessControl();
            security.AddAccessRule(
                new System.Security.AccessControl.FileSystemAccessRule(
                    identity: System.Security.Principal.WindowsIdentity.GetCurrent().Name,
                    fileSystemRights: System.Security.AccessControl.FileSystemRights.ListDirectory
                        | System.Security.AccessControl.FileSystemRights.Traverse,
                    type: System.Security.AccessControl.AccessControlType.Deny
                )
            );
            directoryInfo.SetAccessControl(security);
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private static void _AllowDirectoryTraverse(string directoryPath)
        {
            var directoryInfo = new DirectoryInfo(directoryPath);
            var security = directoryInfo.GetAccessControl();
            security.RemoveAccessRuleAll(
                new System.Security.AccessControl.FileSystemAccessRule(
                    identity: System.Security.Principal.WindowsIdentity.GetCurrent().Name,
                    fileSystemRights: System.Security.AccessControl.FileSystemRights.ListDirectory
                        | System.Security.AccessControl.FileSystemRights.Traverse,
                    type: System.Security.AccessControl.AccessControlType.Deny
                )
            );
            directoryInfo.SetAccessControl(security);
        }

        private static FilterPreset _CreateAudioTitleAlbumPreset()
        {
            return new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "audio-title-album-preview",
                Description = null,
                Chain = FilterChain.CreateAllEnabled([
                    new FormatterFilter(
                        Target: new FileFullNameTarget(),
                        Options: new FormatterOptions("<audio-title>-<audio-album>")
                    ),
                ]),
            };
        }

        private static CommitPlan _SetupPreview(RenameList renameList, FilterPreset preset)
        {
            preset.Chain.SetupFilters();
            return renameList.Preview(preset);
        }
    }
}
