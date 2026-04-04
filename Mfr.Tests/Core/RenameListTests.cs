using Mfr.Core;

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
            _tempRoot = Path.Combine(
                Directory.GetCurrentDirectory(),
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
                if (Directory.Exists(_tempRoot))
                {
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
            var alphaPath = Path.Combine(_tempRoot, "alpha.txt");
            var betaPath = Path.Combine(_tempRoot, "beta.log");
            var gammaPath = Path.Combine(_tempRoot, "gamma.txt");
            File.WriteAllText(alphaPath, "a");
            File.WriteAllText(betaPath, "b");
            File.WriteAllText(gammaPath, "c");

            var sources = new[]
            {
                betaPath,
                Path.Combine(_tempRoot, "*.txt"),
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
            var source = Path.Combine(_tempRoot, "alpha.txt");
            File.WriteAllText(source, "a");

            var renameList = new RenameList(includeHidden: true);
            Assert.True(renameList.AddSource(source));
            Assert.True(renameList.AddSource(source));

            _ = Assert.Single(renameList.ResolvedItems);
            Assert.Equal(source, renameList.ResolvedItems[0].FullPath);
        }

        [Fact]
        /// <summary>
        /// Verifies that hidden files are skipped unless hidden inclusion is enabled.
        /// </summary>
        public void AddSource_Filters_Hidden_When_Disabled()
        {
            var visiblePath = Path.Combine(_tempRoot, "visible.txt");
            var hiddenPath = Path.Combine(_tempRoot, "hidden.txt");
            File.WriteAllText(visiblePath, "visible");
            File.WriteAllText(hiddenPath, "hidden");

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
    }
}
