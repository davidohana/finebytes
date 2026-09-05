using Mfr.Filters;
using Mfr.Filters.Replace;
using Mfr.Tests.Models.Filters;

namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests for <see cref="FilterChain"/> orchestration (setup, apply order, enabled flags).
    /// </summary>
    public sealed class FilterChainTests
    {
        private static readonly FilePrefixTarget _target = new();

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
            var chain = FilterChain.CreateAllEnabled([
                new ReplacerFilter(
                    Target: _target,
                    Options: new ReplacerOptions(
                        "a",
                        "x",
                        Match: new ReplacerMatchOptions(
                            Mode: ReplacerMode.Literal,
                            CaseSensitive: true,
                            ReplaceAll: true,
                            WholeWord: false
                        )
                    )
                ),
                new ReplacerFilter(
                    Target: _target,
                    Options: new ReplacerOptions(
                        "b",
                        "y",
                        Match: new ReplacerMatchOptions(
                            Mode: ReplacerMode.Literal,
                            CaseSensitive: true,
                            ReplaceAll: true,
                            WholeWord: false
                        )
                    )
                ),
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
        /// Verifies <see cref="FilterChain.SetupFilters"/> skips disabled steps.
        /// </summary>
        [Fact]
        public void SetupFilters_SkipsDisabledSteps()
        {
            var disabled = new SetupCountingFilter(Target: _target);
            var enabled = new SetupCountingFilter(Target: _target);
            var chain = new FilterChain
            {
                Steps =
                [
                    new FilterChainStep(Enabled: false, Filter: disabled),
                    new FilterChainStep(Enabled: true, Filter: enabled),
                ],
            };

            chain.SetupFilters();

            Assert.Equal(0, disabled.SetupCount);
            Assert.Equal(1, enabled.SetupCount);
        }

        /// <summary>
        /// Verifies a disabled step whose setup would throw does not fail <see cref="FilterChain.SetupFilters"/>.
        /// </summary>
        [Fact]
        public void SetupFilters_DisabledThrowingSetup_DoesNotThrow()
        {
            var chain = new FilterChain
            {
                Steps = [new FilterChainStep(Enabled: false, Filter: new ThrowingSetupFilter(Target: _target))],
            };

            chain.SetupFilters();
        }

        /// <summary>
        /// Verifies an enabled setup failure is wrapped with the filter type name.
        /// </summary>
        [Fact]
        public void SetupFilters_ThrowingSetup_WrapsWithFilterType()
        {
            var chain = FilterChain.CreateAllEnabled([new ThrowingSetupFilter(Target: _target)]);

            var ex = Assert.Throws<InvalidOperationException>(chain.SetupFilters);

            Assert.Contains("ThrowingSetup", ex.Message, StringComparison.Ordinal);
            Assert.Contains("Setup failed.", ex.Message, StringComparison.Ordinal);
            Assert.IsType<InvalidOperationException>(ex.InnerException);
        }

        private sealed record SetupCountingFilter(FilterTarget Target, StringApplyScope? ApplyScope = null)
            : StringTargetFilter(Target, ApplyScope)
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

        private sealed record ThrowingSetupFilter(FilterTarget Target, StringApplyScope? ApplyScope = null)
            : StringTargetFilter(Target, ApplyScope)
        {
            public override string Type => "ThrowingSetup";

            protected override void _Setup()
            {
                throw new InvalidOperationException("Setup failed.");
            }

            protected override string _TransformValue(string value, RenameItem item)
            {
                return value;
            }
        }
    }
}
