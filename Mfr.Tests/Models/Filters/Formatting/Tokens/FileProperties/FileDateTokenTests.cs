using Mfr.Filters.Formatting.Tokens.FileProperties;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.FileProperties
{
    /// <summary>
    /// Tests for <see cref="FileDateToken"/>.
    /// </summary>
    public sealed class FileDateTokenTests
    {
        /// <summary>
        /// Verifies a missing argument formats creation date as <c>dd-MM-yyyy</c>.
        /// </summary>
        [Fact]
        public void Resolve_NoArg_DefaultsToCreationDdMmYyyy()
        {
            var token = new FileDateToken();
            var creation = new DateTime(2023, 4, 7, 0, 0, 0, DateTimeKind.Unspecified);
            var item = FilterTestHelpers.CreateRenameItem(creationTime: creation);

            Assert.Equal("07-04-2023", token.Compile(arg: "")(item));
        }

        /// <summary>
        /// Verifies a format-only argument (no comma) defaults to creation date.
        /// </summary>
        [Fact]
        public void Resolve_FormatOnly_UsesCreationDate()
        {
            var token = new FileDateToken();
            var creation = new DateTime(2024, 5, 9, 0, 0, 0, DateTimeKind.Unspecified);
            var item = FilterTestHelpers.CreateRenameItem(creationTime: creation);

            Assert.Equal("2024", token.Compile(arg: "yyyy")(item));
        }

        /// <summary>
        /// Verifies date kind <c>creation</c> selects creation time.
        /// </summary>
        [Fact]
        public void Resolve_DateKindCreation_UsesCreation()
        {
            var token = new FileDateToken();
            var creation = new DateTime(2022, 1, 2, 0, 0, 0, DateTimeKind.Unspecified);
            var item = FilterTestHelpers.CreateRenameItem(creationTime: creation);

            Assert.Equal("2022-01-02", token.Compile(arg: "yyyy-MM-dd,creation")(item));
        }

        /// <summary>
        /// Verifies date kind <c>lastWrite</c> selects last-write date.
        /// </summary>
        [Fact]
        public void Resolve_DateKindLastWrite_UsesLastWrite()
        {
            var token = new FileDateToken();
            var lastWrite = new DateTime(2021, 11, 30, 0, 0, 0, DateTimeKind.Unspecified);
            var item = FilterTestHelpers.CreateRenameItem(lastWriteTime: lastWrite);

            Assert.Equal("2021", token.Compile(arg: "yyyy,lastWrite")(item));
        }

        /// <summary>
        /// Verifies date kind <c>lastAccess</c> selects last-access time.
        /// </summary>
        [Fact]
        public void Resolve_DateKindLastAccess_UsesLastAccess()
        {
            var token = new FileDateToken();
            var lastAccess = new DateTime(2020, 1, 15, 9, 5, 3, DateTimeKind.Unspecified);
            var item = FilterTestHelpers.CreateRenameItem(lastAccessTime: lastAccess);

            Assert.Equal("09-05-03", token.Compile(arg: "HH-mm-ss,lastAccess")(item));
        }

        /// <summary>
        /// Verifies lone keyword <c>lastWrite</c> uses default format.
        /// </summary>
        [Fact]
        public void Resolve_KeywordOnlyLastWrite_UsesDefaultFormat()
        {
            var token = new FileDateToken();
            var lastWrite = new DateTime(2023, 4, 7, 0, 0, 0, DateTimeKind.Unspecified);
            var item = FilterTestHelpers.CreateRenameItem(lastWriteTime: lastWrite);

            Assert.Equal("07-04-2023", token.Compile(arg: "lastWrite")(item));
        }

        /// <summary>
        /// Verifies an unknown date kind throws.
        /// </summary>
        [Fact]
        public void Resolve_UnknownDateKind_Throws()
        {
            var token = new FileDateToken();
            var item = FilterTestHelpers.CreateRenameItem();

            Assert.Throws<NotSupportedException>(() => token.Compile(arg: "dd-MM-yyyy,bogus")(item));
        }

        /// <summary>
        /// Verifies an empty format part falls back to the default while still honoring the date type.
        /// </summary>
        [Fact]
        public void Resolve_EmptyFormatWithDateKind_UsesDefaultFormat()
        {
            var token = new FileDateToken();
            var lastWrite = new DateTime(2023, 4, 7, 0, 0, 0, DateTimeKind.Unspecified);
            var item = FilterTestHelpers.CreateRenameItem(lastWriteTime: lastWrite);

            Assert.Equal("07-04-2023", token.Compile(arg: ",lastWrite")(item));
        }
    }
}
