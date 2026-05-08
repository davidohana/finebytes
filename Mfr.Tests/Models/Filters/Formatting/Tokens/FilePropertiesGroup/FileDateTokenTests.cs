using Mfr.Filters.Formatting.Tokens.FilePropertiesGroup;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.FilePropertiesGroup
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

            Assert.Equal("07-04-2023", token.Resolve(arg: "", item: item));
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

            Assert.Equal("2024", token.Resolve(arg: "yyyy", item: item));
        }

        /// <summary>
        /// Verifies date-type <c>0</c> selects creation date.
        /// </summary>
        [Fact]
        public void Resolve_DateTypeZero_UsesCreation()
        {
            var token = new FileDateToken();
            var creation = new DateTime(2022, 1, 2, 0, 0, 0, DateTimeKind.Unspecified);
            var item = FilterTestHelpers.CreateRenameItem(creationTime: creation);

            Assert.Equal("2022-01-02", token.Resolve(arg: "yyyy-MM-dd,0", item: item));
        }

        /// <summary>
        /// Verifies date-type <c>1</c> selects last-write date.
        /// </summary>
        [Fact]
        public void Resolve_DateTypeOne_UsesLastWrite()
        {
            var token = new FileDateToken();
            var lastWrite = new DateTime(2021, 11, 30, 0, 0, 0, DateTimeKind.Unspecified);
            var item = FilterTestHelpers.CreateRenameItem(lastWriteTime: lastWrite);

            Assert.Equal("2021", token.Resolve(arg: "yyyy,1", item: item));
        }

        /// <summary>
        /// Verifies date-type <c>2</c> selects last-access time.
        /// </summary>
        [Fact]
        public void Resolve_DateTypeTwo_UsesLastAccess()
        {
            var token = new FileDateToken();
            var lastAccess = new DateTime(2020, 1, 15, 9, 5, 3, DateTimeKind.Unspecified);
            var item = FilterTestHelpers.CreateRenameItem(lastAccessTime: lastAccess);

            Assert.Equal("09-05-03", token.Resolve(arg: "HH-mm-ss,2", item: item));
        }

        /// <summary>
        /// Verifies an out-of-range date-type throws.
        /// </summary>
        [Fact]
        public void Resolve_DateTypeOutOfRange_Throws()
        {
            var token = new FileDateToken();
            var item = FilterTestHelpers.CreateRenameItem();

            Assert.Throws<NotSupportedException>(() => token.Resolve(arg: "dd-MM-yyyy,3", item: item));
        }

        /// <summary>
        /// Verifies an empty format part falls back to the default while still honoring the date type.
        /// </summary>
        [Fact]
        public void Resolve_EmptyFormatWithDateType_UsesDefaultFormat()
        {
            var token = new FileDateToken();
            var lastWrite = new DateTime(2023, 4, 7, 0, 0, 0, DateTimeKind.Unspecified);
            var item = FilterTestHelpers.CreateRenameItem(lastWriteTime: lastWrite);

            Assert.Equal("07-04-2023", token.Resolve(arg: ",1", item: item));
        }
    }
}
