using Mfr.Utils;

namespace Mfr.Tests.Utils
{
    /// <summary>
    /// Tests for <see cref="PathRelations"/>.
    /// </summary>
    public sealed class PathRelationsTests
    {
        private static string Root => OperatingSystem.IsWindows() ? @"C:\" : "/";
        private static char Sep => Path.DirectorySeparatorChar;

        /// <summary>
        /// Verifies SameOnDisk respects host case sensitivity for identical paths.
        /// </summary>
        [Fact]
        public void SameOnDisk_identical_paths_returns_true()
        {
            var path = $"{Root}a{Sep}b{Sep}c.txt";
            Assert.True(PathRelations.SameOnDisk(path, path));
        }

        /// <summary>
        /// Verifies SameOnDisk treats case differences according to host filesystem.
        /// </summary>
        [Fact]
        public void SameOnDisk_case_only_difference_matches_host_filesystem()
        {
            var lower = $"{Root}a{Sep}b{Sep}c.txt";
            var upper = $"{Root}a{Sep}b{Sep}C.txt";

            Assert.Equal(OperatingSystem.IsWindows(), PathRelations.SameOnDisk(lower, upper));
        }

        /// <summary>
        /// Verifies DiffersOnlyInCase reports true only when ordinal text differs but disk identity is the same.
        /// </summary>
        [Fact]
        public void DiffersOnlyInCase_case_only_diff_returns_true_on_windows_only()
        {
            var lower = $"{Root}a{Sep}b{Sep}c.txt";
            var upper = $"{Root}a{Sep}b{Sep}C.txt";

            Assert.Equal(OperatingSystem.IsWindows(), PathRelations.DiffersOnlyInCase(lower, upper));
        }

        /// <summary>
        /// Verifies identical strings are not flagged as differing in case.
        /// </summary>
        [Fact]
        public void DiffersOnlyInCase_identical_strings_returns_false()
        {
            var path = $"{Root}a{Sep}b.txt";
            Assert.False(PathRelations.DiffersOnlyInCase(path, path));
        }

        /// <summary>
        /// Verifies fully different paths are not flagged as case-only differences.
        /// </summary>
        [Fact]
        public void DiffersOnlyInCase_disjoint_paths_returns_false()
        {
            var first = $"{Root}a{Sep}b.txt";
            var second = $"{Root}a{Sep}c.txt";

            Assert.False(PathRelations.DiffersOnlyInCase(first, second));
        }

        /// <summary>
        /// Verifies a strict descendant is recognized as such.
        /// </summary>
        [Fact]
        public void IsDescendantOf_strict_child_returns_true()
        {
            var ancestor = $"{Root}root{Sep}folder";
            var child = $"{Root}root{Sep}folder{Sep}file.txt";

            Assert.True(PathRelations.IsDescendantOf(child, ancestor));
        }

        /// <summary>
        /// Verifies a path equal to the ancestor is not its own descendant.
        /// </summary>
        [Fact]
        public void IsDescendantOf_same_path_returns_false()
        {
            var ancestor = $"{Root}root{Sep}folder";
            Assert.False(PathRelations.IsDescendantOf(ancestor, ancestor));
        }

        /// <summary>
        /// Verifies sibling-like prefix collisions are not treated as descendants.
        /// </summary>
        [Fact]
        public void IsDescendantOf_sibling_prefix_returns_false()
        {
            var ancestor = $"{Root}foo";
            var notDescendant = $"{Root}foobar";

            Assert.False(PathRelations.IsDescendantOf(notDescendant, ancestor));
        }

        /// <summary>
        /// Verifies trailing separators on the ancestor do not influence the result.
        /// </summary>
        [Fact]
        public void IsDescendantOf_trailing_separator_on_ancestor_normalized()
        {
            var ancestor = $"{Root}root{Sep}folder{Sep}";
            var child = $"{Root}root{Sep}folder{Sep}file.txt";

            Assert.True(PathRelations.IsDescendantOf(child, ancestor));
        }

        /// <summary>
        /// Verifies ReplaceAncestor rewrites a descendant path with the new ancestor.
        /// </summary>
        [Fact]
        public void ReplaceAncestor_descendant_rewrites_prefix()
        {
            var oldAncestor = $"{Root}root{Sep}old";
            var newAncestor = $"{Root}root{Sep}new";
            var path = $"{Root}root{Sep}old{Sep}sub{Sep}file.txt";

            var result = PathRelations.ReplaceAncestor(path, oldAncestor, newAncestor);

            Assert.Equal($"{Root}root{Sep}new{Sep}sub{Sep}file.txt", result);
        }

        /// <summary>
        /// Verifies ReplaceAncestor returns the new ancestor when the path equals the old ancestor.
        /// </summary>
        [Fact]
        public void ReplaceAncestor_same_path_returns_new_ancestor()
        {
            var oldAncestor = $"{Root}root{Sep}old";
            var newAncestor = $"{Root}root{Sep}new";

            var result = PathRelations.ReplaceAncestor(oldAncestor, oldAncestor, newAncestor);

            Assert.Equal(newAncestor, result);
        }

        /// <summary>
        /// Verifies ReplaceAncestor leaves an unrelated path unchanged.
        /// </summary>
        [Fact]
        public void ReplaceAncestor_unrelated_path_returns_unchanged()
        {
            var oldAncestor = $"{Root}root{Sep}old";
            var newAncestor = $"{Root}root{Sep}new";
            var path = $"{Root}elsewhere{Sep}file.txt";

            var result = PathRelations.ReplaceAncestor(path, oldAncestor, newAncestor);

            Assert.Equal(path, result);
        }
    }
}
