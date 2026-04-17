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
    }
}
