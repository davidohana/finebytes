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

            var firstItem = FilterTestHelpers.CreateRenameItem(prefix: "first");
            filter.Apply(firstItem);
            var secondItem = FilterTestHelpers.CreateRenameItem(prefix: "second");
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
            var item = FilterTestHelpers.CreateRenameItem(prefix: "first");

            var ex = Assert.Throws<InvalidOperationException>(() => filter.TransformSegment(segment: "value", item: item));
            Assert.Contains("setup must complete before transform", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that setup failures propagate and keep the filter unusable for transform/apply.
        /// </summary>
        [Fact]
        public void Setup_WhenSetupThrows_PropagatesAndApplyStillFails()
        {
            var filter = new ThrowingSetupFilter(Enabled: true, Target: _target);
            var item = FilterTestHelpers.CreateRenameItem(prefix: "first");

            var setupEx = Assert.Throws<InvalidOperationException>(filter.Setup);
            Assert.Equal("Setup failed.", setupEx.Message);

            var applyEx = Assert.Throws<InvalidOperationException>(() => filter.Apply(item));
            Assert.Contains("setup must complete before transform", applyEx.Message, StringComparison.OrdinalIgnoreCase);
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

        private sealed record ThrowingSetupFilter(bool Enabled, FilterTarget Target) : BaseFilter(Enabled, Target)
        {
            public override string Type => "ThrowingSetup";

            protected override void _Setup()
            {
                throw new InvalidOperationException("Setup failed.");
            }

            protected override string _TransformSegment(string segment, RenameItem item)
            {
                return segment;
            }
        }
    }
}
