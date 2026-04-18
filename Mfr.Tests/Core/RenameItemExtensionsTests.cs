using Mfr.Filters.Formatting;
using Mfr.Filters.Replace;
using Mfr.Models;
using Mfr.Tests.Models.Filters;
using FormatterFilter = Mfr.Filters.Formatting.FormatterFilter;

namespace Mfr.Tests.Core
{
    /// <summary>
    /// Tests for <see cref="FilterChain.ApplyFilters"/>.
    /// </summary>
    public class RenameItemExtensionsTests
    {
        /// <summary>
        /// Verifies disabled filters do not change the resulting preview.
        /// </summary>
        [Fact]
        public void ApplyFilters_AllFiltersDisabled_LeavesPreviewAtOriginal()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "track", extension: ".mp3");
            var firstChain = FilterChain.CreateAllEnabled(
            [
                new ReplacerFilter(
                    Target: new FileNameTarget(FileNamePart.Prefix),
                    Options: new ReplacerOptions("track", "stale", ReplacerMode.Literal, CaseSensitive: true, ReplaceAll: true, WholeWord: false))
            ]);
            firstChain.SetupFilters();
            firstChain.ApplyFilters(item);
            var chain = new FilterChain
            {
                Steps =
                [
                    new FilterChainStep(
                        Enabled: false,
                        Filter: new ReplacerFilter(
                            Target: new FileNameTarget(FileNamePart.Prefix),
                            Options: new ReplacerOptions("track", "song", ReplacerMode.Literal, CaseSensitive: true, ReplaceAll: true, WholeWord: false)))
                ]
            };

            chain.SetupFilters();
            chain.ApplyFilters(item);

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
            var item = FilterTestHelpers.CreateRenameItem(prefix: "track old", extension: ".mp3");
            var chain = FilterChain.CreateAllEnabled(
            [
                new ReplacerFilter(
                    Target: new FileNameTarget(FileNamePart.Prefix),
                    Options: new ReplacerOptions("track", "song", ReplacerMode.Literal, CaseSensitive: true, ReplaceAll: true, WholeWord: false)),
                new ReplacerFilter(
                    Target: new FileNameTarget(FileNamePart.Prefix),
                    Options: new ReplacerOptions("old", "new", ReplacerMode.Literal, CaseSensitive: true, ReplaceAll: true, WholeWord: false))
            ]);

            chain.SetupFilters();
            chain.ApplyFilters(item);

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
            var item = FilterTestHelpers.CreateRenameItem(prefix: "track", extension: ".mp3");
            var chain = FilterChain.CreateAllEnabled(
            [
                new ReplacerFilter(
                    Target: new FileNameTarget(FileNamePart.Extension),
                    Options: new ReplacerOptions(".mp3", ".flac", ReplacerMode.Literal, CaseSensitive: true, ReplaceAll: true, WholeWord: false)),
                new FormatterFilter(
                    Target: new FileNameTarget(FileNamePart.Full),
                    Options: new FormatterOptions("renamed.final.wav"))
            ]);

            chain.SetupFilters();
            chain.ApplyFilters(item);

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
            var item = FilterTestHelpers.CreateRenameItem();
            var chain = FilterChain.CreateAllEnabled(
            [
                new ReplacerFilter(
                    Target: new UnsupportedTarget(),
                    Options: new ReplacerOptions("a", "b", ReplacerMode.Literal, CaseSensitive: true, ReplaceAll: true, WholeWord: false))
            ]);

            chain.SetupFilters();
            var ex = Assert.Throws<NotSupportedException>(() => chain.ApplyFilters(item));
            Assert.Contains("requires a FileName target", ex.Message);
            Assert.Contains("UnsupportedTarget", ex.Message);
        }

        private sealed record UnsupportedTarget : FilterTarget;
    }
}
