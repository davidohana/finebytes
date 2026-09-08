using System.Globalization;
using Mfr.Filters.Formatting.Tokens.Generators;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.Generators
{
    /// <summary>
    /// Tests for <see cref="NowToken"/>.
    /// </summary>
    public sealed class NowTokenTests
    {
        /// <summary>
        /// Verifies the no-arg form uses <c>yyyy-MM-dd_HH-mm-ss</c>.
        /// </summary>
        [Fact]
        public void Resolve_NoArg_UsesDefaultFormat()
        {
            var token = new NowToken();
            var item = FilterTestHelpers.CreateRenameItem();
            var before = DateTimeOffset.UtcNow;

            var result = token.Compile(tokenArgs: "")(item);

            Assert.True(
                DateTimeOffset.TryParseExact(
                    result,
                    "yyyy-MM-dd_HH-mm-ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var parsed
                )
            );
            var after = DateTimeOffset.UtcNow;
            Assert.InRange(parsed, before.AddSeconds(-1), after.AddSeconds(1));
        }

        /// <summary>
        /// Verifies a custom format string is honored.
        /// </summary>
        [Fact]
        public void Resolve_CustomFormat_UsesSuppliedFormat()
        {
            var token = new NowToken();
            var item = FilterTestHelpers.CreateRenameItem();
            var expectedYear = DateTimeOffset.UtcNow.ToString("yyyy", CultureInfo.InvariantCulture);

            Assert.Equal(expectedYear, token.Compile(tokenArgs: "yyyy")(item));
        }
    }
}
