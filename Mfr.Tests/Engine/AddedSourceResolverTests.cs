using Mfr.Utils;

namespace Mfr.Tests.Engine
{
    /// <summary>
    /// Tests <see cref="AddedSourceResolver"/> in isolation from <see cref="RenameList"/>.
    /// </summary>
    public class AddedSourceResolverTests : IDisposable
    {
        private readonly string _tempRoot;

        /// <summary>
        /// Initializes a new test instance with an isolated temporary directory under the current workspace.
        /// </summary>
        public AddedSourceResolverTests()
        {
            _tempRoot = Directory
                .GetCurrentDirectory()
                .CombinePath("mfr_sourcesolver_tests_" + Guid.NewGuid().ToString("N"));
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
        /// Verifies that a missing parent directory yields <see cref="UserException"/>.
        /// </summary>
        public void Resolve_MissingParentDirectory_ThrowsUserException()
        {
            var missingParent = _tempRoot.CombinePath("not_created", "child");
            var source = missingParent.CombinePath("file.txt");

            var ex = Assert.Throws<UserException>(() =>
                AddedSourceResolver
                    .ResolveToPaths(source: source, includeFiles: true, includeFolders: true, includeSubdirs: false)
                    .ToList()
            );

            Assert.Contains("does not exist", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        /// <summary>
        /// Verifies that a non-existent file under an existing directory resolves to an empty sequence.
        /// </summary>
        public void Resolve_MissingExactFile_ReturnsEmpty()
        {
            var paths = AddedSourceResolver
                .ResolveToPaths(
                    source: _tempRoot.CombinePath("definitely_missing.bin"),
                    includeFiles: true,
                    includeFolders: true,
                    includeSubdirs: false
                )
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

            var paths = AddedSourceResolver
                .ResolveToPaths(source: filePath, includeFiles: true, includeFolders: true, includeSubdirs: false)
                .ToList();

            Assert.Equal([filePath], paths);
        }

        [Fact]
        /// <summary>
        /// Verifies that a directory source with folder inclusion yields the directory path only when files are disabled.
        /// </summary>
        public void Resolve_Directory_WithIncludeFolders_ReturnsDirectoryPath()
        {
            var folderPath = Directory.CreateDirectory(_tempRoot.CombinePath("Album")).FullName;
            TestHelpers.CreateFile(folderPath, "inside.txt");

            var paths = AddedSourceResolver
                .ResolveToPaths(source: folderPath, includeFiles: false, includeFolders: true, includeSubdirs: false)
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

            var paths = AddedSourceResolver
                .ResolveToPaths(source: folderPath, includeFiles: true, includeFolders: false, includeSubdirs: false)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var expected = new[] { topA, topB }.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
            Assert.Equal(expected, paths);
        }

        [Fact]
        /// <summary>
        /// Verifies that a folder source without recursion returns the folder and its top-level files only.
        /// </summary>
        public void Resolve_Directory_WithoutSubdirs_ReturnsFolderAndTopLevelFiles()
        {
            var folderPath = Directory.CreateDirectory(_tempRoot.CombinePath("Album")).FullName;
            var topFile = TestHelpers.CreateFile(folderPath, "track.mp3");
            var childFolder = Directory.CreateDirectory(folderPath.CombinePath("Disc1")).FullName;
            TestHelpers.CreateFile(childFolder, "nested.mp3");

            var paths = AddedSourceResolver
                .ResolveToPaths(source: folderPath, includeFiles: true, includeFolders: true, includeSubdirs: false)
                .ToList();

            Assert.Equal([folderPath, topFile], paths);
            Assert.DoesNotContain(childFolder, paths);
        }

        [Fact]
        /// <summary>
        /// Verifies that full recursion returns nested folders and files while keeping the explicit source folder first.
        /// </summary>
        public void Resolve_Directory_FullRecursion_ReturnsNestedFoldersAndFiles()
        {
            var folderPath = Directory.CreateDirectory(_tempRoot.CombinePath("Album")).FullName;
            var topFile = TestHelpers.CreateFile(folderPath, "readme.txt");
            var childFolder = Directory.CreateDirectory(folderPath.CombinePath("Disc1")).FullName;
            var nestedFile = TestHelpers.CreateFile(childFolder, "track.mp3");

            var paths = AddedSourceResolver
                .ResolveToPaths(source: folderPath, includeFiles: true, includeFolders: true, includeSubdirs: true)
                .ToList();

            Assert.Equal(folderPath, paths[0]);
            Assert.Equal(
                new[] { folderPath, topFile, childFolder, nestedFile }.OrderBy(
                    static path => path,
                    StringComparer.OrdinalIgnoreCase
                ),
                paths.OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            );
        }

        [Fact]
        /// <summary>
        /// Verifies that folders-only without recursion returns the explicit source folder only.
        /// </summary>
        public void Resolve_Directory_FoldersOnly_WithoutSubdirs_ReturnsSourceFolder()
        {
            var folderPath = Directory.CreateDirectory(_tempRoot.CombinePath("Album")).FullName;
            Directory.CreateDirectory(folderPath.CombinePath("Disc1"));
            TestHelpers.CreateFile(folderPath, "track.mp3");

            var paths = AddedSourceResolver
                .ResolveToPaths(source: folderPath, includeFiles: false, includeFolders: true, includeSubdirs: false)
                .ToList();

            Assert.Equal([folderPath], paths);
        }

        [Fact]
        /// <summary>
        /// Verifies that folders-only with recursion returns the source folder and descendant folders.
        /// </summary>
        public void Resolve_Directory_FoldersOnly_WithSubdirs_AddsNestedFolders()
        {
            var folderPath = Directory.CreateDirectory(_tempRoot.CombinePath("Album")).FullName;
            var childFolder = Directory.CreateDirectory(folderPath.CombinePath("Disc1")).FullName;
            TestHelpers.CreateFile(folderPath, "track.mp3");
            TestHelpers.CreateFile(childFolder, "nested.mp3");

            var paths = AddedSourceResolver
                .ResolveToPaths(source: folderPath, includeFiles: false, includeFolders: true, includeSubdirs: true)
                .ToList();

            Assert.Equal(folderPath, paths[0]);
            Assert.Equal(
                new[] { folderPath, childFolder }.OrderBy(static path => path, StringComparer.OrdinalIgnoreCase),
                paths.OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            );
        }

        [Fact]
        /// <summary>
        /// Verifies that a last-segment mask filters discovered names and still adds the explicit folder.
        /// </summary>
        public void Resolve_DirectoryMask_AppliesIncludeAndExcludeMasksToDiscoveredEntries()
        {
            var folderPath = Directory.CreateDirectory(_tempRoot.CombinePath("Album")).FullName;
            TestHelpers.CreateFile(folderPath, "keep.mp3");
            var skipFile = TestHelpers.CreateFile(folderPath, "skip.mp3");
            Directory.CreateDirectory(folderPath.CombinePath("Disc1"));
            TestHelpers.CreateFile(folderPath.CombinePath("Disc1"), "nested.mp3");

            var paths = AddedSourceResolver
                .ResolveToPaths(
                    source: folderPath.CombinePath("*.mp3"),
                    includeFiles: true,
                    includeFolders: true,
                    includeSubdirs: false,
                    excludeMasks: ["keep.*"]
                )
                .ToList();

            Assert.Equal([folderPath, skipFile], paths);
        }

        [Fact]
        /// <summary>
        /// Verifies that the explicit source folder is included even when its name does not match the mask.
        /// </summary>
        public void Resolve_DirectoryMask_ExplicitFolderBypassesIncludeMask()
        {
            var folderPath = Directory.CreateDirectory(_tempRoot.CombinePath("Album.v2")).FullName;
            TestHelpers.CreateFile(folderPath, "track.mp3");

            var paths = AddedSourceResolver
                .ResolveToPaths(
                    source: folderPath.CombinePath("*.mp3"),
                    includeFiles: true,
                    includeFolders: true,
                    includeSubdirs: false
                )
                .ToList();

            Assert.Equal(folderPath, paths[0]);
        }

        [Fact]
        /// <summary>
        /// Verifies that a last-segment mask with folders disabled matches top-level files only.
        /// </summary>
        public void Resolve_DirectoryMask_FoldersDisabled_MatchesTopDirectoryOnly()
        {
            var top = TestHelpers.CreateFile(_tempRoot, "keep.txt");
            TestHelpers.CreateFile(_tempRoot.CombinePath("nested"), "skip.txt");

            var paths = AddedSourceResolver
                .ResolveToPaths(
                    source: _tempRoot.CombinePath("*.txt"),
                    includeFiles: true,
                    includeFolders: false,
                    includeSubdirs: false
                )
                .ToList();

            Assert.Equal([top], paths);
        }

        [Fact]
        /// <summary>
        /// Verifies that recursion matches nested files even when parent folder names do not match the include mask.
        /// </summary>
        public void Resolve_DirectoryMask_Recursive_MatchesNestedFiles_WhenParentFolderDoesNotMatchMask()
        {
            var folderPath = Directory.CreateDirectory(_tempRoot.CombinePath("Album")).FullName;
            var nested = TestHelpers.CreateFile(folderPath.CombinePath("Disc1"), "track.mp3");
            TestHelpers.CreateFile(folderPath, "readme.txt");

            var paths = AddedSourceResolver
                .ResolveToPaths(
                    source: folderPath.CombinePath("*.mp3"),
                    includeFiles: true,
                    includeFolders: true,
                    includeSubdirs: true
                )
                .ToList();

            Assert.Equal(folderPath, paths[0]);
            Assert.Equal([folderPath, nested], paths);
        }

        [Fact]
        /// <summary>
        /// Verifies that a last-segment mask with recursion includes nested files matching the mask.
        /// </summary>
        public void Resolve_DirectoryMask_FoldersDisabled_Recursive_MatchesNestedFiles()
        {
            var top = TestHelpers.CreateFile(_tempRoot, "keep.txt");
            var nested = TestHelpers.CreateFile(_tempRoot.CombinePath("nested"), "skip.txt");

            var paths = AddedSourceResolver
                .ResolveToPaths(
                    source: _tempRoot.CombinePath("*.txt"),
                    includeFiles: true,
                    includeFolders: false,
                    includeSubdirs: true
                )
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var expected = new[] { top, nested }.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
            Assert.Equal(expected, paths);
        }

        [Fact]
        /// <summary>
        /// Verifies that exclude masks still apply when the include mask is in the source.
        /// </summary>
        public void Resolve_DirectoryMask_AppliesExcludeMasks()
        {
            TestHelpers.CreateFile(_tempRoot, "keep.txt");
            var skip = TestHelpers.CreateFile(_tempRoot, "skip.txt");

            var paths = AddedSourceResolver
                .ResolveToPaths(
                    source: _tempRoot.CombinePath("*.txt"),
                    includeFiles: true,
                    includeFolders: false,
                    includeSubdirs: false,
                    excludeMasks: ["keep.*"]
                )
                .ToList();

            Assert.Equal([skip], paths);
        }

        [Fact]
        /// <summary>
        /// Verifies that a missing parent for a last-segment mask yields <see cref="UserException"/>.
        /// </summary>
        public void Resolve_DirectoryMask_MissingParent_ThrowsUserException()
        {
            var source = _tempRoot.CombinePath("absent_subdir", "*.txt");

            var ex = Assert.Throws<UserException>(() =>
                AddedSourceResolver
                    .ResolveToPaths(source: source, includeFiles: true, includeFolders: false, includeSubdirs: false)
                    .ToList()
            );

            Assert.Contains("does not exist", ex.Message, StringComparison.Ordinal);
            Assert.Contains("absent_subdir", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        /// <summary>
        /// Verifies that <c>**</c> glob syntax is rejected in favor of recursive directory expansion.
        /// </summary>
        public void Resolve_DoubleStarGlob_ThrowsUserException()
        {
            var source = _tempRoot.CombinePath("**", "*.txt");

            var ex = Assert.Throws<UserException>(() =>
                AddedSourceResolver
                    .ResolveToPaths(source: source, includeFiles: true, includeFolders: false, includeSubdirs: false)
                    .ToList()
            );

            Assert.Contains("**", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        /// <summary>
        /// Verifies that wildcards are rejected in any path segment except the last.
        /// </summary>
        public void Resolve_WildcardInNonLastSegment_ThrowsUserException()
        {
            var source = _tempRoot.CombinePath("*", "file.txt");

            var ex = Assert.Throws<UserException>(() =>
                AddedSourceResolver
                    .ResolveToPaths(source: source, includeFiles: true, includeFolders: false, includeSubdirs: false)
                    .ToList()
            );

            Assert.Contains("last path segment", ex.Message, StringComparison.Ordinal);
        }
    }
}
