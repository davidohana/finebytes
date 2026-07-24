using Mfr.Utils;

namespace Mfr.Tests.Utils
{
    /// <summary>
    /// Tests for <see cref="Nullables"/>.
    /// </summary>
    public sealed class NullablesTests
    {
        /// <summary>
        /// Verifies the class overload returns the first non-null candidate.
        /// </summary>
        [Fact]
        public void FirstNonNull_class_returns_first_non_null()
        {
            var result = Nullables.FirstNonNull(null, "a", "b");

            Assert.Equal("a", result);
        }

        /// <summary>
        /// Verifies the class overload returns null when every candidate is null.
        /// </summary>
        [Fact]
        public void FirstNonNull_class_all_null_returns_null()
        {
            var result = Nullables.FirstNonNull<string>(null, null);

            Assert.Null(result);
        }

        /// <summary>
        /// Verifies the class overload returns null when no candidates are supplied.
        /// </summary>
        [Fact]
        public void FirstNonNull_class_empty_returns_null()
        {
            var result = Nullables.FirstNonNull<string>();

            Assert.Null(result);
        }

        /// <summary>
        /// Verifies the struct overload returns the first candidate that has a value.
        /// </summary>
        [Fact]
        public void FirstNonNull_struct_returns_first_with_value()
        {
            var result = Nullables.FirstNonNull((uint?)null, 7u, 9u);

            Assert.Equal(7u, result);
        }

        /// <summary>
        /// Verifies the struct overload returns null when every candidate is null.
        /// </summary>
        [Fact]
        public void FirstNonNull_struct_all_null_returns_null()
        {
            var result = Nullables.FirstNonNull((uint?)null, null);

            Assert.Null(result);
        }

        /// <summary>
        /// Verifies the struct overload returns null when no candidates are supplied.
        /// </summary>
        [Fact]
        public void FirstNonNull_struct_empty_returns_null()
        {
            var result = Nullables.FirstNonNull<uint>();

            Assert.Null(result);
        }
    }
}
