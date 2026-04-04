using Mfr.Models;
using Mfr.Models.Filters.Advanced;
using Mfr.Utils;

namespace Mfr.Tests.Models.Filters.Advanced
{
    /// <summary>
    /// Tests for <see cref="FormatterFilter"/>.
    /// </summary>
    public class FormatterFilterTests
    {
        private static readonly FileNameTarget _Target = new(FileNameTargetMode.Full);

        /// <summary>
        /// Verifies file-name token substitution.
        /// </summary>
        [Fact]
        public void Apply_FileNameToken_UsesPrefix()
        {
            var f = new FormatterFilter(true, _Target, new FormatterOptions("<file-name>"));
            var file = FilterTestHelpers.CreateFile(prefix: "song", extension: ".mp3");
            Assert.Equal("song", f.Apply("ignored", file));
        }

        /// <summary>
        /// Verifies counter token with global index.
        /// </summary>
        [Fact]
        public void Apply_CounterToken_UsesGlobalIndex()
        {
            var f = new FormatterFilter(true, _Target, new FormatterOptions("<counter:10,2,0,4,0>"));
            var file = FilterTestHelpers.CreateFile(globalIndex: 3);
            Assert.Equal("0016", f.Apply("ignored", file));
        }

        /// <summary>
        /// Verifies parent-folder token.
        /// </summary>
        [Fact]
        public void Apply_ParentFolderToken_UsesDirectoryName()
        {
            var f = new FormatterFilter(true, _Target, new FormatterOptions("<parent-folder>"));
            var file = FilterTestHelpers.CreateFile(directory: "Music".CombinePath("My Album"));
            Assert.Equal("My Album", f.Apply("ignored", file));
        }
    }
}
