using System.Collections.Immutable;
using Mfr.Utils;

namespace Mfr.Tests.Utils
{
    /// <summary>
    /// Tests for <see cref="DelimitedText"/>.
    /// </summary>
    public sealed class DelimitedTextTests
    {
        /// <summary>
        /// Verifies blank input has no list parts.
        /// </summary>
        /// <param name="joined">Delimited text under test.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(";")]
        [InlineData(" ; ; ")]
        public void Split_blank_returns_empty(string? joined)
        {
            var result = DelimitedText.Split(joined);

            Assert.Empty(result);
        }

        /// <summary>
        /// Verifies splitting trims each part and drops the blank ones.
        /// </summary>
        [Fact]
        public void Split_trims_parts_and_drops_blanks()
        {
            var result = DelimitedText.Split("  Alice ;; Bob ; ");

            Assert.Equal(["Alice", "Bob"], [.. result]);
        }

        /// <summary>
        /// Verifies text without a separator yields one value.
        /// </summary>
        [Fact]
        public void Split_without_separator_returns_single_value()
        {
            var result = DelimitedText.Split(" Solo Artist ");

            Assert.Equal(["Solo Artist"], [.. result]);
        }

        /// <summary>
        /// Verifies joining uses a semicolon and a space.
        /// </summary>
        [Fact]
        public void Join_uses_semicolon_space()
        {
            var result = DelimitedText.Join(["Alice", "Bob"]);

            Assert.Equal("Alice; Bob", result);
        }

        /// <summary>
        /// Verifies joining trims values and skips the blank ones.
        /// </summary>
        [Fact]
        public void Join_trims_values_and_skips_blanks()
        {
            var result = DelimitedText.Join([" Alice ", "  ", "Bob"]);

            Assert.Equal("Alice; Bob", result);
        }

        /// <summary>
        /// Verifies an empty array joins to an empty string rather than throwing.
        /// </summary>
        [Fact]
        public void Join_empty_returns_empty_string()
        {
            Assert.Equal(string.Empty, DelimitedText.Join([]));
        }

        /// <summary>
        /// Verifies a default array is treated as empty.
        /// </summary>
        [Fact]
        public void Join_default_array_returns_empty_string()
        {
            Assert.Equal(string.Empty, DelimitedText.Join(default));
        }

        /// <summary>
        /// Verifies a list with nothing left after trimming is reported as absent.
        /// </summary>
        [Fact]
        public void JoinOrNull_all_blank_returns_null()
        {
            Assert.Null(DelimitedText.JoinOrNull(["", "   "]));
        }

        /// <summary>
        /// Verifies a default array is reported as absent.
        /// </summary>
        [Fact]
        public void JoinOrNull_default_array_returns_null()
        {
            Assert.Null(DelimitedText.JoinOrNull(default));
        }

        /// <summary>
        /// Verifies a null sequence is reported as absent.
        /// </summary>
        [Fact]
        public void JoinOrNull_null_sequence_returns_null()
        {
            Assert.Null(DelimitedText.JoinOrNull(null));
        }

        /// <summary>
        /// Verifies the sequence overload joins surviving values.
        /// </summary>
        [Fact]
        public void JoinOrNull_sequence_joins_non_blank_values()
        {
            var result = DelimitedText.JoinOrNull(new List<string> { "Alice", " ", " Bob" });

            Assert.Equal("Alice; Bob", result);
        }

        /// <summary>
        /// Verifies joining then splitting returns the original values.
        /// </summary>
        [Fact]
        public void Join_then_Split_round_trips_values()
        {
            ImmutableArray<string> values = ["Alice", "Bob", "Carol"];

            var result = DelimitedText.Split(DelimitedText.Join(values));

            Assert.Equal(values.ToArray(), result.ToArray());
        }

        /// <summary>
        /// Verifies a null sequence trims to no values.
        /// </summary>
        [Fact]
        public void TrimNonEmpty_null_returns_empty()
        {
            Assert.Empty(DelimitedText.TrimNonEmpty(null));
        }

        /// <summary>
        /// Verifies trimming keeps order, trims each value, and drops the blank ones.
        /// </summary>
        [Fact]
        public void TrimNonEmpty_trims_and_drops_blanks_in_order()
        {
            var result = DelimitedText.TrimNonEmpty([" b ", "", "a", "\t"]);

            Assert.Equal(["b", "a"], [.. result]);
        }

        /// <summary>
        /// Verifies an embedded separator survives trimming (only splitting divides values).
        /// </summary>
        [Fact]
        public void TrimNonEmpty_keeps_embedded_separator()
        {
            var result = DelimitedText.TrimNonEmpty(["Alice; Bob"]);

            Assert.Equal(["Alice; Bob"], [.. result]);
        }
    }
}
