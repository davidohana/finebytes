using Mfr.Filters.Formatting.Tokens;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens
{
    /// <summary>
    /// Tests for shared named-option split/parse used by Compile and Format Editor.
    /// </summary>
    public sealed class FormatOptionsParsingTests
    {
        /// <summary>
        /// Verifies commas inside nested <c>&lt;…&gt;</c> are not segment separators.
        /// </summary>
        [Fact]
        public void SplitNamedArgumentSegments_PreservesCommasInsideAngleBrackets()
        {
            var segments = FormatOptionsParsing.SplitNamedArgumentSegments(
                "start=1,end=5,source=<substr:start=2,end=3,source=<full-name>>"
            );

            Assert.Equal(["start=1", "end=5", "source=<substr:start=2,end=3,source=<full-name>>"], segments);
        }

        /// <summary>
        /// Verifies a trailing segment after the last comma is included.
        /// </summary>
        [Fact]
        public void SplitNamedArgumentSegments_IncludesFinalSegment()
        {
            Assert.Equal(["a=1", "b=2"], FormatOptionsParsing.SplitNamedArgumentSegments("a=1,b=2"));
            Assert.Equal([""], FormatOptionsParsing.SplitNamedArgumentSegments(""));
        }

        /// <summary>
        /// Verifies successful named key=value parse with trimmed keys/values and case-insensitive keys.
        /// </summary>
        [Fact]
        public void ParseNamedKeyValuePairs_ValidPairs_ReturnsMap()
        {
            var map = FormatOptionsParsing.ParseNamedKeyValuePairs(
                " start = 1 , End=-1 , source= <file-name> ",
                "<substr>"
            );

            Assert.Equal(3, map.Count);
            Assert.Equal("1", map["start"]);
            Assert.Equal("-1", map["END"]);
            Assert.Equal("<file-name>", map["source"]);
        }

        /// <summary>
        /// Verifies nested source tokens round-trip through parse without splitting inner commas.
        /// </summary>
        [Fact]
        public void ParseNamedKeyValuePairs_NestedSource_KeepsInnerCommas()
        {
            var map = FormatOptionsParsing.ParseNamedKeyValuePairs(
                "tokenNumber=1,separator=-,includeNext=false,includePrev=false,source=<token:tokenNumber=2,separator=_,includeNext=false,includePrev=false,source=<full-name>>",
                "<token>"
            );

            Assert.Equal(
                "<token:tokenNumber=2,separator=_,includeNext=false,includePrev=false,source=<full-name>>",
                map["source"]
            );
        }

        /// <summary>
        /// Verifies empty segments, missing equals, empty keys, and duplicate keys throw.
        /// </summary>
        [Theory]
        [InlineData("start=1,", "empty name=value segment")]
        [InlineData("start", "not a valid name=value pair")]
        [InlineData("=1", "not a valid name=value pair")]
        [InlineData(" =1", "not a valid name=value pair")]
        [InlineData("start=1,start=2", "duplicate option 'start'")]
        public void ParseNamedKeyValuePairs_Invalid_Throws(string arg, string messageFragment)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                FormatOptionsParsing.ParseNamedKeyValuePairs(arg, "<substr>")
            );

            Assert.Contains(messageFragment, ex.Message, StringComparison.Ordinal);
            Assert.Equal("arg", ex.ParamName);
        }
    }
}
