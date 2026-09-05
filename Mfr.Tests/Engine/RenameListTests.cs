using Mfr.Filters.Audio;
using Mfr.Filters.Case;
using Mfr.Filters.Formatting;
using Mfr.Models.RenameList.Fields.AudioTag;
using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Models.RenameList.Fields.Extended;
using Mfr.Models.Tags;
using Mfr.Tests.Ui.RenameList;
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

            var renameList = new RenameList();
            renameList.AddSources(sources: sources, includeFiles: true, includeFolders: false);
            var entries = renameList.RenameItems;

            Assert.Equal(3, entries.Count);
            Assert.Equal(betaPath, entries[0].Original.FullPath);
            Assert.Equal(
                new[] { alphaPath, gammaPath }.OrderBy(path => path, StringComparer.Ordinal).ToArray(),
                entries.Skip(1).Select(e => e.Original.FullPath).OrderBy(path => path, StringComparer.Ordinal).ToArray()
            );
            Assert.Equal([0, 1, 2], entries.Select(e => e.Original.RenameListIndex));
            Assert.Equal("beta", entries[0].Original.Prefix);
            Assert.Equal(".log", entries[0].Original.Extension);
            Assert.All(entries, e => Assert.Equal(_tempRoot, e.Original.DirectoryPath));
        }

        [Fact]
        /// <summary>
        /// Verifies that duplicate source additions are allowed, while resolved items stay distinct.
        /// </summary>
        public void AddSources_Allows_Duplicate_Sources_But_ResolvedItems_Are_Deduplicated()
        {
            var source = TestHelpers.CreateFile(_tempRoot, "alpha.txt");

            var renameList = new RenameList();
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

            var renameList = new RenameList();
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

            var renameList = new RenameList();
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

            var renameList = new RenameList();
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

            var renameList = new RenameList();
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

            var renameList = new RenameList();
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

            var renameList = new RenameList();
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

            var renameList = new RenameList();
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

            var renameList = new RenameList();
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

            var renameList = new RenameList();
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

            var renameList = new RenameList();
            renameList.AddSources([alphaPath, betaPath, gammaPath]);
            var items = renameList.RenameItems;

            Assert.False(renameList.MoveSelectedBefore([items[0], items[1]], beforeItem: items[1]));
            Assert.Equal([alphaPath, betaPath, gammaPath], items.Select(entry => entry.Original.FullPath));
        }

        /// <summary>
        /// Verifies Sort with default keys orders by type, parent folder, then file name.
        /// </summary>
        [Fact]
        public void Sort_DefaultKeys_Orders_By_Type_Then_Parent_Then_Name()
        {
            var subDir = Path.Combine(_tempRoot, "beta");
            Directory.CreateDirectory(subDir);
            var alphaDir = Path.Combine(_tempRoot, "alpha");
            Directory.CreateDirectory(alphaDir);

            var rootFile = TestHelpers.CreateFile(_tempRoot, "zeta.txt");
            var subFile = TestHelpers.CreateFile(subDir, "alpha.txt");
            var alphaFile = TestHelpers.CreateFile(alphaDir, "beta.txt");
            var folderPath = Path.Combine(_tempRoot, "folder-item");
            Directory.CreateDirectory(folderPath);

            var renameList = new RenameList();
            renameList.AddSources([rootFile, subFile, alphaFile, folderPath], includeFolders: true, includeFiles: true);

            Assert.True(renameList.Sort(RenameListSortKey.DefaultKeys));

            Assert.Equal(
                [rootFile, alphaFile, subFile, folderPath],
                renameList.RenameItems.Select(item => item.Original.FullPath)
            );
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

            var renameList = new RenameList();
            renameList.AddSources([filePath, folderPath, earlyFile], includeFolders: true, includeFiles: true);

            Assert.True(
                renameList.Sort([
                    new RenameListSortKey(RenameListTestHelpers.FileFolderKey),
                    new RenameListSortKey(RenameListTestHelpers.FullPathKey),
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

            var renameList = new RenameList();
            renameList.AddSources([alphaPath, betaPath, gammaPath]);

            Assert.True(
                renameList.Sort([new RenameListSortKey(RenameListTestHelpers.FullFileNameKey, Descending: true)])
            );

            Assert.Equal(
                [gammaPath, betaPath, alphaPath],
                renameList.RenameItems.Select(item => item.Original.FullPath)
            );
        }

        /// <summary>
        /// Verifies Sort by File Name orders alphabetically by prefix.
        /// </summary>
        [Fact]
        public void Sort_BasicName_Orders_By_Prefix()
        {
            var (alphaPath, betaPath, gammaPath) = TestHelpers.CreateFiles(
                _tempRoot,
                "alpha.txt",
                "beta.log",
                "gamma.txt"
            );

            var renameList = new RenameList();
            renameList.AddSources([gammaPath, betaPath, alphaPath]);

            var nameKey = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            Assert.True(renameList.Sort([new RenameListSortKey(nameKey)]));

            Assert.Equal(
                [alphaPath, betaPath, gammaPath],
                renameList.RenameItems.Select(item => item.Original.FullPath)
            );
        }

        /// <summary>
        /// Verifies Sort by Creation Date orders chronologically.
        /// </summary>
        [Fact]
        public void Sort_ExtendedCreationDate_Orders_Chronologically()
        {
            var earlyPath = TestHelpers.CreateFile(_tempRoot, "early.txt");
            var latePath = TestHelpers.CreateFile(_tempRoot, "late.txt");
            File.SetCreationTime(earlyPath, new DateTime(2020, 1, 1, 12, 0, 0));
            File.SetCreationTime(latePath, new DateTime(2024, 6, 1, 12, 0, 0));

            var renameList = new RenameList();
            renameList.AddSources([latePath, earlyPath]);

            var creationDateKey = RenameListFieldKey.Original(
                ExtendedRenameListFields.Group,
                ExtendedCreationDateField.CreationDateKey
            );
            Assert.True(renameList.Sort([new RenameListSortKey(creationDateKey)]));

            Assert.Equal([earlyPath, latePath], renameList.RenameItems.Select(item => item.Original.FullPath));
        }

        /// <summary>
        /// Verifies Sort is a no-op for empty keys or a single-item list.
        /// </summary>
        [Fact]
        public void Sort_Empty_Keys_Or_Single_Item_Is_NoOp()
        {
            var path = TestHelpers.CreateFile(_tempRoot, "alpha.txt");
            var renameList = new RenameList();
            renameList.AddSources([path]);

            Assert.False(renameList.Sort([]));
            Assert.False(renameList.Sort(RenameListSortKey.DefaultKeys));

            var (alphaPath, betaPath) = TestHelpers.CreateFiles(_tempRoot, "z.txt", "a.txt");
            renameList = new RenameList();
            renameList.AddSources([alphaPath, betaPath]);
            Assert.False(renameList.Sort([]));
            Assert.Equal([alphaPath, betaPath], renameList.RenameItems.Select(item => item.Original.FullPath));
        }

        /// <summary>
        /// Verifies Sort by File Name Numeric Value orders 2 before 10.
        /// </summary>
        [Fact]
        public void Sort_FileNameNumeric_Orders_2_Before_10()
        {
            var tenPath = TestHelpers.CreateFile(_tempRoot, "file10.txt");
            var twoPath = TestHelpers.CreateFile(_tempRoot, "file2.txt");
            var renameList = new RenameList();
            renameList.AddSources([tenPath, twoPath]);

            var numericKey = RenameListFieldKey.Original(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FileNameNumeric
            );
            Assert.True(renameList.Sort([new RenameListSortKey(numericKey)]));
            Assert.Equal([twoPath, tenPath], renameList.RenameItems.Select(item => item.Original.FullPath));
        }

        /// <summary>
        /// Verifies Sort by Size orders smaller files first.
        /// </summary>
        [Fact]
        public void Sort_Size_Orders_Smaller_First()
        {
            var largePath = TestHelpers.CreateFile(_tempRoot, "large.txt");
            var smallPath = TestHelpers.CreateFile(_tempRoot, "small.txt");
            File.WriteAllText(largePath, new string('x', 50));
            File.WriteAllText(smallPath, "x");

            var renameList = new RenameList();
            renameList.AddSources([largePath, smallPath]);

            var sizeKey = RenameListFieldKey.Original(ExtendedRenameListFields.Group, ExtendedSizeField.SizeKey);
            Assert.True(renameList.Sort([new RenameListSortKey(sizeKey)]));
            Assert.Equal([smallPath, largePath], renameList.RenameItems.Select(item => item.Original.FullPath));
        }

        /// <summary>
        /// Verifies equal sort keys keep add order across repeated Sort calls.
        /// </summary>
        [Fact]
        public void Sort_Tied_Keys_Keep_Add_Order()
        {
            var betaPath = TestHelpers.CreateFile(_tempRoot, "beta.txt");
            var alphaPath = TestHelpers.CreateFile(_tempRoot, "alpha.txt");
            var renameList = new RenameList();
            renameList.AddSources([betaPath, alphaPath]);

            var extensionKey = RenameListFieldKey.Original(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.Extension
            );
            Assert.True(renameList.Sort([new RenameListSortKey(extensionKey)]));
            Assert.Equal([betaPath, alphaPath], renameList.RenameItems.Select(item => item.Original.FullPath));

            Assert.True(renameList.Sort([new RenameListSortKey(extensionKey)]));
            Assert.Equal([betaPath, alphaPath], renameList.RenameItems.Select(item => item.Original.FullPath));
        }

        /// <summary>
        /// Verifies a preview sort key is ignored and does not throw.
        /// </summary>
        [Fact]
        public void Sort_Preview_Key_Does_Not_Throw_And_Keeps_Order()
        {
            var (alphaPath, betaPath) = TestHelpers.CreateFiles(_tempRoot, "alpha.txt", "beta.txt");
            var renameList = new RenameList();
            renameList.AddSources([betaPath, alphaPath]);

            var previewKey = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName);
            Assert.False(renameList.Sort([new RenameListSortKey(previewKey)]));
            Assert.Equal([betaPath, alphaPath], renameList.RenameItems.Select(item => item.Original.FullPath));
        }

        [Fact]
        /// <summary>
        /// Verifies that removing by item reference drops the item from the list.
        /// </summary>
        public void Remove_ByItem_Removes_From_List()
        {
            var path = TestHelpers.CreateFile(_tempRoot, "alpha.txt");

            var renameList = new RenameList();
            renameList.AddSources([path]);
            var item = renameList.RenameItems[0];

            Assert.Equal(1, renameList.Remove(item));
            Assert.Empty(renameList.RenameItems);
        }

        /// <summary>
        /// Verifies RemoveUnchanged keeps only rows whose preview for the key differs from the original.
        /// </summary>
        [Fact]
        public void RemoveUnchanged_keeps_changed_preview_rows_only()
        {
            var (helloPath, worldPath, otherPath) = TestHelpers.CreateFiles(
                _tempRoot,
                "hello.txt",
                "WORLD.txt",
                "other.txt"
            );

            var renameList = new RenameList();
            renameList.AddSources([helloPath, worldPath, otherPath]);
            renameList.Preview(
                FilterChain.CreateAllEnabled([
                    new LettersCaseFilter(
                        new FilePrefixTarget(),
                        new LettersCaseOptions(LettersCaseMode.UpperCase, CapitalizeSkipWords: [])
                    ),
                ])
            );

            var previewKey = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName);
            Assert.Equal(1, renameList.RemoveUnchanged(previewKey));

            Assert.Equal([helloPath, otherPath], renameList.RenameItems.Select(item => item.Original.FullPath));
            Assert.Equal([0, 1], renameList.RenameItems.Select(item => item.Original.RenameListIndex));
        }

        /// <summary>
        /// Verifies RemoveUnchanged is a no-op for original keys and an empty list.
        /// </summary>
        [Fact]
        public void RemoveUnchanged_original_key_or_empty_list_is_noop()
        {
            var emptyList = new RenameList();
            var previewKey = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName);
            Assert.Equal(0, emptyList.RemoveUnchanged(previewKey));

            var path = TestHelpers.CreateFile(_tempRoot, "hello.txt");
            var renameList = new RenameList();
            renameList.AddSources([path]);
            renameList.Preview(
                FilterChain.CreateAllEnabled([
                    new LettersCaseFilter(
                        new FilePrefixTarget(),
                        new LettersCaseOptions(LettersCaseMode.UpperCase, CapitalizeSkipWords: [])
                    ),
                ])
            );

            var originalKey = RenameListFieldKey.Original(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FullName
            );
            Assert.Equal(0, renameList.RemoveUnchanged(originalKey));
            Assert.Single(renameList.RenameItems);
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

            var renameList = new RenameList();
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

            var renameList = new RenameList();
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

            var renameList = new RenameList();
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

            var renameList = new RenameList();
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
            var renameList = new RenameList();

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
            var renameList = new RenameList();

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

            var excludeHiddenList = new RenameList();
            excludeHiddenList.AddSources([hiddenPath]);
            excludeHiddenList.AddSources([visiblePath]);
            var excludedHidden = excludeHiddenList.RenameItems.ToList();

            var includeHiddenList = new RenameList();
            includeHiddenList.AddSources([hiddenPath], includeHidden: true);
            includeHiddenList.AddSources([visiblePath], includeHidden: true);
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

            var renameList = new RenameList();
            renameList.AddSources(sources: [filePath, folderPath], includeFiles: false, includeFolders: true);

            var entry = Assert.Single(renameList.RenameItems);
            Assert.Equal(folderPath, entry.Original.FullPath);
        }

        [Fact]
        /// <summary>
        /// Verifies a path skipped by include flags is not reserved, so a later add with those flags on can accept it.
        /// </summary>
        public void AddSources_RejectedByIncludeFlags_CanBeAddedWhenIncluded()
        {
            var filePath = TestHelpers.CreateFile(_tempRoot, "alpha.txt");

            var renameList = new RenameList();
            renameList.AddSources(sources: [filePath], includeFiles: false, includeFolders: true);
            Assert.Empty(renameList.RenameItems);

            renameList.AddSources(sources: [filePath], includeFiles: true, includeFolders: false);
            Assert.Equal(filePath, Assert.Single(renameList.RenameItems).Original.FullPath);
        }

        [Fact]
        /// <summary>
        /// Verifies that a folder whose name contains a dot is not split into pseudo prefix and extension like a file.
        /// </summary>
        public void AddSources_Folder_WithDotInName_UsesFullSegmentAsPrefix()
        {
            var folderPath = Directory.CreateDirectory(_tempRoot.CombinePath("release.v2")).FullName;

            var renameList = new RenameList();
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

            var renameList = new RenameList();
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

            var renameList = new RenameList();
            var beforeCount = renameList.RenameItems.Count;
            renameList.AddSources(sources: [folderPath], includeFiles: true, includeFolders: false);
            var addedCount = renameList.RenameItems.Count - beforeCount;

            Assert.Equal(2, addedCount);
            Assert.Equal(
                new[] { topLevelFirstPath, topLevelSecondPath }.OrderBy(path => path, StringComparer.Ordinal).ToArray(),
                renameList
                    .RenameItems.Select(entry => entry.Original.FullPath)
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .ToArray()
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

            var renameList = new RenameList();
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

            var renameList = new RenameList();
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

            var renameList = new RenameList();
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

            var renameList = new RenameList();
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

            var renameList = new RenameList();
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

            var renameList = new RenameList();
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

            var reports = new List<RenameListProgress>();
            var renameList = new RenameList();
            renameList.AddSources(
                sources: [_tempRoot.CombinePath("*.txt")],
                includeFiles: true,
                includeFolders: false,
                includeSubdirs: true,
                progress: new SynchronousProgress<RenameListProgress>(reports.Add)
            );

            Assert.Equal(3, renameList.RenameItems.Count);
            Assert.NotEmpty(reports);
            var last = reports[^1];
            Assert.True(last.ScannedCount >= 3);
            Assert.Equal(3, last.AddedCount);
            Assert.Equal(RenameListProgressPhase.ResolveSources, last.Phase);
            Assert.Equal(0, last.MetadataProcessedCount);
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
            var renameList = new RenameList();
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

            using var cts = new CancellationTokenSource();
            var renameList = new RenameList();
            renameList.AddSources([keepPath]);

            renameList.AddSources(
                sources: [_tempRoot.CombinePath("*.txt")],
                includeFiles: true,
                includeFolders: false,
                includeSubdirs: true,
                cancellationToken: cts.Token,
                progress: new SynchronousProgress<RenameListProgress>(report =>
                {
                    if (report.Phase == RenameListProgressPhase.ResolveSources && report.ScannedCount >= 1)
                    {
                        cts.Cancel();
                    }
                })
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

            var renameList = new RenameList();
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

            var renameList = new RenameList();
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

            var renameList = new RenameList();
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

            var renameList = new RenameList();
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

            var renameList = new RenameList();
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

            var renameList = new RenameList();
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

            var renameList = new RenameList();
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

            var renameList = new RenameList();
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
                var renameList = new RenameList();
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
                var renameList = new RenameList();
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

        /// <summary>
        /// Verifies EnsureMetadataLoaded reads embedded tags for grid display.
        /// </summary>
        [Fact]
        public void EnsureMetadataLoaded_Loads_Embedded_Audio_Tags()
        {
            var path = Path.Combine(_tempRoot, $"tagged_{Guid.NewGuid():N}.wav");
            TaggedMinimalWav.WriteTagged(path, title: "HydrateTitle", album: null);

            var renameList = new RenameList();
            renameList.AddSources([path]);
            var item = Assert.Single(renameList.RenameItems);
            Assert.False(item.TagLibLoadAttempted);

            renameList.EnsureMetadataLoaded(RenameListMetadataRequirement.TagLib);

            Assert.True(item.TagLibLoadAttempted);
            Assert.Equal("HydrateTitle", item.Original.AudioTagOverlay.Semantic().Title);
        }

        /// <summary>
        /// Verifies EnsureMetadataLoaded with no requirement does not open files.
        /// </summary>
        [Fact]
        public void EnsureMetadataLoaded_None_Does_Not_Load()
        {
            var path = Path.Combine(_tempRoot, $"tagged_{Guid.NewGuid():N}.wav");
            TaggedMinimalWav.WriteTagged(path, title: "SkipTitle", album: null);

            var renameList = new RenameList();
            renameList.AddSources([path]);
            renameList.EnsureMetadataLoaded(RenameListMetadataRequirement.None);

            var item = Assert.Single(renameList.RenameItems);
            Assert.False(item.TagLibLoadAttempted);
        }

        /// <summary>
        /// Verifies hydrate then sort orders rows by embedded title without the comparer opening files.
        /// </summary>
        [Fact]
        public void EnsureMetadataLoaded_Then_Sort_By_Title_Orders_Items()
        {
            var betaPath = Path.Combine(_tempRoot, $"beta_{Guid.NewGuid():N}.wav");
            var alphaPath = Path.Combine(_tempRoot, $"alpha_{Guid.NewGuid():N}.wav");
            TaggedMinimalWav.WriteTagged(betaPath, title: "Beta", album: null);
            TaggedMinimalWav.WriteTagged(alphaPath, title: "Alpha", album: null);

            var renameList = new RenameList();
            renameList.AddSources([betaPath, alphaPath]);
            var titleKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");
            renameList.EnsureMetadataLoaded(RenameListMetadataRequirement.TagLib);

            Assert.True(renameList.Sort([new RenameListSortKey(titleKey)]));
            Assert.Equal(alphaPath, renameList.RenameItems[0].Original.FullPath);
            Assert.Equal(betaPath, renameList.RenameItems[1].Original.FullPath);
        }

        /// <summary>
        /// Verifies add with a metadata requirement reports a distinct hydrate phase that keeps resolve counts.
        /// </summary>
        [Fact]
        public void AddSources_With_MetadataRequirement_Reports_LoadMetadata_Phase()
        {
            var path = Path.Combine(_tempRoot, $"tagged_{Guid.NewGuid():N}.wav");
            TaggedMinimalWav.WriteTagged(path, title: "ProgressTitle", album: null);

            var reports = new List<RenameListProgress>();
            var renameList = new RenameList();
            renameList.AddSources(
                [path],
                metadataRequirement: RenameListMetadataRequirement.TagLib,
                progress: new SynchronousProgress<RenameListProgress>(reports.Add)
            );

            Assert.NotEmpty(reports);
            var last = reports[^1];
            Assert.Equal(RenameListProgressPhase.LoadMetadata, last.Phase);
            Assert.Equal(1, last.AddedCount);
            Assert.True(last.ScannedCount >= 1);
            Assert.Equal(1, last.MetadataProcessedCount);
            Assert.Equal(1, last.MetadataTotalCount);
        }

        /// <summary>
        /// Verifies canceling during metadata hydrate discards the staging batch.
        /// </summary>
        [Fact]
        public void AddSources_Cancel_During_Metadata_Discards_Batch()
        {
            var path = Path.Combine(_tempRoot, $"tagged_{Guid.NewGuid():N}.wav");
            TaggedMinimalWav.WriteTagged(path, title: "CancelTitle", album: null);

            using var cts = new CancellationTokenSource();
            var renameList = new RenameList();
            renameList.AddSources(
                [path],
                metadataRequirement: RenameListMetadataRequirement.TagLib,
                cancellationToken: cts.Token,
                progress: new SynchronousProgress<RenameListProgress>(report =>
                {
                    if (report.Phase == RenameListProgressPhase.LoadMetadata)
                    {
                        cts.Cancel();
                    }
                })
            );

            Assert.Empty(renameList.RenameItems);
        }

        /// <summary>
        /// Verifies Sort does not open files; metadata must be hydrated first.
        /// </summary>
        [Fact]
        public void Sort_By_Title_Does_Not_Load_Embedded_Tags()
        {
            var alphaPath = Path.Combine(_tempRoot, $"alpha_{Guid.NewGuid():N}.wav");
            var betaPath = Path.Combine(_tempRoot, $"beta_{Guid.NewGuid():N}.wav");
            TaggedMinimalWav.WriteTagged(alphaPath, title: "Beta", album: null);
            TaggedMinimalWav.WriteTagged(betaPath, title: "Alpha", album: null);

            var renameList = new RenameList();
            renameList.AddSources([alphaPath, betaPath]);
            var titleKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");

            renameList.Sort([new RenameListSortKey(titleKey)]);

            Assert.All(renameList.RenameItems, item => Assert.False(item.TagLibLoadAttempted));
        }

        /// <summary>
        /// Verifies AddSources hydrates a staging batch when a metadata requirement is supplied.
        /// </summary>
        [Fact]
        public void AddSources_With_MetadataRequirement_Hydrates_Batch_Before_Insert()
        {
            var path = Path.Combine(_tempRoot, $"tagged_{Guid.NewGuid():N}.wav");
            TaggedMinimalWav.WriteTagged(path, title: "BatchTitle", album: null);

            var renameList = new RenameList();
            renameList.AddSources([path], metadataRequirement: RenameListMetadataRequirement.TagLib);

            var item = Assert.Single(renameList.RenameItems);
            Assert.True(item.TagLibLoadAttempted);
            Assert.Equal("BatchTitle", item.Original.AudioTagOverlay.Semantic().Title);
        }

        /// <summary>
        /// Verifies EnsureMetadataLoaded honors cancel without throwing or loading.
        /// </summary>
        [Fact]
        public void EnsureMetadataLoaded_Cancel_Stops_Without_Throwing()
        {
            var path = Path.Combine(_tempRoot, $"tagged_{Guid.NewGuid():N}.wav");
            TaggedMinimalWav.WriteTagged(path, title: "CancelTitle", album: null);

            var renameList = new RenameList();
            renameList.AddSources([path]);

            using var cts = new CancellationTokenSource();
            cts.Cancel();
            renameList.EnsureMetadataLoaded(RenameListMetadataRequirement.TagLib, cts.Token);

            Assert.False(Assert.Single(renameList.RenameItems).TagLibLoadAttempted);
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
            return renameList.Preview(preset.Chain);
        }
    }
}
