using Mfr.Core;
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
            _ = Directory.CreateDirectory(_tempRoot);
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
            var alphaPath = _tempRoot.CombinePath("alpha.txt");
            var betaPath = _tempRoot.CombinePath("beta.log");
            var gammaPath = _tempRoot.CombinePath("gamma.txt");
            _CreateDummyFile(alphaPath, "a");
            _CreateDummyFile(betaPath, "b");
            _CreateDummyFile(gammaPath, "c");

            var sources = new[]
            {
                betaPath,
                _tempRoot.CombinePath("*.txt"),
                _tempRoot,
            };

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources(sources);
            var entries = renameList.ResolvedItems;

            Assert.Equal(3, entries.Count);
            Assert.Equal(
                [betaPath, alphaPath, gammaPath],
                entries.Select(e => e.FullPath));
            Assert.Equal([0, 1, 2], entries.Select(e => e.GlobalIndex));
            Assert.Equal([0, 1, 2], entries.Select(e => e.FolderOccurrenceIndex));
            Assert.Equal(["beta", "alpha", "gamma"], entries.Select(e => e.Prefix));
            Assert.Equal([".log", ".txt", ".txt"], entries.Select(e => e.Extension));
        }

        [Fact]
        /// <summary>
        /// Verifies that duplicate source additions are allowed, while resolved items stay distinct.
        /// </summary>
        public void AddSource_Allows_Duplicate_Sources_But_ResolvedItems_Are_Deduplicated()
        {
            var source = _tempRoot.CombinePath("alpha.txt");
            _CreateDummyFile(source, "a");

            var renameList = new RenameList(includeHidden: true);
            Assert.Equal(1, renameList.AddSource(source));
            Assert.Equal(0, renameList.AddSource(source));

            _ = Assert.Single(renameList.ResolvedItems);
            Assert.Equal(source, renameList.ResolvedItems[0].FullPath);
        }

        [Fact]
        /// <summary>
        /// Verifies that hidden files are skipped unless hidden inclusion is enabled.
        /// </summary>
        public void AddSource_Filters_Hidden_When_Disabled()
        {
            var visiblePath = _tempRoot.CombinePath("visible.txt");
            var hiddenPath = _tempRoot.CombinePath("hidden.txt");
            _CreateDummyFile(visiblePath, "visible");
            _CreateDummyFile(hiddenPath, "hidden");

            var hiddenAttrs = File.GetAttributes(hiddenPath);
            File.SetAttributes(hiddenPath, hiddenAttrs | FileAttributes.Hidden);

            var excludeHiddenList = new RenameList(includeHidden: false);
            _ = excludeHiddenList.AddSource(hiddenPath);
            _ = excludeHiddenList.AddSource(visiblePath);
            var excludedHidden = excludeHiddenList.ResolvedItems.ToList();

            var includeHiddenList = new RenameList(includeHidden: true);
            _ = includeHiddenList.AddSource(hiddenPath);
            _ = includeHiddenList.AddSource(visiblePath);
            var includedHidden = includeHiddenList.ResolvedItems.ToList();

            _ = Assert.Single(excludedHidden);
            Assert.Equal(visiblePath, excludedHidden[0].FullPath);
            Assert.Equal(1, excludedHidden[0].GlobalIndex);
            Assert.Equal(0, excludedHidden[0].FolderOccurrenceIndex);

            Assert.Equal(2, includedHidden.Count);
            Assert.Equal([hiddenPath, visiblePath], includedHidden.Select(x => x.FullPath));
            Assert.Equal([0, 1], includedHidden.Select(x => x.GlobalIndex));
            Assert.Equal([0, 1], includedHidden.Select(x => x.FolderOccurrenceIndex));
        }

        [Fact]
        /// <summary>
        /// Verifies that glob sources resolve files from the parent directory only.
        /// </summary>
        public void AddSource_Resolves_Glob_In_TopDirectoryOnly()
        {
            var topLevelMatch = _tempRoot.CombinePath("top.txt");
            var nestedDir = _tempRoot.CombinePath("nested");
            var nestedMatch = nestedDir.CombinePath("nested.txt");
            _CreateDummyFile(topLevelMatch, "top");
            _CreateDummyFile(nestedMatch, "nested");

            var renameList = new RenameList(includeHidden: true);
            var addedCount = renameList.AddSource(_tempRoot.CombinePath("*.txt"));

            Assert.Equal(1, addedCount);
            var entry = Assert.Single(renameList.ResolvedItems);
            Assert.Equal(topLevelMatch, entry.FullPath);
        }

        [Fact]
        /// <summary>
        /// Verifies that glob and exact-file sources deduplicate to a single resolved item.
        /// </summary>
        public void AddSource_GlobAndExactFile_Deduplicates_ResolvedItem()
        {
            var alphaPath = _tempRoot.CombinePath("alpha.txt");
            _CreateDummyFile(alphaPath, "a");

            var renameList = new RenameList(includeHidden: true);
            Assert.Equal(1, renameList.AddSource(_tempRoot.CombinePath("*.txt")));
            Assert.Equal(0, renameList.AddSource(alphaPath));

            var entry = Assert.Single(renameList.ResolvedItems);
            Assert.Equal(alphaPath, entry.FullPath);
        }

        private static void _CreateDummyFile(string path, string contents = "dummy")
        {
            var parentDirectory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(parentDirectory))
            {
                _ = Directory.CreateDirectory(parentDirectory);
            }

            File.WriteAllText(path, contents);
        }
    }

}
