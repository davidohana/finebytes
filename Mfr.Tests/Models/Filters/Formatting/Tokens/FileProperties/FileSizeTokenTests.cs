using Mfr.Filters.Formatting.Tokens.FileProperties;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.FileProperties
{
    /// <summary>
    /// Tests for <see cref="FileSizeToken"/>.
    /// </summary>
    public sealed class FileSizeTokenTests
    {
        /// <summary>
        /// Verifies auto-unit selects bytes for sizes below 1 KB.
        /// </summary>
        [Fact]
        public void Resolve_AutoUnit_BelowKb_FormatsAsBytes()
        {
            var token = new FileSizeToken();
            var item = FilterTestHelpers.CreateRenameItem(fileSize: 512);

            Assert.Equal("512 B", token.Compile(arg: "")(item));
        }

        /// <summary>
        /// Verifies auto-unit selects KB for sizes between 1 KB and 1 MB.
        /// </summary>
        [Fact]
        public void Resolve_AutoUnit_KbRange_FormatsAsKb()
        {
            var token = new FileSizeToken();
            var item = FilterTestHelpers.CreateRenameItem(fileSize: 2048);

            Assert.Equal("2 KB", token.Compile(arg: "")(item));
        }

        /// <summary>
        /// Verifies auto-unit selects MB for sizes between 1 MB and 1 GB.
        /// </summary>
        [Fact]
        public void Resolve_AutoUnit_MbRange_FormatsAsMb()
        {
            var token = new FileSizeToken();
            var item = FilterTestHelpers.CreateRenameItem(fileSize: 5L * 1024 * 1024);

            Assert.Equal("5 MB", token.Compile(arg: "")(item));
        }

        /// <summary>
        /// Verifies auto-unit selects GB at the GB threshold.
        /// </summary>
        [Fact]
        public void Resolve_AutoUnit_GbRange_FormatsAsGb()
        {
            var token = new FileSizeToken();
            var item = FilterTestHelpers.CreateRenameItem(fileSize: 3L * 1024 * 1024 * 1024);

            Assert.Equal("3 GB", token.Compile(arg: "")(item));
        }

        /// <summary>
        /// Verifies a zero-byte file still formats with the bytes unit.
        /// </summary>
        [Fact]
        public void Resolve_ZeroBytes_AutoFormatsAsBytes()
        {
            var token = new FileSizeToken();
            var item = FilterTestHelpers.CreateRenameItem(fileSize: 0);

            Assert.Equal("0 B", token.Compile(arg: "")(item));
        }

        /// <summary>
        /// Verifies the explicit auto-unit alias matches the no-arg behavior.
        /// </summary>
        [Theory]
        [InlineData("0")]
        [InlineData("auto")]
        public void Resolve_AutoUnitAliases_BehaveLikeNoArg(string unit)
        {
            var token = new FileSizeToken();
            var item = FilterTestHelpers.CreateRenameItem(fileSize: 2048);

            Assert.Equal("2 KB", token.Compile(arg: unit)(item));
        }

        /// <summary>
        /// Verifies the bytes unit accepts every documented spelling.
        /// </summary>
        [Theory]
        [InlineData("1")]
        [InlineData("b")]
        [InlineData("bytes")]
        public void Resolve_BytesUnit_AllAliases_FormatsAsBytes(string unit)
        {
            var token = new FileSizeToken();
            var item = FilterTestHelpers.CreateRenameItem(fileSize: 512);

            Assert.Equal("512 B", token.Compile(arg: unit)(item));
        }

        /// <summary>
        /// Verifies the kilobytes unit accepts numeric and string aliases.
        /// </summary>
        [Theory]
        [InlineData("2")]
        [InlineData("kb")]
        public void Resolve_KbUnit_AllAliases_FormatsAsKb(string unit)
        {
            var token = new FileSizeToken();
            var item = FilterTestHelpers.CreateRenameItem(fileSize: 3072);

            Assert.Equal("3 KB", token.Compile(arg: unit)(item));
        }

        /// <summary>
        /// Verifies the megabytes unit accepts numeric and string aliases.
        /// </summary>
        [Theory]
        [InlineData("3")]
        [InlineData("mb")]
        public void Resolve_MbUnit_AllAliases_FormatsAsMb(string unit)
        {
            var token = new FileSizeToken();
            var item = FilterTestHelpers.CreateRenameItem(fileSize: 1024 * 1024);

            Assert.Equal("1 MB", token.Compile(arg: unit)(item));
        }

        /// <summary>
        /// Verifies the gigabytes unit accepts numeric and string aliases.
        /// </summary>
        [Theory]
        [InlineData("4")]
        [InlineData("gb")]
        public void Resolve_GbUnit_AllAliases_FormatsAsGb(string unit)
        {
            var token = new FileSizeToken();
            var item = FilterTestHelpers.CreateRenameItem(fileSize: 1024L * 1024 * 1024);

            Assert.Equal("1 GB", token.Compile(arg: unit)(item));
        }

        /// <summary>
        /// Verifies the decimal-places argument controls precision.
        /// </summary>
        [Fact]
        public void Resolve_WithDecimals_FormatsWithRequestedPrecision()
        {
            var token = new FileSizeToken();
            var item = FilterTestHelpers.CreateRenameItem(fileSize: 1572864);

            Assert.Equal("1.50 MB", token.Compile(arg: "3,2")(item));
        }

        /// <summary>
        /// Verifies an unknown unit throws.
        /// </summary>
        [Fact]
        public void Resolve_UnknownUnit_Throws()
        {
            var token = new FileSizeToken();
            var item = FilterTestHelpers.CreateRenameItem(fileSize: 1024);

            Assert.Throws<NotSupportedException>(() => token.Compile(arg: "tb")(item));
        }
    }
}
