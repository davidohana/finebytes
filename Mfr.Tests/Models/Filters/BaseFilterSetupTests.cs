using Mfr.Filters;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters
{
    /// <summary>
    /// Tests setup lifecycle behavior on <see cref="BaseFilter"/>.
    /// </summary>
    public sealed class BaseFilterSetupTests
    {
        private static readonly FileNameTarget _target = new(FileNamePart.Prefix);

        /// <summary>
        /// Verifies setup is invoked once per filter instance lifetime.
        /// </summary>
        [Fact]
        public void Apply_SetupRunsOncePerInstanceLifetime()
        {
            var filter = new SetupCountingFilter(Enabled: true, Target: _target);
            filter.Setup();

            var firstItem = FilterTestHelpers.CreateFile(prefix: "first");
            filter.Apply(firstItem);
            var secondItem = FilterTestHelpers.CreateFile(prefix: "second");
            filter.Apply(secondItem);

            Assert.Equal(1, filter.SetupCount);
            Assert.Equal("first-1", firstItem.Preview.Prefix);
            Assert.Equal("second-1", secondItem.Preview.Prefix);
        }

        /// <summary>
        /// Verifies transform guard rejects direct transform calls before setup.
        /// </summary>
        [Fact]
        public void TransformSegment_SetupNotRun_ThrowsInvalidOperationException()
        {
            var filter = new SetupCountingFilter(Enabled: true, Target: _target);
            var item = FilterTestHelpers.CreateFile(prefix: "first");

            var ex = Assert.Throws<InvalidOperationException>(() => filter.TransformSegment(segment: "value", item: item));
            Assert.Contains("setup must complete before transform", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        private sealed record SetupCountingFilter(bool Enabled, FilterTarget Target) : BaseFilter(Enabled, Target)
        {
            public override string Type => "SetupCounting";

            public int SetupCount { get; private set; }

            protected override void _Setup()
            {
                SetupCount++;
            }

            protected override string _TransformSegment(string segment, RenameItem item)
            {
                return $"{segment}-{SetupCount}";
            }
        }
    }
}
