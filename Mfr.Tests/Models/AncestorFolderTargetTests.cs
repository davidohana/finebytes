using Mfr.Filters.Replace;
using Mfr.Models;
using Mfr.Tests.Models.Filters;
using Mfr.Utils;

namespace Mfr.Tests.Models
{
    /// <summary>
    /// Thin integration checks that <see cref="AncestorFolderTarget"/> resolves through filters to <see cref="DirectoryPathAncestor"/>.
    /// </summary>
    public sealed class AncestorFolderTargetTests
    {
        private static ReplacerFilter _Replacer(FilterTarget target, string find, string replacement)
        {
            return new(
                Target: target,
                Options: new ReplacerOptions(
                    Find: find,
                    Replacement: replacement,
                    Mode: ReplacerMode.Literal,
                    CaseSensitive: true,
                    ReplaceAll: false,
                    WholeWord: false));
        }

        /// <summary>
        /// Verifies Replacer preview output matches direct <see cref="M:Mfr.Utils.DirectoryPathAncestor.ReplaceSegment(System.String,System.Int32,System.String)"/> for level 1.
        /// </summary>
        [Fact]
        public void Replacer_WithAncestor_target_matches_DirectoryPathAncestor_replace_segment()
        {
            var root = OperatingSystem.IsWindows() ? @"C:\" : "/";
            var sep = Path.DirectorySeparatorChar;
            var directoryPath = $"{root}a{sep}b{sep}c";
            var expected = DirectoryPathAncestor.ReplaceSegment(directoryPath, level: 1, newSegmentName: "cRenamed");

            var item = FilterTestHelpers.CreateRenameItem(directory: directoryPath);
            var filter = _Replacer(target: new AncestorFolderTarget(Level: 1), find: "c", replacement: "cRenamed");

            filter.Setup();
            filter.Apply(item);

            Assert.Equal(expected, item.Preview.DirectoryPath);
        }

        /// <summary>
        /// Verifies Replacer results stay aligned when the directory path carries a trailing separator.
        /// </summary>
        [Fact]
        public void Replacer_trailing_separator_on_directory_matches_DirectoryPathAncestor()
        {
            var root = OperatingSystem.IsWindows() ? @"C:\" : "/";
            var sep = Path.DirectorySeparatorChar;
            var directoryWithTrail = $"{root}a{sep}b{sep}c{sep}";
            var expected = DirectoryPathAncestor.ReplaceSegment(directoryWithTrail, level: 1, newSegmentName: "cRenamed");

            var item = FilterTestHelpers.CreateRenameItem(directory: directoryWithTrail);
            var filter = _Replacer(target: new AncestorFolderTarget(Level: 1), find: "c", replacement: "cRenamed");

            filter.Setup();
            filter.Apply(item);

            Assert.Equal(expected, item.Preview.DirectoryPath);
        }

        /// <summary>
        /// Verifies invalid ancestor level errors surface through applied filters unchanged.
        /// </summary>
        [Fact]
        public void Replacer_Invalid_level_throws_same_as_DirectoryPathAncestor()
        {
            var path = OperatingSystem.IsWindows()
                ? @"C:\a\b"
                : $"{Path.DirectorySeparatorChar}a{Path.DirectorySeparatorChar}b";
            var item = FilterTestHelpers.CreateRenameItem(directory: path);
            var filter = _Replacer(target: new AncestorFolderTarget(Level: 0), find: "a", replacement: "b");
            filter.Setup();

            var exFilter = Assert.Throws<ArgumentOutOfRangeException>(() => filter.Apply(item));
            var exDirect = Assert.Throws<ArgumentOutOfRangeException>(() =>
                DirectoryPathAncestor.GetSegmentName(path, level: 0));

            Assert.Equal(exDirect.ParamName, exFilter.ParamName);
            Assert.Equal(exDirect.Message, exFilter.Message);
        }

        /// <summary>
        /// Verifies rejected segment names propagate from the ancestor helper through Replacer writes.
        /// </summary>
        [Fact]
        public void Replacer_empty_segment_replacement_throws_matching_DirectoryPathAncestor()
        {
            var directory =
                $"{(OperatingSystem.IsWindows() ? @"C:\" : "/")}a{Path.DirectorySeparatorChar}b";
            var item = FilterTestHelpers.CreateRenameItem(directory: directory);
            var filter = _Replacer(target: new AncestorFolderTarget(Level: 1), find: "b", replacement: "");
            filter.Setup();

            var exFilter = Assert.Throws<ArgumentException>(() => filter.Apply(item));
            var exDirect = Assert.Throws<ArgumentException>(() =>
                DirectoryPathAncestor.ReplaceSegment(directory, level: 1, newSegmentName: ""));

            Assert.Equal(exDirect.ParamName, exFilter.ParamName);
        }
    }
}
