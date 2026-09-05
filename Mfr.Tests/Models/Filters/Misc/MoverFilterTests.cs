using Mfr.Filters.Misc;
using Mfr.Utils;

namespace Mfr.Tests.Models.Filters.Misc
{
    /// <summary>
    /// Tests for <see cref="MoverFilter"/>.
    /// </summary>
    public class MoverFilterTests
    {
        private static string Dest => TestPaths.Absolute("Dest");
        private static string Source => TestPaths.Absolute("Source");
        private static string Music => TestPaths.Absolute("Music");
        private static string Archive => TestPaths.Absolute("Archive");

        /// <summary>
        /// Verifies the preview directory is set to RootFolder when SubFolder is omitted.
        /// </summary>
        [Fact]
        public void Apply_RootOnly_SetsDirectoryToRoot()
        {
            var filter = new MoverFilter(new MoverOptions(Dest));
            var item = FilterTestHelpers.ApplyReturnItem(filter, "track", directory: Source);

            Assert.Equal(Dest, item.Preview.DirectoryPath);
        }

        /// <summary>
        /// Verifies the preview directory is set to RootFolder when SubFolder is an empty string.
        /// </summary>
        [Fact]
        public void Apply_EmptySubFolder_SetsDirectoryToRoot()
        {
            var filter = new MoverFilter(new MoverOptions(Dest, SubFolder: ""));
            var item = FilterTestHelpers.ApplyReturnItem(filter, "track", directory: Source);

            Assert.Equal(Dest, item.Preview.DirectoryPath);
        }

        /// <summary>
        /// Verifies the preview directory is Root combined with a static sub-folder.
        /// </summary>
        [Fact]
        public void Apply_StaticSubFolder_CombinesRootAndSubFolder()
        {
            var filter = new MoverFilter(new MoverOptions(Dest, SubFolder: "Albums"));
            var item = FilterTestHelpers.ApplyReturnItem(filter, "track", directory: Source);

            Assert.Equal(Path.Combine(Dest, "Albums"), item.Preview.DirectoryPath);
        }

        /// <summary>
        /// Verifies that a multi-level sub-folder with backslashes creates a deep directory structure.
        /// </summary>
        [Fact]
        public void Apply_MultiLevelSubFolder_CombinesRootAndDeepPath()
        {
            var filter = new MoverFilter(new MoverOptions(Dest, SubFolder: @"Artist\Album"));
            var item = FilterTestHelpers.ApplyReturnItem(filter, "track", directory: Source);

            Assert.Equal(Path.Combine(Dest, "Artist", "Album"), item.Preview.DirectoryPath);
        }

        /// <summary>
        /// Verifies the file prefix is unchanged after applying the filter.
        /// </summary>
        [Fact]
        public void Apply_DoesNotChangePrefix()
        {
            var filter = new MoverFilter(new MoverOptions(Dest, SubFolder: "Sub"));
            var item = FilterTestHelpers.ApplyReturnItem(filter, "my-track", directory: Source);

            Assert.Equal("my-track", item.Preview.Prefix);
        }

        /// <summary>
        /// Verifies the file extension is unchanged after applying the filter.
        /// </summary>
        [Fact]
        public void Apply_DoesNotChangeExtension()
        {
            var filter = new MoverFilter(new MoverOptions(Dest));
            var item = FilterTestHelpers.ApplyReturnItem(filter, "track", extension: ".flac", directory: Source);

            Assert.Equal(".flac", item.Preview.Extension);
        }

        /// <summary>
        /// Verifies that a template token in SubFolder is resolved from the item.
        /// </summary>
        [Fact]
        public void Apply_TemplateSubFolder_ResolvesToken()
        {
            var filter = new MoverFilter(new MoverOptions(Music, SubFolder: "<file-name>"));
            var item = FilterTestHelpers.ApplyReturnItem(
                filter,
                "Blue Moon",
                directory: TestPaths.Absolute("Downloads")
            );

            Assert.Equal(Path.Combine(Music, "Blue Moon"), item.Preview.DirectoryPath);
        }

        /// <summary>
        /// Verifies that a template token mixed with a static segment produces the correct path.
        /// </summary>
        [Fact]
        public void Apply_TemplateWithStaticSegment_ProducesCompoundPath()
        {
            var filter = new MoverFilter(new MoverOptions(Music, SubFolder: @"Artists\<parent-folder>"));
            var item = FilterTestHelpers.ApplyReturnItem(
                filter,
                "track",
                directory: TestPaths.Absolute("Downloads", "Junkies")
            );

            Assert.Equal(Path.Combine(Music, "Artists", "Junkies"), item.Preview.DirectoryPath);
        }

        /// <summary>
        /// Verifies that a leading separator in the resolved sub-folder is stripped before combining.
        /// </summary>
        [Fact]
        public void Apply_SubFolderWithLeadingSeparator_StripsAndCombines()
        {
            var filter = new MoverFilter(new MoverOptions(Dest, SubFolder: @"\Sub"));
            var item = FilterTestHelpers.ApplyReturnItem(filter, "track", directory: Source);

            Assert.Equal(Path.Combine(Dest, "Sub"), item.Preview.DirectoryPath);
        }

        /// <summary>
        /// Verifies that Setup throws when RootFolder is empty.
        /// </summary>
        [Fact]
        public void Setup_EmptyRootFolder_Throws()
        {
            var filter = new MoverFilter(new MoverOptions(""));

            Assert.Throws<ArgumentException>(filter.Setup);
        }

        /// <summary>
        /// Verifies that Setup throws when RootFolder is whitespace.
        /// </summary>
        [Fact]
        public void Setup_WhitespaceRootFolder_Throws()
        {
            var filter = new MoverFilter(new MoverOptions("   "));

            Assert.Throws<ArgumentException>(filter.Setup);
        }

        /// <summary>
        /// Verifies that Setup throws when RootFolder is a relative path.
        /// </summary>
        [Fact]
        public void Setup_RelativeRootFolder_Throws()
        {
            var filter = new MoverFilter(new MoverOptions(@"relative\path"));

            Assert.Throws<ArgumentException>(filter.Setup);
        }

        /// <summary>
        /// Verifies that the original directory is not modified by the filter.
        /// </summary>
        [Fact]
        public void Apply_OriginalDirectoryUnchanged()
        {
            var filter = new MoverFilter(new MoverOptions(Dest));
            var item = FilterTestHelpers.ApplyReturnItem(filter, "track", directory: Source);

            Assert.Equal(Source, item.Original.DirectoryPath);
        }

        /// <summary>
        /// Verifies folder list entries (<see cref="FileAttributes.Directory"/>, empty extension) get a preview
        /// parent path under root + sub-folder and keep the folder name as <see cref="FileMeta.Prefix"/>
        /// (same layout as filesystem directories resolved into the rename list).
        /// </summary>
        [Fact]
        public void Apply_FolderEntry_MultipliesPreviewDirectoryAndKeepsFolderName()
        {
            var filter = new MoverFilter(new MoverOptions(Archive, SubFolder: "Sorted"));
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "Photos",
                extension: string.Empty,
                directory: TestPaths.Absolute("Inbox"),
                attributes: FileAttributes.Directory
            );
            filter.Setup();
            filter.Apply(item);

            var entryIsDirectoryAfterMove =
                item.Preview.Attributes.IsDirectory() && item.Original.Attributes.IsDirectory();
            Assert.True(entryIsDirectoryAfterMove);
            Assert.Equal(Path.Combine(Archive, "Sorted"), item.Preview.DirectoryPath);
            Assert.Equal(Path.Combine(Archive, "Sorted", "Photos"), item.Preview.FullPath);
        }

        /// <summary>
        /// Verifies formatter tokens resolve from the folder entry's original path the same way as for files.
        /// </summary>
        [Fact]
        public void Apply_FolderEntry_TemplateUsesOriginalParentSegment()
        {
            var filter = new MoverFilter(new MoverOptions(Music, SubFolder: "<parent-folder>"));
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "TheTrinitySession",
                extension: string.Empty,
                directory: TestPaths.Absolute("Downloads", "CowboyJunkies"),
                attributes: FileAttributes.Directory
            );
            filter.Setup();
            filter.Apply(item);

            Assert.Equal(Path.Combine(Music, "CowboyJunkies"), item.Preview.DirectoryPath);
            Assert.Equal(Path.Combine(Music, "CowboyJunkies", "TheTrinitySession"), item.Preview.FullPath);
        }
    }
}
