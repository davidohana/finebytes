using Mfr.Filters.Formatting;
using Mfr.Utils;

namespace Mfr.Tests.Models.Filters.Formatting
{
    /// <summary>
    /// Tests for <see cref="FormatterFilter"/>.
    /// </summary>
    public class FormatterFilterTests
    {
        private static readonly FileFullNameTarget _target = new();

        /// <summary>
        /// Verifies file-name token substitution.
        /// </summary>
        [Fact]
        public void Apply_FileNameToken_UsesPrefix()
        {
            var f = new FormatterFilter(_target, new FormatterOptions("<file-name>"));
            Assert.Equal("song", FilterTestHelpers.ApplyToFileName(f, "song"));
        }

        /// <summary>
        /// Verifies counter token with global index.
        /// </summary>
        [Fact]
        public void Apply_CounterToken_UsesGlobalIndex()
        {
            var f = new FormatterFilter(
                _target,
                new FormatterOptions("<counter:initial=10,step=2,padding=fixed,length=4,resetScope=global>")
            );
            Assert.Equal("0016", FilterTestHelpers.ApplyToFileName(f, "ignored", renameListIndex: 3));
        }

        /// <summary>
        /// Verifies parent-folder token.
        /// </summary>
        [Fact]
        public void Apply_ParentFolderToken_UsesDirectoryName()
        {
            var f = new FormatterFilter(_target, new FormatterOptions("<parent-folder>"));
            Assert.Equal(
                "My Album",
                FilterTestHelpers.ApplyToFileName(f, "ignored", directory: "Music".CombinePath("My Album"))
            );
        }

        /// <summary>
        /// Full path target picks up formatter template output (preview-based tokens plus literals).
        /// </summary>
        [Fact]
        public void Apply_FullPathTarget_SetsPreviewFromTemplate()
        {
            var staging = TestPaths.Absolute("Staging");
            var template = Path.Combine(staging, "<full-name>");
            var f = new FormatterFilter(new FullPathTarget(), new FormatterOptions(template));
            var item = FilterTestHelpers.ApplyReturnItem(
                f,
                inputFileName: "song",
                directory: TestPaths.Absolute("Music", "Album")
            );
            Assert.Equal(Path.Combine(staging, "song.mp3"), item.Preview.FullPath);
            Assert.Equal(staging, item.Preview.DirectoryPath);
            Assert.Equal("song", item.Preview.FileName);
            Assert.Equal("mp3", item.Preview.Extension);
        }

        /// <summary>
        /// Parent directory target applies absolute directory from template; file name unchanged.
        /// </summary>
        [Fact]
        public void Apply_ParentDirectoryTarget_LiteralTemplate_MovesDirectoryOnly()
        {
            var archived = TestPaths.Absolute("Archived");
            var f = new FormatterFilter(new ParentDirectoryTarget(), new FormatterOptions(archived));
            var item = FilterTestHelpers.ApplyReturnItem(
                f,
                inputFileName: "song",
                directory: TestPaths.Absolute("Music", "Album")
            );
            Assert.Equal(archived, item.Preview.DirectoryPath);
            Assert.Equal("song", item.Preview.FileName);
            Assert.Equal("mp3", item.Preview.Extension);
            Assert.Equal(Path.Combine(archived, "song.mp3"), item.Preview.FullPath);
        }

        /// <summary>
        /// Parent directory target can include <c>parent-folder</c> token from the preview directory.
        /// </summary>
        [Fact]
        public void Apply_ParentDirectoryTarget_TokenFromPreviewDirectory()
        {
            var libs = TestPaths.Absolute("Libs");
            var f = new FormatterFilter(
                new ParentDirectoryTarget(),
                new FormatterOptions(Path.Combine(libs, "<parent-folder>"))
            );
            var item = FilterTestHelpers.ApplyReturnItem(
                f,
                inputFileName: "track",
                directory: TestPaths.Absolute("Music", "Album")
            );
            Assert.Equal(Path.Combine(libs, "Album"), item.Preview.DirectoryPath);
        }
    }
}
