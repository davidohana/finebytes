using Mfr.Utils;

namespace Mfr.Tests.Utils
{
    /// <summary>
    /// Tests for <see cref="OrdinalSequence"/>.
    /// </summary>
    public sealed class OrdinalSequenceTests
    {
        /// <summary>
        /// Verifies equal sequences compare as equal.
        /// </summary>
        [Fact]
        public void Compare_equal_sequences_returns_zero()
        {
            Assert.Equal(0, OrdinalSequence.Compare(["a", "b"], ["a", "b"]));
        }

        /// <summary>
        /// Verifies ordering is decided by the first differing element.
        /// </summary>
        [Fact]
        public void Compare_orders_by_first_difference()
        {
            Assert.True(OrdinalSequence.Compare(["a", "b"], ["a", "c"]) < 0);
            Assert.True(OrdinalSequence.Compare(["a", "c"], ["a", "b"]) > 0);
        }

        /// <summary>
        /// Verifies a prefix sorts before the longer sequence it prefixes.
        /// </summary>
        [Fact]
        public void Compare_prefix_sorts_before_longer()
        {
            Assert.True(OrdinalSequence.Compare(["a"], ["a", "b"]) < 0);
            Assert.True(OrdinalSequence.Compare(["a", "b"], ["a"]) > 0);
        }

        /// <summary>
        /// Verifies comparison is ordinal, so uppercase sorts before lowercase.
        /// </summary>
        [Fact]
        public void Compare_is_ordinal()
        {
            Assert.True(OrdinalSequence.Compare(["B"], ["a"]) < 0);
        }

        /// <summary>
        /// Verifies a default array compares as an empty one.
        /// </summary>
        [Fact]
        public void Compare_default_counts_as_empty()
        {
            Assert.Equal(0, OrdinalSequence.Compare(default, []));
            Assert.True(OrdinalSequence.Compare(default, ["a"]) < 0);
        }

        /// <summary>
        /// Verifies equal values in the same order are reported equal.
        /// </summary>
        [Fact]
        public void AreEqual_same_values_returns_true()
        {
            Assert.True(OrdinalSequence.AreEqual(["a", "b"], ["a", "b"]));
        }

        /// <summary>
        /// Verifies value order is part of equality.
        /// </summary>
        [Fact]
        public void AreEqual_reordered_values_returns_false()
        {
            Assert.False(OrdinalSequence.AreEqual(["a", "b"], ["b", "a"]));
        }

        /// <summary>
        /// Verifies equality is case-sensitive.
        /// </summary>
        [Fact]
        public void AreEqual_case_difference_returns_false()
        {
            Assert.False(OrdinalSequence.AreEqual(["Alice"], ["alice"]));
        }

        /// <summary>
        /// Verifies a default array equals an empty one.
        /// </summary>
        [Fact]
        public void AreEqual_default_equals_empty()
        {
            Assert.True(OrdinalSequence.AreEqual(default, []));
        }

        /// <summary>
        /// Verifies sequences of different length are not equal.
        /// </summary>
        [Fact]
        public void AreEqual_different_length_returns_false()
        {
            Assert.False(OrdinalSequence.AreEqual(["a"], ["a", "b"]));
        }
    }
}
