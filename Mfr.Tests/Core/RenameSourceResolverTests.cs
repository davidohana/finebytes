using Mfr.Core;
using Mfr.Models;
using Mfr.Tests.TestSupport;
using Mfr.Utils;

namespace Mfr.Tests.Core
{
    /// <summary>
    /// Tests <see cref="RenameSourceResolver"/> in isolation from <see cref="RenameList"/>.
    /// </summary>
    public class RenameSourceResolverTests : IDisposable
    {
        private readonly string _tempRoot;

        /// <summary>
        /// Initializes a new test instance with an isolated temporary directory under the current workspace.
        /// </summary>
        public RenameSourceResolverTests()
        {
            _tempRoot = Directory.GetCurrentDirectory().CombinePath(
                "mfr_sourcesolver_tests_" + Guid.NewGuid().ToString("N"));
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
        /// Verifies that a missing parent directory yields <see cref="UserException"/>.
        /// </summary>
        public void Resolve_MissingParentDirectory_ThrowsUserException()
        {
            var missingParent = _tempRoot.CombinePath("not_created", "child");
            var source = missingParent.CombinePath("file.txt");

            var ex = Assert.Throws<UserException>(() =>
                RenameSourceResolver.Resolve(
                        source: source,
                        includeFolders: true,
                        recursiveDirectoryFileAdd: false)
                    .ToList());

            Assert.Contains("does not exist", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        /// <summary>
        /// Verifies that a non-existent file under an existing directory resolves to an empty sequence.
        /// </summary>
        public void Resolve_MissingExactFile_ReturnsEmpty()
        {
            var paths = RenameSourceResolver.Resolve(
                    source: _tempRoot.CombinePath("definitely_missing.bin"),
                    includeFolders: true,
                    recursiveDirectoryFileAdd: false)
                .ToList();

            Assert.Empty(paths);
        }

        [Fact]
        /// <summary>
        /// Verifies that an existing file resolves to that path only.
        /// </summary>
        public void Resolve_ExactFile_ReturnsSinglePath()
        {
            var filePath = TestHelpers.CreateFile(_tempRoot, "single.txt");

            var paths = RenameSourceResolver.Resolve(
                    source: filePath,
                    includeFolders: true,
                    recursiveDirectoryFileAdd: false)
                .ToList();

            Assert.Equal([filePath], paths);
        }

        [Fact]
        /// <summary>
        /// Verifies that a directory source with folder inclusion yields the directory path.
        /// </summary>
        public void Resolve_Directory_WithIncludeFolders_ReturnsDirectoryPath()
        {
            var folderPath = Directory.CreateDirectory(_tempRoot.CombinePath("Album")).FullName;

            var paths = RenameSourceResolver.Resolve(
                    source: folderPath,
                    includeFolders: true,
                    recursiveDirectoryFileAdd: false)
                .ToList();

            Assert.Equal([folderPath], paths);
        }

        [Fact]
        /// <summary>
        /// Verifies that a directory source without folder inclusion enumerates top-level files only.
        /// </summary>
        public void Resolve_Directory_FoldersDisabled_TopLevelFilesOnly()
        {
            var folderPath = Directory.CreateDirectory(_tempRoot.CombinePath("Album")).FullName;
            var topA = TestHelpers.CreateFile(folderPath, "a.txt");
            var topB = TestHelpers.CreateFile(folderPath, "b.log");
            TestHelpers.CreateFile(folderPath.CombinePath("Sub"), "nested.txt");

            var paths = RenameSourceResolver.Resolve(
                    source: folderPath,
                    includeFolders: false,
                    recursiveDirectoryFileAdd: false)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var expected = new[] { topA, topB }
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();
            Assert.Equal(expected, paths);
        }

        [Fact]
        /// <summary>
        /// Verifies that a simple filename wildcard resolves against the parent directory only.
        /// </summary>
        public void Resolve_SimpleFilenameGlob_MatchesTopDirectoryOnly()
        {
            var top = TestHelpers.CreateFile(_tempRoot, "keep.txt");
            TestHelpers.CreateFile(_tempRoot.CombinePath("nested"), "skip.txt");

            var paths = RenameSourceResolver.Resolve(
                    source: _tempRoot.CombinePath("*.txt"),
                    includeFolders: true,
                    recursiveDirectoryFileAdd: false)
                .ToList();

            Assert.Equal([top], paths);
        }

        [Fact]
        /// <summary>
        /// Verifies that a multi-segment glob with a missing base directory yields <see cref="UserException"/>.
        /// </summary>
        public void Resolve_MultiSegmentGlob_MissingBaseDirectory_ThrowsUserException()
        {
            var source = _tempRoot.CombinePath("absent_subdir", "**", "*.txt");

            var ex = Assert.Throws<UserException>(() =>
                RenameSourceResolver.Resolve(
                        source: source,
                        includeFolders: true,
                        recursiveDirectoryFileAdd: false)
                    .ToList());

            Assert.Contains("does not exist", ex.Message, StringComparison.Ordinal);
            Assert.Contains("absent_subdir", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
