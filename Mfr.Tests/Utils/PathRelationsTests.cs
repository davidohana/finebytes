using Mfr.Utils;

namespace Mfr.Tests.Utils
{
    /// <summary>
    /// Tests for <see cref="PathRelations"/>.
    /// </summary>
    public sealed class PathRelationsTests
    {
        private static string Root => TestPaths.VolumeRoot;

        /// <summary>
        /// Verifies IsSamePath respects host case sensitivity for identical paths.
        /// </summary>
        [Fact]
        public void IsSamePath_identical_paths_returns_true()
        {
            var path = TestPaths.Absolute("a", "b", "c.txt");
            Assert.True(PathRelations.IsSamePath(path, path));
        }

        /// <summary>
        /// Verifies IsSamePath treats case differences according to host filesystem.
        /// </summary>
        [Fact]
        public void IsSamePath_case_only_difference_matches_host_filesystem()
        {
            var lower = TestPaths.Absolute("a", "b", "c.txt");
            var upper = TestPaths.Absolute("a", "b", "C.txt");

            Assert.Equal(OperatingSystem.IsWindows(), PathRelations.IsSamePath(lower, upper));
        }

        /// <summary>
        /// Verifies exact path-text comparison does not trim trailing separators.
        /// </summary>
        [Fact]
        public void IsSamePath_exact_text_trailing_separator_difference_returns_false()
        {
            var without = TestPaths.Absolute("root", "folder");
            var with = without + Path.DirectorySeparatorChar;

            Assert.False(PathRelations.IsSamePath(first: without, second: with, trimTrailingSeparators: false));
            Assert.True(PathRelations.IsSamePath(without, with));
        }

        /// <summary>
        /// Verifies DiffersOnlyInCase reports true only when ordinal text differs but disk identity is the same.
        /// </summary>
        [Fact]
        public void DiffersOnlyInCase_case_only_diff_returns_true_on_windows_only()
        {
            var lower = TestPaths.Absolute("a", "b", "c.txt");
            var upper = TestPaths.Absolute("a", "b", "C.txt");

            Assert.Equal(OperatingSystem.IsWindows(), PathRelations.DiffersOnlyInCase(lower, upper));
        }

        /// <summary>
        /// Verifies identical strings are not flagged as differing in case.
        /// </summary>
        [Fact]
        public void DiffersOnlyInCase_identical_strings_returns_false()
        {
            var path = TestPaths.Absolute("a", "b.txt");
            Assert.False(PathRelations.DiffersOnlyInCase(path, path));
        }

        /// <summary>
        /// Verifies fully different paths are not flagged as case-only differences.
        /// </summary>
        [Fact]
        public void DiffersOnlyInCase_disjoint_paths_returns_false()
        {
            var first = TestPaths.Absolute("a", "b.txt");
            var second = TestPaths.Absolute("a", "c.txt");

            Assert.False(PathRelations.DiffersOnlyInCase(first, second));
        }

        /// <summary>
        /// Verifies trailing-separator-only differences are not treated as case-only renames.
        /// </summary>
        [Fact]
        public void DiffersOnlyInCase_trailing_separator_difference_returns_false()
        {
            var without = TestPaths.Absolute("root", "folder");
            var with = without + Path.DirectorySeparatorChar;

            Assert.False(PathRelations.DiffersOnlyInCase(without, with));
        }

        /// <summary>
        /// Verifies a strict descendant is recognized as such.
        /// </summary>
        [Fact]
        public void IsDescendantOf_strict_child_returns_true()
        {
            var ancestor = TestPaths.Absolute("root", "folder");
            var child = TestPaths.Absolute("root", "folder", "file.txt");

            Assert.True(PathRelations.IsDescendantOf(child, ancestor));
        }

        /// <summary>
        /// Verifies a path equal to the ancestor is not its own descendant.
        /// </summary>
        [Fact]
        public void IsDescendantOf_same_path_returns_false()
        {
            var ancestor = TestPaths.Absolute("root", "folder");
            Assert.False(PathRelations.IsDescendantOf(ancestor, ancestor));
        }

        /// <summary>
        /// Verifies sibling-like prefix collisions are not treated as descendants.
        /// </summary>
        [Fact]
        public void IsDescendantOf_sibling_prefix_returns_false()
        {
            var ancestor = TestPaths.Absolute("foo");
            var notDescendant = TestPaths.Absolute("foobar");

            Assert.False(PathRelations.IsDescendantOf(notDescendant, ancestor));
        }

        /// <summary>
        /// Verifies trailing separators on the ancestor do not influence the result.
        /// </summary>
        [Fact]
        public void IsDescendantOf_trailing_separator_on_ancestor_normalized()
        {
            var ancestor = TestPaths.Absolute("root", "folder") + Path.DirectorySeparatorChar;
            var child = TestPaths.Absolute("root", "folder", "file.txt");

            Assert.True(PathRelations.IsDescendantOf(child, ancestor));
        }

        /// <summary>
        /// Verifies ReplaceAncestor rewrites a descendant path with the new ancestor.
        /// </summary>
        [Fact]
        public void ReplaceAncestor_descendant_rewrites_prefix()
        {
            var oldAncestor = TestPaths.Absolute("root", "old");
            var newAncestor = TestPaths.Absolute("root", "new");
            var path = TestPaths.Absolute("root", "old", "sub", "file.txt");

            var result = PathRelations.ReplaceAncestor(path, oldAncestor, newAncestor);

            Assert.Equal(TestPaths.Absolute("root", "new", "sub", "file.txt"), result);
        }

        /// <summary>
        /// Verifies ReplaceAncestor returns the new ancestor when the path equals the old ancestor.
        /// </summary>
        [Fact]
        public void ReplaceAncestor_same_path_returns_new_ancestor()
        {
            var oldAncestor = TestPaths.Absolute("root", "old");
            var newAncestor = TestPaths.Absolute("root", "new");

            var result = PathRelations.ReplaceAncestor(oldAncestor, oldAncestor, newAncestor);

            Assert.Equal(newAncestor, result);
        }

        /// <summary>
        /// Verifies ReplaceAncestor treats trailing-separator-only differences as the same ancestor path.
        /// </summary>
        [Fact]
        public void ReplaceAncestor_trailing_separator_on_path_returns_new_ancestor()
        {
            var oldAncestor = TestPaths.Absolute("root", "old");
            var newAncestor = TestPaths.Absolute("root", "new");
            var path = oldAncestor + Path.DirectorySeparatorChar;

            var result = PathRelations.ReplaceAncestor(path, oldAncestor, newAncestor);

            Assert.Equal(newAncestor, result);
        }

        /// <summary>
        /// Verifies ReplaceAncestor leaves an unrelated path unchanged.
        /// </summary>
        [Fact]
        public void ReplaceAncestor_unrelated_path_returns_unchanged()
        {
            var oldAncestor = TestPaths.Absolute("root", "old");
            var newAncestor = TestPaths.Absolute("root", "new");
            var path = TestPaths.Absolute("elsewhere", "file.txt");

            var result = PathRelations.ReplaceAncestor(path, oldAncestor, newAncestor);

            Assert.Equal(path, result);
        }

        /// <summary>
        /// Verifies IsSamePath trims trailing separators before comparing by default.
        /// </summary>
        [Fact]
        public void IsSamePath_trailing_separator_difference_returns_true()
        {
            var without = TestPaths.Absolute("root", "folder");
            var with = without + Path.DirectorySeparatorChar;

            Assert.True(PathRelations.IsSamePath(without, with));
        }

        /// <summary>
        /// Verifies IsSamePath directory comparison treats case differences according to the host filesystem.
        /// </summary>
        [Fact]
        public void IsSamePath_directory_case_only_difference_matches_host_filesystem()
        {
            var lower = TestPaths.Absolute("a", "b");
            var upper = TestPaths.Absolute("a", "B");

            Assert.Equal(OperatingSystem.IsWindows(), PathRelations.IsSamePath(lower, upper));
        }

        /// <summary>
        /// Verifies IsFilesystemRoot recognizes volume roots and rejects nested paths.
        /// </summary>
        [Fact]
        public void IsFilesystemRoot_volume_root_true_nested_false()
        {
            Assert.True(PathRelations.IsFilesystemRoot(Root));
            Assert.False(PathRelations.IsFilesystemRoot(TestPaths.Absolute("a", "b")));
        }
    }
}
