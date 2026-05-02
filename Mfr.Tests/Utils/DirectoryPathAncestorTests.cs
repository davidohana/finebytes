using Mfr.Utils;

namespace Mfr.Tests.Utils
{
    /// <summary>
    /// Tests for <see cref="DirectoryPathAncestor"/>.
    /// </summary>
    public sealed class DirectoryPathAncestorTests
    {
        private static string Root => OperatingSystem.IsWindows() ? @"C:\" : "/";

        /// <summary>
        /// Verifies level-1 read returns immediate folder name.
        /// </summary>
        [Fact]
        public void GetSegmentName_level_1_Returns_leaf_directory_name()
        {
            var sep = Path.DirectorySeparatorChar;
            var path = $"{Root}a{sep}b{sep}c";

            Assert.Equal("c", DirectoryPathAncestor.GetSegmentName(path, level: 1));
            Assert.Equal("b", DirectoryPathAncestor.GetSegmentName(path, level: 2));
            Assert.Equal("a", DirectoryPathAncestor.GetSegmentName(path, level: 3));
        }

        /// <summary>
        /// Verifies level-6 read on matching depth paths.
        /// </summary>
        [Fact]
        public void GetSegmentName_deep_path_Returns_far_ancestor_name()
        {
            var sep = Path.DirectorySeparatorChar;
            var path = $"{Root}s1{sep}s2{sep}s3{sep}s4{sep}s5{sep}s6";

            Assert.Equal("s1", DirectoryPathAncestor.GetSegmentName(path, level: 6));
        }

        /// <summary>
        /// Verifies trailing directory separators do not hide the leaf segment name.
        /// </summary>
        [Fact]
        public void GetSegmentName_trimmed_trailing_separator_Matches_normalized_path_behavior()
        {
            var sep = Path.DirectorySeparatorChar;
            var pathWithTrailing = $"{Root}a{sep}b{sep}c{sep}";
            var pathTrimmed = $"{Root}a{sep}b{sep}c";

            Assert.Equal("c", DirectoryPathAncestor.GetSegmentName(pathWithTrailing, level: 1));
            Assert.Equal(
                DirectoryPathAncestor.ReplaceSegment(pathWithTrailing, level: 1, newSegmentName: "cn"),
                DirectoryPathAncestor.ReplaceSegment(pathTrimmed, level: 1, newSegmentName: "cn"));
        }

        /// <summary>
        /// Verifies level-1 replace rewrites closest ancestor folder segment.
        /// </summary>
        [Fact]
        public void ReplaceSegment_level_1_Rewrites_leaf_directory_segment()
        {
            var sep = Path.DirectorySeparatorChar;
            var path = $"{Root}a{sep}b{sep}c";

            var result = DirectoryPathAncestor.ReplaceSegment(path, level: 1, newSegmentName: "cRenamed");

            Assert.Equal($"{Root}a{sep}b{sep}cRenamed", result);
        }

        /// <summary>
        /// Verifies level-2 replace rewrites intermediate segment.
        /// </summary>
        [Fact]
        public void ReplaceSegment_level_2_Rewrites_parent_segment()
        {
            var sep = Path.DirectorySeparatorChar;
            var path = $"{Root}a{sep}b{sep}c";

            var result = DirectoryPathAncestor.ReplaceSegment(path, level: 2, newSegmentName: "bRenamed");

            Assert.Equal($"{Root}a{sep}bRenamed{sep}c", result);
        }

        /// <summary>
        /// Verifies the deepest level swaps the farthest-named segment toward the volume root.
        /// </summary>
        [Fact]
        public void ReplaceSegment_level_matching_depth_Replaces_far_ancestor_segment()
        {
            var sep = Path.DirectorySeparatorChar;
            var path = $"{Root}s1{sep}s2{sep}s3{sep}s4{sep}s5{sep}s6";

            var result = DirectoryPathAncestor.ReplaceSegment(path, level: 6, newSegmentName: "s1Renamed");

            Assert.Equal($"{Root}s1Renamed{sep}s2{sep}s3{sep}s4{sep}s5{sep}s6", result);
        }

        /// <summary>
        /// Verifies shallow paths throw instead of yielding an empty ancestor name.
        /// </summary>
        [Fact]
        public void GetSegmentName_insufficient_depth_Throws_InvalidOperation()
        {
            var pathOnly = OperatingSystem.IsWindows() ? @"C:\only" : "/only";

            var ex = Assert.Throws<InvalidOperationException>(() => DirectoryPathAncestor.GetSegmentName(pathOnly, level: 2));
            Assert.Contains("level", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies replace with insufficient ancestor depth fails.
        /// </summary>
        [Fact]
        public void ReplaceSegment_insufficient_depth_Throws_InvalidOperation()
        {
            var pathOnly = OperatingSystem.IsWindows() ? @"C:\only" : "/only";

            var ex = Assert.Throws<InvalidOperationException>(() =>
                DirectoryPathAncestor.ReplaceSegment(pathOnly, level: 2, newSegmentName: "x"));
            Assert.Contains("level", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies non-positive levels are rejected on read and write.
        /// </summary>
        [Fact]
        public void GetSegmentName_non_positive_level_Throws_ArgumentOutOfRange()
        {
            var path = OperatingSystem.IsWindows()
                ? @"C:\a\b"
                : $"{Path.DirectorySeparatorChar}a{Path.DirectorySeparatorChar}b";

            foreach (var badLevel in new[] { 0, -1, -99 })
            {
                var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                    DirectoryPathAncestor.GetSegmentName(path, badLevel));
                Assert.Equal("level", ex.ParamName);
            }
        }

        /// <summary>
        /// Verifies replace rejects non-positive level.
        /// </summary>
        [Fact]
        public void ReplaceSegment_non_positive_level_Throws_ArgumentOutOfRange()
        {
            var path =
                $"{(OperatingSystem.IsWindows() ? @"C:\" : "/")}a{Path.DirectorySeparatorChar}b";

            foreach (var badLevel in new[] { 0, -1, -99 })
            {
                var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                    DirectoryPathAncestor.ReplaceSegment(path, badLevel, newSegmentName: "x"));
                Assert.Equal("level", ex.ParamName);
            }
        }

        /// <summary>
        /// Verifies replace rejects empty replacement segment names.
        /// </summary>
        [Fact]
        public void ReplaceSegment_empty_NewSegment_throws_argument()
        {
            var directory =
                $"{(OperatingSystem.IsWindows() ? @"C:\" : "/")}a{Path.DirectorySeparatorChar}b";

            var ex = Assert.Throws<ArgumentException>(() =>
                DirectoryPathAncestor.ReplaceSegment(directory, level: 1, newSegmentName: ""));
            Assert.Equal("newSegmentName", ex.ParamName);
        }
    }
}
