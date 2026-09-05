using Mfr.Filters;

namespace Mfr.Tests.Models.Filters
{
    /// <summary>
    /// Tests setup lifecycle behavior on <see cref="BaseFilter"/>.
    /// </summary>
    public sealed class BaseFilterSetupTests
    {
        private static readonly FilePrefixTarget _target = new();

        /// <summary>
        /// Verifies setup is invoked once per filter instance lifetime.
        /// </summary>
        [Fact]
        public void Apply_SetupRunsOncePerInstanceLifetime()
        {
            var filter = new SetupCountingFilter(Target: _target);
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
        public void TransformValue_SetupNotRun_ThrowsInvalidOperationException()
        {
            var filter = new SetupCountingFilter(Target: _target);
            var item = FilterTestHelpers.CreateRenameItem(prefix: "first");

            var ex = Assert.Throws<InvalidOperationException>(() => filter.TransformValue(value: "value", item: item));
            Assert.Contains("setup must complete before transform", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that setup failures propagate and keep the filter unusable for transform/apply.
        /// </summary>
        [Fact]
        public void Setup_WhenSetupThrows_PropagatesAndApplyStillFails()
        {
            var filter = new ThrowingSetupFilter(Target: _target);
            var item = FilterTestHelpers.CreateRenameItem(prefix: "first");

            var setupEx = Assert.Throws<InvalidOperationException>(filter.Setup);
            Assert.Equal("Setup failed.", setupEx.Message);

            var applyEx = Assert.Throws<InvalidOperationException>(() => filter.Apply(item));
            Assert.Contains(
                "setup must complete before transform",
                applyEx.Message,
                StringComparison.OrdinalIgnoreCase
            );
        }

        /// <summary>
        /// Verifies a <c>with</c> clone must run setup again (setup-complete flag is not copied).
        /// </summary>
        [Fact]
        public void With_clone_is_not_already_set_up()
        {
            var filter = new SetupCountingFilter(Target: _target);
            filter.Setup();
            Assert.Equal(1, filter.SetupCount);

            var clone = filter with { };
            clone.Setup();

            Assert.Equal(1, filter.SetupCount);
            Assert.Equal(2, clone.SetupCount);

            var item = FilterTestHelpers.CreateRenameItem(prefix: "name");
            clone.Apply(item);
            Assert.Equal("name-2", item.Preview.Prefix);
        }

        /// <summary>
        /// Verifies clearing an optional setup cache via <c>with</c> does not keep the prior compiled value.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Record <c>with</c> copies private instance fields; <see cref="BaseFilter._Setup"/> must
        /// assign every cache unconditionally (including null when the option is empty).
        /// </para>
        /// </remarks>
        [Fact]
        public void With_clearing_optional_setup_cache_drops_prior_value()
        {
            var filter = new OptionalCacheFilter(Template: "abc");
            filter.Setup();

            var cleared = filter with { Template = "" };
            var item = FilterTestHelpers.CreateRenameItem(prefix: "name");
            cleared.Setup();
            cleared.Apply(item);

            Assert.Equal("none", item.Preview.Prefix);
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
                return $"{value}-{SetupCount}";
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

        /// <summary>
        /// Stand-in for filters with an optional compiled field (same shape as Mover sub-folder).
        /// </summary>
        private sealed record OptionalCacheFilter(string Template) : BaseFilter
        {
            private string? _compiled;

            public override string Type => "OptionalCache";

            protected override void _Setup()
            {
                _compiled = string.IsNullOrEmpty(Template) ? null : Template.ToUpperInvariant();
            }

            protected internal override void ApplyCore(RenameItem item)
            {
                VerifySetupComplete();
                item.Preview.Prefix = _compiled ?? "none";
            }
        }
    }
}
