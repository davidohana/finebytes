using Mfr.Filters.Formatting.Tokens.FileName;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Tests for <see cref="FileNameNumericValueToken"/>.
    /// </summary>
    public sealed class FileNameNumericValueTokenTests
    {
        /// <summary>
        /// Verifies the first digit run is returned with leading zeros stripped.
        /// </summary>
        [Theory]
        [InlineData("track01", "1")]
        [InlineData("02-song", "2")]
        [InlineData("abc123def456", "123")]
        [InlineData("file10", "10")]
        [InlineData("000", "0")]
        [InlineData("007", "7")]
        public void Resolve_PrefixWithDigits_ReturnsFirstNumber(string prefix, string expected)
        {
            var token = new FileNameNumericValueToken();
            var item = FilterTestHelpers.CreateRenameItem(prefix: prefix);

            Assert.Equal(expected, token.Compile(tokenArgs: "")(item));
            Assert.Contains("file-name-numeric-value", token.Names);
        }

        /// <summary>
        /// Verifies prefixes without digits expand to <c>0</c>.
        /// </summary>
        [Theory]
        [InlineData("track")]
        [InlineData("")]
        [InlineData("no-digits")]
        public void Resolve_PrefixWithoutDigits_ReturnsZero(string prefix)
        {
            var token = new FileNameNumericValueToken();
            var item = FilterTestHelpers.CreateRenameItem(prefix: prefix);

            Assert.Equal("0", token.Compile(tokenArgs: "")(item));
        }

        /// <summary>
        /// Verifies stray arguments are rejected.
        /// </summary>
        [Fact]
        public void Resolve_WithArgument_Throws()
        {
            var token = new FileNameNumericValueToken();

            var ex = Assert.Throws<ArgumentException>(() => token.Compile(tokenArgs: "x"));
            Assert.Contains("file-name-numeric-value", ex.Message);
        }
    }
}
