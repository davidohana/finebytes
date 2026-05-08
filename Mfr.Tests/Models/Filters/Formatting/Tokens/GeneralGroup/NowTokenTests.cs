using System.Globalization;
using Mfr.Filters.Formatting.Tokens.GeneralGroup;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.GeneralGroup
{
    /// <summary>
    /// Tests for <see cref="NowToken"/>.
    /// </summary>
    public sealed class NowTokenTests
    {
        /// <summary>
        /// Verifies the no-arg form returns a round-trip ISO 8601 UTC string.
        /// </summary>
        [Fact]
        public void Resolve_NoArg_ProducesParseableUtcString()
        {
            var token = new NowToken();
            var item = FilterTestHelpers.CreateRenameItem();

            var result = token.Resolve(arg: "", item: item);

            Assert.True(
                DateTimeOffset.TryParse(
                    result,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var parsed));
            Assert.Equal(DateTimeKind.Utc, parsed.UtcDateTime.Kind);
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

            Assert.Equal(expectedYear, token.Resolve(arg: "yyyy", item: item));
        }
    }
}
