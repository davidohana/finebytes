using System.Globalization;
using Mfr.Filters.Formatting;

namespace Mfr.Tests.Models.Filters.Formatting
{
    /// <summary>
    /// Tests for <see cref="FormatStringResolver"/>.
    /// </summary>
    public sealed class FormatStringResolverTests
    {
        /// <summary>
        /// Verifies that multiple known tokens are replaced.
        /// </summary>
        [Fact]
        public void ResolveTemplate_KnownTokens_ReplacesValues()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "song",
                extension: ".mp3",
                directory: @"C:\Music\My Album");

            var result = FormatStringResolver.ResolveTemplate(
                template: "<file-name><ext>-<parent-folder>",
                item: item);

            Assert.Equal("song.mp3-My Album", result);
        }

        /// <summary>
        /// Verifies counter token uses global index when reset flag is 0.
        /// </summary>
        [Fact]
        public void ResolveTemplate_CounterTokenGlobalIndex_UsesGlobalIndex()
        {
            var item = FilterTestHelpers.CreateRenameItem(globalIndex: 2, inFolderIndex: 7);

            var result = FormatStringResolver.ResolveTemplate(
                template: "<counter:10,1,0,2,0>",
                item: item);

            Assert.Equal("12", result);
        }

        /// <summary>
        /// Verifies counter token uses in-folder index when reset flag is 1.
        /// </summary>
        [Fact]
        public void ResolveTemplate_CounterTokenFolderIndex_UsesInFolderIndex()
        {
            var item = FilterTestHelpers.CreateRenameItem(globalIndex: 2, inFolderIndex: 7);

            var result = FormatStringResolver.ResolveTemplate(
                template: "<counter:10,1,1,2,0>",
                item: item);

            Assert.Equal("17", result);
        }

        /// <summary>
        /// Verifies invalid counter args throw.
        /// </summary>
        [Fact]
        public void ResolveTemplate_InvalidCounterArgs_Throws()
        {
            var item = FilterTestHelpers.CreateRenameItem();

            var ex = Assert.Throws<InvalidOperationException>(() =>
                FormatStringResolver.ResolveTemplate(
                    template: "<counter:1,2>",
                    item: item));

            Assert.Contains("Invalid counter token arg", ex.Message);
        }

        /// <summary>
        /// Verifies unknown tokens throw.
        /// </summary>
        [Fact]
        public void ResolveTemplate_UnknownToken_Throws()
        {
            var item = FilterTestHelpers.CreateRenameItem();

            var ex = Assert.Throws<NotSupportedException>(() =>
                FormatStringResolver.ResolveTemplate(
                    template: "<does-not-exist>",
                    item: item));

            Assert.Contains("not supported", ex.Message);
        }

        /// <summary>
        /// Verifies <c>&lt;file-extension&gt;</c> and <c>&lt;ext&gt;</c> both resolve to the extension.
        /// </summary>
        [Fact]
        public void ResolveTemplate_FileExtensionToken_MatchesExtension()
        {
            var item = FilterTestHelpers.CreateRenameItem(extension: ".flac");

            var ext = FormatStringResolver.ResolveTemplate("<ext>", item);
            var fileExtension = FormatStringResolver.ResolveTemplate("<file-extension>", item);

            Assert.Equal(".flac", ext);
            Assert.Equal(".flac", fileExtension);
        }

        /// <summary>
        /// Verifies <c>&lt;full-name&gt;</c> is prefix plus extension.
        /// </summary>
        [Fact]
        public void ResolveTemplate_FullNameToken_ConcatenatesPrefixAndExtension()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "a", extension: ".b");

            var result = FormatStringResolver.ResolveTemplate("<full-name>", item);

            Assert.Equal("a.b", result);
        }

        /// <summary>
        /// Verifies <c>&lt;full-path&gt;</c> matches combined directory and file name.
        /// </summary>
        [Fact]
        public void ResolveTemplate_FullPathToken_MatchesFilePath()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "song",
                extension: ".mp3",
                directory: @"D:\Music\Album");

            var result = FormatStringResolver.ResolveTemplate("<full-path>", item);

            Assert.Equal(item.Original.FullPath, result);
        }

        /// <summary>
        /// Verifies <c>&lt;now&gt;</c> resolves to a round-trip ISO 8601 UTC string.
        /// </summary>
        [Fact]
        public void ResolveTemplate_NowToken_ProducesParseableUtcString()
        {
            var item = FilterTestHelpers.CreateRenameItem();

            var result = FormatStringResolver.ResolveTemplate("<now>", item);

            Assert.True(
                DateTimeOffset.TryParse(
                    result,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var parsed));
            Assert.Equal(DateTimeKind.Utc, parsed.UtcDateTime.Kind);
        }

        /// <summary>
        /// Verifies <c>&lt;now:format&gt;</c> uses the format segment after the first colon.
        /// </summary>
        [Fact]
        public void ResolveTemplate_NowWithFormat_UsesSuppliedFormat()
        {
            var item = FilterTestHelpers.CreateRenameItem();

            var result = FormatStringResolver.ResolveTemplate("<now:yyyy>", item);
            var expectedYear = DateTimeOffset.UtcNow.ToString("yyyy", CultureInfo.InvariantCulture);

            Assert.Equal(expectedYear, result);
        }

        /// <summary>
        /// Verifies counter token pad mode <c>1</c> uses spaces (matches formatter docs).
        /// </summary>
        [Fact]
        public void ResolveTemplate_CounterTokenPadModeSpace_PadsWithSpaces()
        {
            var item = FilterTestHelpers.CreateRenameItem(globalIndex: 0, inFolderIndex: 0);

            var result = FormatStringResolver.ResolveTemplate(
                template: "<counter:7,1,0,4,1>",
                item: item);

            Assert.Equal("   7", result);
        }

        /// <summary>
        /// Verifies <c>&lt;parent-folder:2&gt;</c> walks up two levels from the containing directory.
        /// </summary>
        [Fact]
        public void ResolveTemplate_ParentFolderLevel2_ReturnsGrandparentName()
        {
            var item = FilterTestHelpers.CreateRenameItem(directory: @"C:\Medical Data\apr03\patients");

            var result = FormatStringResolver.ResolveTemplate("<parent-folder:2>", item);

            Assert.Equal("apr03", result);
        }

        /// <summary>
        /// Verifies <c>&lt;parent-folder:N&gt;</c> returns empty string when level exceeds path depth.
        /// </summary>
        [Fact]
        public void ResolveTemplate_ParentFolderLevelTooHigh_ReturnsEmpty()
        {
            var item = FilterTestHelpers.CreateRenameItem(directory: @"C:\Medical Data\apr03\patients");

            var result = FormatStringResolver.ResolveTemplate("<parent-folder:5>", item);

            Assert.Equal(string.Empty, result);
        }

        /// <summary>
        /// Verifies <c>&lt;file-date&gt;</c> defaults to creation date formatted as dd-MM-yyyy.
        /// </summary>
        [Fact]
        public void ResolveTemplate_FileDateToken_DefaultsToCreationDateDdMmYyyy()
        {
            var creation = new DateTime(2023, 4, 7, 0, 0, 0, DateTimeKind.Unspecified);
            var item = FilterTestHelpers.CreateRenameItem(creationTime: creation);

            var result = FormatStringResolver.ResolveTemplate("<file-date>", item);

            Assert.Equal("07-04-2023", result);
        }

        /// <summary>
        /// Verifies <c>&lt;file-date:yyyy,1&gt;</c> uses last-write date with the supplied format.
        /// </summary>
        [Fact]
        public void ResolveTemplate_FileDateTokenLastWrite_UsesLastWriteDate()
        {
            var lastWrite = new DateTime(2021, 11, 30, 0, 0, 0, DateTimeKind.Unspecified);
            var item = FilterTestHelpers.CreateRenameItem(lastWriteTime: lastWrite);

            var result = FormatStringResolver.ResolveTemplate("<file-date:yyyy,1>", item);

            Assert.Equal("2021", result);
        }

        /// <summary>
        /// Verifies <c>&lt;file-date:HH-mm-ss,2&gt;</c> uses last-access time.
        /// </summary>
        [Fact]
        public void ResolveTemplate_FileDateTokenLastAccess_UsesLastAccessDate()
        {
            var lastAccess = new DateTime(2020, 1, 15, 9, 5, 3, DateTimeKind.Unspecified);
            var item = FilterTestHelpers.CreateRenameItem(lastAccessTime: lastAccess);

            var result = FormatStringResolver.ResolveTemplate("<file-date:HH-mm-ss,2>", item);

            Assert.Equal("09-05-03", result);
        }

        /// <summary>
        /// Verifies unsupported date type throws.
        /// </summary>
        [Fact]
        public void ResolveTemplate_FileDateUnsupportedType_Throws()
        {
            var item = FilterTestHelpers.CreateRenameItem();

            Assert.Throws<NotSupportedException>(() =>
                FormatStringResolver.ResolveTemplate("<file-date:dd-MM-yyyy,3>", item));
        }

        /// <summary>
        /// Verifies <c>&lt;drive-letter&gt;</c> extracts the drive letter from a local path.
        /// </summary>
        [Fact]
        public void ResolveTemplate_DriveLetter_ReturnsRootWithoutSeparator()
        {
            var item = FilterTestHelpers.CreateRenameItem(directory: @"C:\Medical Data\patients");

            var result = FormatStringResolver.ResolveTemplate("<drive-letter>", item);

            Assert.Equal("C:", result);
        }

        /// <summary>
        /// Verifies <c>&lt;drive-letter&gt;</c> returns <c>$</c> for UNC (network) paths.
        /// </summary>
        [Fact]
        public void ResolveTemplate_DriveLetterUncPath_ReturnsDollarSign()
        {
            var item = FilterTestHelpers.CreateRenameItem(directory: @"\\server\share\docs");

            var result = FormatStringResolver.ResolveTemplate("<drive-letter>", item);

            Assert.Equal("$", result);
        }

        /// <summary>
        /// Verifies <c>&lt;file-size&gt;</c> auto-selects KB for a 2048-byte file.
        /// </summary>
        [Fact]
        public void ResolveTemplate_FileSizeAutoKb_FormatsWithKbUnit()
        {
            var item = FilterTestHelpers.CreateRenameItem(fileSize: 2048);

            var result = FormatStringResolver.ResolveTemplate("<file-size>", item);

            Assert.Equal("2 KB", result);
        }

        /// <summary>
        /// Verifies <c>&lt;file-size:3,2&gt;</c> formats in MB with two decimal places.
        /// </summary>
        [Fact]
        public void ResolveTemplate_FileSizeExplicitMb_FormatsWithTwoDecimals()
        {
            var item = FilterTestHelpers.CreateRenameItem(fileSize: 1572864);

            var result = FormatStringResolver.ResolveTemplate("<file-size:3,2>", item);

            Assert.Equal("1.50 MB", result);
        }

        /// <summary>
        /// Verifies <c>&lt;file-size:kb,1&gt;</c> accepts the string alias for kilobytes.
        /// </summary>
        [Fact]
        public void ResolveTemplate_FileSizeKbAlias_FormatsInKilobytes()
        {
            var item = FilterTestHelpers.CreateRenameItem(fileSize: 3072);

            var result = FormatStringResolver.ResolveTemplate("<file-size:kb,1>", item);

            Assert.Equal("3.0 KB", result);
        }

        /// <summary>
        /// Verifies <c>&lt;file-size:b&gt;</c> returns raw bytes with B unit.
        /// </summary>
        [Fact]
        public void ResolveTemplate_FileSizeBytesAlias_ReturnsBytesWithUnit()
        {
            var item = FilterTestHelpers.CreateRenameItem(fileSize: 512);

            var result = FormatStringResolver.ResolveTemplate("<file-size:b>", item);

            Assert.Equal("512 B", result);
        }

        /// <summary>
        /// Verifies <c>&lt;file-count&gt;</c> returns the number of entries in an existing directory.
        /// </summary>
        [Fact]
        public void ResolveTemplate_FileCount_ReturnsEntryCountForRealDirectory()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            try
            {
                File.WriteAllText(Path.Combine(tempDir, "a.txt"), "");
                File.WriteAllText(Path.Combine(tempDir, "b.txt"), "");
                var item = FilterTestHelpers.CreateRenameItem(directory: tempDir);

                var result = FormatStringResolver.ResolveTemplate("<file-count>", item);

                Assert.Equal("2", result);
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }

        /// <summary>
        /// Verifies <c>&lt;file-count&gt;</c> returns empty string when the directory does not exist.
        /// </summary>
        [Fact]
        public void ResolveTemplate_FileCount_ReturnsEmptyForNonExistentDirectory()
        {
            var item = FilterTestHelpers.CreateRenameItem(directory: @"C:\DoesNotExist\Never");

            var result = FormatStringResolver.ResolveTemplate("<file-count>", item);

            Assert.Equal(string.Empty, result);
        }
    }
}
