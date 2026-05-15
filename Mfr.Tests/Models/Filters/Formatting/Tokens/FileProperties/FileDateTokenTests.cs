using Mfr.Filters.Formatting.Tokens.FileProperties;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.FileProperties
{
    /// <summary>
    /// Tests for <see cref="FileDateToken"/>.
    /// </summary>
    public sealed class FileDateTokenTests
    {
        /// <summary>
        /// Verifies an empty or whitespace argument throws.
        /// </summary>
        [Fact]
        public void Resolve_EmptyArg_Throws()
        {
            var token = new FileDateToken();
            var item = FilterTestHelpers.CreateRenameItem();

            var ex = Assert.Throws<ArgumentException>(() => token.Compile(tokenArgs: "")(item));
            Assert.Contains("requires arguments", ex.Message);
        }

        /// <summary>
        /// Verifies creation time with <c>dd-MM-yyyy</c> format produces expected output.
        /// </summary>
        [Fact]
        public void Resolve_CreationDdMmYyyy_FormatsExpected()
        {
            var token = new FileDateToken();
            var creation = new DateTime(2023, 4, 7, 0, 0, 0, DateTimeKind.Unspecified);
            var item = FilterTestHelpers.CreateRenameItem(creationTime: creation);

            Assert.Equal("07-04-2023", token.Compile(tokenArgs: "dd-MM-yyyy,creation")(item));
        }

        /// <summary>
        /// Verifies format-only argument (no comma) throws.
        /// </summary>
        [Fact]
        public void Resolve_FormatOnly_Throws()
        {
            var token = new FileDateToken();
            var item = FilterTestHelpers.CreateRenameItem();

            var ex = Assert.Throws<ArgumentException>(() => token.Compile(tokenArgs: "yyyy")(item));
            Assert.Contains("date-kind", ex.Message);
        }

        /// <summary>
        /// Verifies date kind <c>creation</c> with explicit format selects creation time.
        /// </summary>
        [Fact]
        public void Resolve_DateKindCreation_UsesCreation()
        {
            var token = new FileDateToken();
            var creation = new DateTime(2022, 1, 2, 0, 0, 0, DateTimeKind.Unspecified);
            var item = FilterTestHelpers.CreateRenameItem(creationTime: creation);

            Assert.Equal("2022-01-02", token.Compile(tokenArgs: "yyyy-MM-dd,creation")(item));
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

            Assert.Equal("2021", token.Compile(tokenArgs: "yyyy,lastWrite")(item));
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

            Assert.Equal("09-05-03", token.Compile(tokenArgs: "HH-mm-ss,lastAccess")(item));
        }

        /// <summary>
        /// Verifies lone keyword without format throws.
        /// </summary>
        [Fact]
        public void Resolve_KeywordOnly_Throws()
        {
            var token = new FileDateToken();
            var item = FilterTestHelpers.CreateRenameItem();

            var ex = Assert.Throws<ArgumentException>(() => token.Compile(tokenArgs: "lastWrite")(item));
            Assert.Contains("comma", ex.Message);
        }

        /// <summary>
        /// Verifies an unknown date kind throws.
        /// </summary>
        [Fact]
        public void Resolve_UnknownDateKind_Throws()
        {
            var token = new FileDateToken();
            var item = FilterTestHelpers.CreateRenameItem();

            Assert.Throws<ArgumentException>(() => token.Compile(tokenArgs: "dd-MM-yyyy,bogus")(item));
        }

        /// <summary>
        /// Verifies an empty format part throws.
        /// </summary>
        [Fact]
        public void Resolve_EmptyFormatPart_Throws()
        {
            var token = new FileDateToken();
            var item = FilterTestHelpers.CreateRenameItem();

            var ex = Assert.Throws<ArgumentException>(() => token.Compile(tokenArgs: ",lastWrite")(item));
            Assert.Contains("format", ex.Message);
        }

        /// <summary>
        /// Verifies trailing comma (missing date-kind) throws.
        /// </summary>
        [Fact]
        public void Resolve_EmptyDateKindPart_Throws()
        {
            var token = new FileDateToken();
            var item = FilterTestHelpers.CreateRenameItem();

            var ex = Assert.Throws<ArgumentException>(() => token.Compile(tokenArgs: "yyyy,")(item));
            Assert.Contains("date-kind", ex.Message);
        }

    }
}
