using Mfr.Core;
using Mfr.Models;

namespace Mfr.Tests.Core
{
    /// <summary>
    /// Tests scanning and file-entry conversion behavior in <see cref="FileScanner"/>.
    /// </summary>
    public class FileScannerTests : IDisposable
    {
        private readonly string _tempRoot;

        /// <summary>
        /// Initializes a new test instance with an isolated temporary directory under the current workspace.
        /// </summary>
        public FileScannerTests()
        {
            _tempRoot = Path.Combine(
                Directory.GetCurrentDirectory(),
                "mfr_filescanner_tests_" + Guid.NewGuid().ToString("N"));
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
        /// Verifies that mixed source types are expanded and deduplicated into ordered entries.
        /// </summary>
        public void ScanSources_Resolves_Mixed_Sources_Into_Distinct_Entries()
        {
            var alphaPath = Path.Combine(_tempRoot, "alpha.txt");
            var betaPath = Path.Combine(_tempRoot, "beta.log");
            var gammaPath = Path.Combine(_tempRoot, "gamma.txt");
            File.WriteAllText(alphaPath, "a");
            File.WriteAllText(betaPath, "b");
            File.WriteAllText(gammaPath, "c");

            var sources = new[]
            {
                Path.Combine(_tempRoot, "*.txt"),
                betaPath,
                _tempRoot
            };

            var entries = FileScanner.ScanSources(sources, includeHidden: true);

            Assert.Equal(3, entries.Count);
            Assert.Equal(
                [alphaPath, betaPath, gammaPath],
                entries.Select(e => e.FullPath).ToArray());
            Assert.Equal([0, 1, 2], entries.Select(e => e.GlobalIndex).ToArray());
            Assert.Equal([0, 1, 2], entries.Select(e => e.FolderOccurrenceIndex).ToArray());
            Assert.Equal(["alpha", "beta", "gamma"], entries.Select(e => e.Prefix).ToArray());
            Assert.Equal([".txt", ".log", ".txt"], entries.Select(e => e.Extension).ToArray());
        }

        [Fact]
        /// <summary>
        /// Verifies that hidden files are skipped unless hidden inclusion is enabled.
        /// </summary>
        public void ToFileEntryLiteList_Filters_Hidden_When_Disabled()
        {
            var visiblePath = Path.Combine(_tempRoot, "visible.txt");
            var hiddenPath = Path.Combine(_tempRoot, "hidden.txt");
            File.WriteAllText(visiblePath, "visible");
            File.WriteAllText(hiddenPath, "hidden");

            var hiddenAttrs = File.GetAttributes(hiddenPath);
            File.SetAttributes(hiddenPath, hiddenAttrs | FileAttributes.Hidden);

            var orderedPaths = new List<string> { hiddenPath, visiblePath };

            var excludedHidden = FileScanner.ToFileEntryLiteList(orderedPaths, includeHidden: false);
            var includedHidden = FileScanner.ToFileEntryLiteList(orderedPaths, includeHidden: true);

            Assert.Single(excludedHidden);
            Assert.Equal(visiblePath, excludedHidden[0].FullPath);
            Assert.Equal(1, excludedHidden[0].GlobalIndex);
            Assert.Equal(0, excludedHidden[0].FolderOccurrenceIndex);

            Assert.Equal(2, includedHidden.Count);
            Assert.Equal([hiddenPath, visiblePath], includedHidden.Select(x => x.FullPath).ToArray());
            Assert.Equal([0, 1], includedHidden.Select(x => x.GlobalIndex).ToArray());
            Assert.Equal([0, 1], includedHidden.Select(x => x.FolderOccurrenceIndex).ToArray());
        }
    }
}
