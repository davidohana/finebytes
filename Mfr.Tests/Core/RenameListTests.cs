using Mfr.Core;
using Mfr.Tests.TestSupport;
using Mfr.Utils;

namespace Mfr.Tests.Core
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
            _tempRoot = Directory.GetCurrentDirectory().CombinePath(
                "mfr_renamelist_tests_" + Guid.NewGuid().ToString("N"));
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
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
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
                "gamma.txt");

            var sources = new[]
            {
                betaPath,
                _tempRoot.CombinePath("*.txt"),
                _tempRoot,
            };

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources(sources);
            var entries = renameList.RenameItems;

            var tempRootParent = Path.GetDirectoryName(_tempRoot) ?? "";
            var tempRootName = Path.GetFileName(_tempRoot);

            Assert.Equal(4, entries.Count);
            Assert.Equal(
                [betaPath, alphaPath, gammaPath, _tempRoot],
                entries.Select(e => e.Original.FullPath));
            Assert.Equal([0, 1, 2, 3], entries.Select(e => e.Original.GlobalIndex));
            Assert.Equal([0, 1, 2, 0], entries.Select(e => e.Original.InFolderIndex));
            Assert.Equal(["beta", "alpha", "gamma", tempRootName], entries.Select(e => e.Original.Prefix));
            Assert.Equal([".log", ".txt", ".txt", ""], entries.Select(e => e.Original.Extension));
            Assert.Equal(
                [_tempRoot, _tempRoot, _tempRoot, tempRootParent],
                entries.Select(e => e.Original.DirectoryPath));
        }

        [Fact]
        /// <summary>
        /// Verifies that duplicate source additions are allowed, while resolved items stay distinct.
        /// </summary>
        public void AddSource_Allows_Duplicate_Sources_But_ResolvedItems_Are_Deduplicated()
        {
            var source = TestHelpers.CreateFile(_tempRoot, "alpha.txt");

            var renameList = new RenameList(includeHidden: true);
            Assert.Equal(1, renameList.AddSource(source));
            Assert.Equal(0, renameList.AddSource(source));

            Assert.Single(renameList.RenameItems);
            Assert.Equal(source, renameList.RenameItems[0].Original.FullPath);
        }

        [Fact]
        /// <summary>
        /// Verifies that hidden files are skipped unless hidden inclusion is enabled.
        /// </summary>
        public void AddSource_Filters_Hidden_When_Disabled()
        {
            var hiddenFileName = OperatingSystem.IsWindows() ? "hidden.txt" : ".hidden.txt";
            var (visiblePath, hiddenPath) = TestHelpers.CreateFiles(_tempRoot, "visible.txt", hiddenFileName);
            if (OperatingSystem.IsWindows())
            {
                var hiddenAttrs = File.GetAttributes(hiddenPath);
                File.SetAttributes(hiddenPath, hiddenAttrs | FileAttributes.Hidden);
            }

            var excludeHiddenList = new RenameList(includeHidden: false);
            excludeHiddenList.AddSource(hiddenPath);
            excludeHiddenList.AddSource(visiblePath);
            var excludedHidden = excludeHiddenList.RenameItems.ToList();

            var includeHiddenList = new RenameList(includeHidden: true);
            includeHiddenList.AddSource(hiddenPath);
            includeHiddenList.AddSource(visiblePath);
            var includedHidden = includeHiddenList.RenameItems.ToList();

            Assert.Single(excludedHidden);
            Assert.Equal(visiblePath, excludedHidden[0].Original.FullPath);
            Assert.Equal(0, excludedHidden[0].Original.GlobalIndex);
            Assert.Equal(0, excludedHidden[0].Original.InFolderIndex);

            Assert.Equal(2, includedHidden.Count);
            Assert.Equal([hiddenPath, visiblePath], includedHidden.Select(x => x.Original.FullPath));
            Assert.Equal([0, 1], includedHidden.Select(x => x.Original.GlobalIndex));
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
            renameList.AddSources(
                sources: [filePath, folderPath],
                includeFiles: false,
                includeFolders: true);

            var entry = Assert.Single(renameList.RenameItems);
            Assert.Equal(folderPath, entry.Original.FullPath);
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
            renameList.AddSources(
                sources: [filePath, folderPath],
                includeFiles: true,
                includeFolders: false);

            Assert.Equal(
                [filePath, folderFilePath],
                renameList.RenameItems.Select(entry => entry.Original.FullPath));
        }

        [Fact]
        /// <summary>
        /// Verifies that a directory source expands to top-level files only when folder inclusion is disabled.
        /// </summary>
        public void AddSource_DirectorySource_WithFoldersDisabled_AddsTopLevelFilesOnly()
        {
            var folderPath = Directory.CreateDirectory(_tempRoot.CombinePath("Album")).FullName;
            var topLevelFirstPath = TestHelpers.CreateFile(folderPath, "first.txt");
            var topLevelSecondPath = TestHelpers.CreateFile(folderPath, "second.log");
            var nestedFilePath = TestHelpers.CreateFile(folderPath.CombinePath("Sub"), "nested.txt");

            var renameList = new RenameList(includeHidden: true);
            var addedCount = renameList.AddSource(
                source: folderPath,
                includeFiles: true,
                includeFolders: false);

            Assert.Equal(2, addedCount);
            Assert.Equal(
                [topLevelFirstPath, topLevelSecondPath],
                renameList.RenameItems.Select(entry => entry.Original.FullPath));
            Assert.DoesNotContain(
                nestedFilePath,
                renameList.RenameItems.Select(entry => entry.Original.FullPath));
        }

        [Fact]
        /// <summary>
        /// Verifies that a directory source expands recursively when folder inclusion is disabled and recursive add is enabled.
        /// </summary>
        public void AddSource_DirectorySource_WithFoldersDisabled_AndRecursiveEnabled_AddsNestedFiles()
        {
            var folderPath = Directory.CreateDirectory(_tempRoot.CombinePath("Album")).FullName;
            var topLevelPath = TestHelpers.CreateFile(folderPath, "first.txt");
            var nestedPath = TestHelpers.CreateFile(folderPath.CombinePath("Sub"), "nested.txt");
            var deeperPath = TestHelpers.CreateFile(folderPath.CombinePath("Sub", "Deep"), "deep.log");

            var renameList = new RenameList(includeHidden: true);
            var addedCount = renameList.AddSource(
                source: folderPath,
                includeFiles: true,
                includeFolders: false,
                recursiveDirectoryFileAdd: true);

            Assert.Equal(3, addedCount);
            Assert.Equal(
                [topLevelPath, nestedPath, deeperPath],
                renameList.RenameItems.Select(entry => entry.Original.FullPath));
        }

        [Fact]
        /// <summary>
        /// Verifies that glob sources resolve files from the parent directory only.
        /// </summary>
        public void AddSource_Resolves_Glob_In_TopDirectoryOnly()
        {
            var (topLevelMatch, _) = TestHelpers.CreateFiles(_tempRoot, "top.txt", "nested/nested.txt");

            var renameList = new RenameList(includeHidden: true);
            var addedCount = renameList.AddSource(_tempRoot.CombinePath("*.txt"));

            Assert.Equal(1, addedCount);
            var entry = Assert.Single(renameList.RenameItems);
            Assert.Equal(topLevelMatch, entry.Original.FullPath);
        }

        [Fact]
        /// <summary>
        /// Verifies that glob and exact-file sources deduplicate to a single resolved item.
        /// </summary>
        public void AddSource_GlobAndExactFile_Deduplicates_ResolvedItem()
        {
            var alphaPath = TestHelpers.CreateFile(_tempRoot, "alpha.txt");

            var renameList = new RenameList(includeHidden: true);
            Assert.Equal(1, renameList.AddSource(_tempRoot.CombinePath("*.txt")));
            Assert.Equal(0, renameList.AddSource(alphaPath));

            var entry = Assert.Single(renameList.RenameItems);
            Assert.Equal(alphaPath, entry.Original.FullPath);
        }

        [Fact]
        /// <summary>
        /// Verifies that recursive glob syntax with ** resolves matches from nested folders.
        /// </summary>
        public void AddSource_Resolves_Recursive_Glob_With_DoubleStar_Syntax()
        {
            var (topLevelMatch, nestedMatch, deeperMatch) = TestHelpers.CreateFiles(
                _tempRoot,
                "top.txt",
                "nested/nested.txt",
                "nested/deeper/deeper.txt");

            var renameList = new RenameList(includeHidden: true);
            var addedCount = renameList.AddSource(_tempRoot.CombinePath("**", "*.txt"));

            Assert.Equal(3, addedCount);
            Assert.Equal(
                [topLevelMatch, nestedMatch, deeperMatch],
                renameList.RenameItems.Select(entry => entry.Original.FullPath));
        }

        [Fact]
        /// <summary>
        /// Verifies that matcher wildcard syntax resolves a nested path pattern with recursive traversal.
        /// </summary>
        public void AddSource_Resolves_Recursive_Nested_Path_Pattern()
        {
            var (nestedMatch, deeperMatch, nonMatch) = TestHelpers.CreateFiles(
                _tempRoot,
                "nested/file1.txt",
                "x/nested/file2.txt",
                "x/other/file3.txt");

            var renameList = new RenameList(includeHidden: true);
            var addedCount = renameList.AddSource(_tempRoot.CombinePath("**", "nested", "*.txt"));

            Assert.Equal(2, addedCount);
            Assert.Equal(
                [nestedMatch, deeperMatch],
                renameList.RenameItems.Select(entry => entry.Original.FullPath));
            Assert.DoesNotContain(nonMatch, renameList.RenameItems.Select(entry => entry.Original.FullPath));
        }

    }

}
