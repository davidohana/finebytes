using Mfr.Filters.Replace;
using Mfr.Models;
using Mfr.Tests.Models.Filters;

namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests for <see cref="FilterChain"/> orchestration (setup, apply order, enabled flags).
    /// </summary>
    public sealed class FilterChainTests
    {
        private static readonly FileNameTarget _target = new(FileNamePart.Prefix);

        /// <summary>
        /// Verifies <see cref="FilterChain.CreateAllEnabled"/> with no filters yields an empty step list.
        /// </summary>
        [Fact]
        public void CreateAllEnabled_Empty_YieldsNoSteps()
        {
            var chain = FilterChain.CreateAllEnabled([]);

            Assert.Empty(chain.Steps);
        }

        /// <summary>
        /// Verifies <see cref="FilterChain.CreateAllEnabled"/> wraps each filter as an enabled step in order.
        /// </summary>
        [Fact]
        public void CreateAllEnabled_AppliesFiltersInOrder()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "ab");
            var chain = FilterChain.CreateAllEnabled(
            [
                new ReplacerFilter(
                    Target: _target,
                    Options: new ReplacerOptions("a", "x", ReplacerMode.Literal, CaseSensitive: true, ReplaceAll: true, WholeWord: false)),
                new ReplacerFilter(
                    Target: _target,
                    Options: new ReplacerOptions("b", "y", ReplacerMode.Literal, CaseSensitive: true, ReplaceAll: true, WholeWord: false))
            ]);

            Assert.All(chain.Steps, step => Assert.True(step.Enabled));

            chain.SetupFilters();
            chain.ApplyFilters(item);

            Assert.Equal("xy", item.Preview.Prefix);
        }

        /// <summary>
        /// Verifies an empty chain does not change the preview after apply (only clear + no transforms).
        /// </summary>
        [Fact]
        public void ApplyFilters_EmptyChain_LeavesPreviewMatchingOriginal()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "only");
            var chain = new FilterChain { Steps = [] };

            chain.SetupFilters();
            chain.ApplyFilters(item);

            Assert.Equal(item.Original.FullPath, item.Preview.FullPath);
            Assert.Equal(item.Original.Prefix, item.Preview.Prefix);
        }

        /// <summary>
        /// Verifies <see cref="FilterChain.SetupFilters"/> runs setup for every step, including disabled steps.
        /// </summary>
        [Fact]
        public void SetupFilters_RunsForDisabledStepsToo()
        {
            var disabled = new SetupCountingFilter(Target: _target);
            var enabled = new SetupCountingFilter(Target: _target);
            var chain = new FilterChain
            {
                Steps =
                [
                    new FilterChainStep(Enabled: false, Filter: disabled),
                    new FilterChainStep(Enabled: true, Filter: enabled)
                ]
            };

            chain.SetupFilters();

            Assert.Equal(1, disabled.SetupCount);
            Assert.Equal(1, enabled.SetupCount);
        }

        private sealed record SetupCountingFilter(FilterTarget Target) : StringTargetFilter(Target)
        {
            public override string Type => "SetupCounting";

            public int SetupCount { get; private set; }

            protected override void _Setup()
            {
                SetupCount++;
            }

            protected override string _TransformValue(string value, RenameItem item)
            {
                return value;
            }
        }
    }
}
