using Mfr.Filters.Misc;
using Mfr.Models;
using Mfr.Utils;

namespace Mfr.Tests.Models.Filters.Misc
{
    /// <summary>
    /// Tests for <see cref="MoverFilter"/>.
    /// </summary>
    public class MoverFilterTests
    {
        /// <summary>
        /// Verifies the preview directory is set to RootFolder when SubFolder is omitted.
        /// </summary>
        [Fact]
        public void Apply_RootOnly_SetsDirectoryToRoot()
        {
            var filter = new MoverFilter(new MoverOptions(@"C:\Dest"));
            var item = FilterTestHelpers.ApplyReturnItem(filter, "track", directory: @"C:\Source");

            Assert.Equal(@"C:\Dest", item.Preview.DirectoryPath);
        }

        /// <summary>
        /// Verifies the preview directory is set to RootFolder when SubFolder is an empty string.
        /// </summary>
        [Fact]
        public void Apply_EmptySubFolder_SetsDirectoryToRoot()
        {
            var filter = new MoverFilter(new MoverOptions(@"C:\Dest", SubFolder: ""));
            var item = FilterTestHelpers.ApplyReturnItem(filter, "track", directory: @"C:\Source");

            Assert.Equal(@"C:\Dest", item.Preview.DirectoryPath);
        }

        /// <summary>
        /// Verifies the preview directory is Root combined with a static sub-folder.
        /// </summary>
        [Fact]
        public void Apply_StaticSubFolder_CombinesRootAndSubFolder()
        {
            var filter = new MoverFilter(new MoverOptions(@"C:\Dest", SubFolder: "Albums"));
            var item = FilterTestHelpers.ApplyReturnItem(filter, "track", directory: @"C:\Source");

            Assert.Equal(@"C:\Dest\Albums", item.Preview.DirectoryPath);
        }

        /// <summary>
        /// Verifies that a multi-level sub-folder with backslashes creates a deep directory structure.
        /// </summary>
        [Fact]
        public void Apply_MultiLevelSubFolder_CombinesRootAndDeepPath()
        {
            var filter = new MoverFilter(new MoverOptions(@"C:\Dest", SubFolder: @"Artist\Album"));
            var item = FilterTestHelpers.ApplyReturnItem(filter, "track", directory: @"C:\Source");

            Assert.Equal(@"C:\Dest\Artist\Album", item.Preview.DirectoryPath);
        }

        /// <summary>
        /// Verifies the file prefix is unchanged after applying the filter.
        /// </summary>
        [Fact]
        public void Apply_DoesNotChangePrefix()
        {
            var filter = new MoverFilter(new MoverOptions(@"C:\Dest", SubFolder: "Sub"));
            var item = FilterTestHelpers.ApplyReturnItem(filter, "my-track", directory: @"C:\Source");

            Assert.Equal("my-track", item.Preview.Prefix);
        }

        /// <summary>
        /// Verifies the file extension is unchanged after applying the filter.
        /// </summary>
        [Fact]
        public void Apply_DoesNotChangeExtension()
        {
            var filter = new MoverFilter(new MoverOptions(@"C:\Dest"));
            var item = FilterTestHelpers.ApplyReturnItem(filter, "track", extension: ".flac", directory: @"C:\Source");

            Assert.Equal(".flac", item.Preview.Extension);
        }

        /// <summary>
        /// Verifies that a template token in SubFolder is resolved from the item.
        /// </summary>
        [Fact]
        public void Apply_TemplateSubFolder_ResolvesToken()
        {
            var filter = new MoverFilter(new MoverOptions(@"C:\Music", SubFolder: "<file-name>"));
            var item = FilterTestHelpers.ApplyReturnItem(filter, "Blue Moon", directory: @"C:\Downloads");

            Assert.Equal(@"C:\Music\Blue Moon", item.Preview.DirectoryPath);
        }

        /// <summary>
        /// Verifies that a template token mixed with a static segment produces the correct path.
        /// </summary>
        [Fact]
        public void Apply_TemplateWithStaticSegment_ProducesCompoundPath()
        {
            var filter = new MoverFilter(
                new MoverOptions(@"C:\Music", SubFolder: @"Artists\<parent-folder>"));
            var item = FilterTestHelpers.ApplyReturnItem(
                filter, "track", directory: @"C:\Downloads\Junkies");

            Assert.Equal(@"C:\Music\Artists\Junkies", item.Preview.DirectoryPath);
        }

        /// <summary>
        /// Verifies that a leading separator in the resolved sub-folder is stripped before combining.
        /// </summary>
        [Fact]
        public void Apply_SubFolderWithLeadingSeparator_StripsAndCombines()
        {
            var filter = new MoverFilter(new MoverOptions(@"C:\Dest", SubFolder: @"\Sub"));
            var item = FilterTestHelpers.ApplyReturnItem(filter, "track", directory: @"C:\Source");

            Assert.Equal(@"C:\Dest\Sub", item.Preview.DirectoryPath);
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
            var filter = new MoverFilter(new MoverOptions(@"C:\Dest"));
            var item = FilterTestHelpers.ApplyReturnItem(filter, "track", directory: @"C:\Source");

            Assert.Equal(@"C:\Source", item.Original.DirectoryPath);
        }

        /// <summary>
        /// Verifies folder list entries (<see cref="FileAttributes.Directory"/>, empty extension) get a preview
        /// parent path under root + sub-folder and keep the folder name as <see cref="FileMeta.Prefix"/>
        /// (same layout as filesystem directories resolved into the rename list).
        /// </summary>
        [Fact]
        public void Apply_FolderEntry_MultipliesPreviewDirectoryAndKeepsFolderName()
        {
            var filter = new MoverFilter(new MoverOptions(@"C:\Archive", SubFolder: "Sorted"));
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "Photos",
                extension: string.Empty,
                directory: @"C:\Inbox",
                attributes: FileAttributes.Directory);
            filter.Setup();
            filter.Apply(item);

            var entryIsDirectoryAfterMove =
                item.Preview.Attributes.IsDirectory()
                && item.Original.Attributes.IsDirectory();
            Assert.True(entryIsDirectoryAfterMove);
            Assert.Equal(@"C:\Archive\Sorted", item.Preview.DirectoryPath);
            Assert.Equal(@"C:\Archive\Sorted\Photos", item.Preview.FullPath);
        }

        /// <summary>
        /// Verifies formatter tokens resolve from the folder entry's original path the same way as for files.
        /// </summary>
        [Fact]
        public void Apply_FolderEntry_TemplateUsesOriginalParentSegment()
        {
            var filter = new MoverFilter(new MoverOptions(@"C:\Music", SubFolder: "<parent-folder>"));
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "TheTrinitySession",
                extension: string.Empty,
                directory: @"C:\Downloads\CowboyJunkies",
                attributes: FileAttributes.Directory);
            filter.Setup();
            filter.Apply(item);

            Assert.Equal(@"C:\Music\CowboyJunkies", item.Preview.DirectoryPath);
            Assert.Equal(@"C:\Music\CowboyJunkies\TheTrinitySession", item.Preview.FullPath);
        }
    }
}
