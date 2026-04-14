using Mfr.Core;
using Mfr.Filters;
using Mfr.Filters.Formatting;
using Mfr.Filters.Replace;
using Mfr.Models;
using Mfr.Tests.Models.Filters;
using FormatterFilter = Mfr.Filters.Formatting.FormatterFilter;

namespace Mfr.Tests.Core
{
    /// <summary>
    /// Tests for <see cref="RenameItemExtensions"/>.
    /// </summary>
    public class RenameItemExtensionsTests
    {
        /// <summary>
        /// Verifies disabled filters do not change the resulting preview.
        /// </summary>
        [Fact]
        public void ApplyFilters_AllFiltersDisabled_LeavesPreviewAtOriginal()
        {
            var item = FilterTestHelpers.CreateFile(prefix: "track", extension: ".mp3");
            item.ApplyFilters(
            [
                new ReplacerFilter(
                    Enabled: true,
                    Target: new FileNameTarget(FileNamePart.Prefix),
                    Options: new ReplacerOptions("track", "stale", ReplacerMode.Literal, CaseSensitive: true, ReplaceAll: true, WholeWord: false))
            ]);
            var filters = new List<BaseFilter>
            {
                new ReplacerFilter(
                    Enabled: false,
                    Target: new FileNameTarget(FileNamePart.Prefix),
                    Options: new ReplacerOptions("track", "song", ReplacerMode.Literal, CaseSensitive: true, ReplaceAll: true, WholeWord: false))
            };

            item.ApplyFilters(filters);

            Assert.Equal(item.Original.FullPath, item.Preview.FullPath);
            Assert.Equal(item.Original.Prefix, item.Preview.Prefix);
            Assert.Equal(item.Original.Extension, item.Preview.Extension);
        }

        /// <summary>
        /// Verifies enabled prefix filters are applied in order.
        /// </summary>
        [Fact]
        public void ApplyFilters_PrefixFilters_ApplyInOrder()
        {
            var item = FilterTestHelpers.CreateFile(prefix: "track old", extension: ".mp3");
            var filters = new List<BaseFilter>
            {
                new ReplacerFilter(
                    Enabled: true,
                    Target: new FileNameTarget(FileNamePart.Prefix),
                    Options: new ReplacerOptions("track", "song", ReplacerMode.Literal, CaseSensitive: true, ReplaceAll: true, WholeWord: false)),
                new ReplacerFilter(
                    Enabled: true,
                    Target: new FileNameTarget(FileNamePart.Prefix),
                    Options: new ReplacerOptions("old", "new", ReplacerMode.Literal, CaseSensitive: true, ReplaceAll: true, WholeWord: false))
            };

            item.ApplyFilters(filters);

            Assert.Equal("song new", item.Preview.Prefix);
            Assert.Equal(".mp3", item.Preview.Extension);
            Assert.Equal(Path.Combine(item.Original.DirectoryPath, "song new.mp3"), item.Preview.FullPath);
        }

        /// <summary>
        /// Verifies extension and full-name modes update preview metadata correctly.
        /// </summary>
        [Fact]
        public void ApplyFilters_ExtensionAndFullModes_UpdatePreview()
        {
            var item = FilterTestHelpers.CreateFile(prefix: "track", extension: ".mp3");
            var filters = new List<BaseFilter>
            {
                new ReplacerFilter(
                    Enabled: true,
                    Target: new FileNameTarget(FileNamePart.Extension),
                    Options: new ReplacerOptions(".mp3", ".flac", ReplacerMode.Literal, CaseSensitive: true, ReplaceAll: true, WholeWord: false)),
                new FormatterFilter(
                    Enabled: true,
                    Target: new FileNameTarget(FileNamePart.Full),
                    Options: new FormatterOptions("renamed.final.wav"))
            };

            item.ApplyFilters(filters);

            Assert.Equal("renamed.final", item.Preview.Prefix);
            Assert.Equal(".wav", item.Preview.Extension);
            Assert.Equal(Path.Combine(item.Original.DirectoryPath, "renamed.final.wav"), item.Preview.FullPath);
        }

        /// <summary>
        /// Verifies non-file-name targets are rejected.
        /// </summary>
        [Fact]
        public void ApplyFilters_NonFileNameTarget_ThrowsNotSupported()
        {
            var item = FilterTestHelpers.CreateFile();
            var filters = new List<BaseFilter>
            {
                new ReplacerFilter(
                    Enabled: true,
                    Target: new UnsupportedTarget(),
                    Options: new ReplacerOptions("a", "b", ReplacerMode.Literal, CaseSensitive: true, ReplaceAll: true, WholeWord: false))
            };

            var ex = Assert.Throws<NotSupportedException>(() => item.ApplyFilters(filters));
            Assert.Contains("target.family='FileName'", ex.Message);
        }

        private sealed record UnsupportedTarget : FilterTarget
        {
            public override FilterTargetFamily Family => FilterTargetFamily.FileContents;
        }
    }
}
