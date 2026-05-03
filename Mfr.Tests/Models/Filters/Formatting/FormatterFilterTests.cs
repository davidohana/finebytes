using Mfr.Filters.Formatting;
using Mfr.Models;
using Mfr.Utils;

namespace Mfr.Tests.Models.Filters.Formatting
{
    /// <summary>
    /// Tests for <see cref="FormatterFilter"/>.
    /// </summary>
    public class FormatterFilterTests
    {
        private static readonly FileNameTarget _target = new(FileNamePart.Full);

        /// <summary>
        /// Verifies file-name token substitution.
        /// </summary>
        [Fact]
        public void Apply_FileNameToken_UsesPrefix()
        {
            var f = new FormatterFilter(_target, new FormatterOptions("<file-name>"));
            Assert.Equal("song", FilterTestHelpers.ApplyToPrefix(f, "song"));
        }

        /// <summary>
        /// Verifies counter token with global index.
        /// </summary>
        [Fact]
        public void Apply_CounterToken_UsesGlobalIndex()
        {
            var f = new FormatterFilter(_target, new FormatterOptions("<counter:10,2,0,4,0>"));
            Assert.Equal("0016", FilterTestHelpers.ApplyToPrefix(f, "ignored", globalIndex: 3));
        }

        /// <summary>
        /// Verifies parent-folder token.
        /// </summary>
        [Fact]
        public void Apply_ParentFolderToken_UsesDirectoryName()
        {
            var f = new FormatterFilter(_target, new FormatterOptions("<parent-folder>"));
            Assert.Equal("My Album", FilterTestHelpers.ApplyToPrefix(f, "ignored", directory: "Music".CombinePath("My Album")));
        }

        /// <summary>
        /// Full path target picks up formatter template output (<c>Original</c>-based tokens plus literals).
        /// </summary>
        [Fact]
        public void Apply_FullPathTarget_SetsPreviewFromTemplate()
        {
            var template = @"D:\Staging\<full-name>";
            var f = new FormatterFilter(new FullPathTarget(), new FormatterOptions(template));
            var item = FilterTestHelpers.ApplyReturnItem(
                f,
                inputPrefix: "song",
                directory: @"C:\Music\Album");
            Assert.Equal(@"D:\Staging\song.mp3", item.Preview.FullPath);
            Assert.Equal(@"D:\Staging", item.Preview.DirectoryPath);
            Assert.Equal("song", item.Preview.Prefix);
            Assert.Equal(".mp3", item.Preview.Extension);
        }

        /// <summary>
        /// Parent directory target applies absolute directory from template; file name unchanged.
        /// </summary>
        [Fact]
        public void Apply_ParentDirectoryTarget_LiteralTemplate_MovesDirectoryOnly()
        {
            var f = new FormatterFilter(new ParentDirectoryTarget(), new FormatterOptions(@"D:\Archived"));
            var item = FilterTestHelpers.ApplyReturnItem(
                f,
                inputPrefix: "song",
                directory: @"C:\Music\Album");
            Assert.Equal(@"D:\Archived", item.Preview.DirectoryPath);
            Assert.Equal("song", item.Preview.Prefix);
            Assert.Equal(".mp3", item.Preview.Extension);
            Assert.Equal(@"D:\Archived\song.mp3", item.Preview.FullPath);
        }

        /// <summary>
        /// Parent directory target can include <c>parent-folder</c> token from original path.
        /// </summary>
        [Fact]
        public void Apply_ParentDirectoryTarget_TokenFromOriginalDirectory()
        {
            var f = new FormatterFilter(
                new ParentDirectoryTarget(),
                new FormatterOptions(@"D:\Libs\<parent-folder>"));
            var item = FilterTestHelpers.ApplyReturnItem(
                f,
                inputPrefix: "track",
                directory: @"C:\Music\Album");
            Assert.Equal(@"D:\Libs\Album", item.Preview.DirectoryPath);
        }
    }
}
