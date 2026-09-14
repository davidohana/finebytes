namespace Mfr.Tests.Models.Media
{
    /// <summary>
    /// Tests for <see cref="TimestampFieldKeywords"/>.
    /// </summary>
    public sealed class TimestampFieldKeywordsTests
    {
        /// <summary>
        /// Verifies keywords match <see cref="TimestampField"/> JSON enum member names.
        /// </summary>
        [Theory]
        [InlineData("creation", TimestampField.Creation)]
        [InlineData("lastWrite", TimestampField.LastWrite)]
        [InlineData("lastAccess", TimestampField.LastAccess)]
        [InlineData("CREATION", TimestampField.Creation)]
        [InlineData("LastWrite", TimestampField.LastWrite)]
        public void TryParse_And_GetKeyword_MatchJsonEnumNames(string keyword, TimestampField expected)
        {
            Assert.True(TimestampFieldKeywords.TryParse(keyword, out var parsed));
            Assert.Equal(expected, parsed);
            Assert.Equal(keyword, TimestampFieldKeywords.GetKeyword(expected), ignoreCase: true);
            Assert.Contains(TimestampFieldKeywords.GetKeyword(expected), TimestampFieldKeywords.Keywords);
        }

        /// <summary>
        /// Verifies blank and unknown keywords fail parse.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("modified")]
        public void TryParse_UnknownOrBlank_ReturnsFalse(string? raw)
        {
            Assert.False(TimestampFieldKeywords.TryParse(raw, out _));
        }
    }
}
