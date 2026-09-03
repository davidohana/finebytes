using System.Text.Json.Serialization;
using Mfr.Models.Rename;
using Mfr.Utils;

namespace Mfr.Models.Filters
{
    /// <summary>
    /// Represents a polymorphic filter.
    /// </summary>
    public abstract record BaseFilter
    {
        private bool _isSetupComplete;

        /// <summary>
        /// Initializes a filter that has not completed setup.
        /// </summary>
        protected BaseFilter() { }

        /// <summary>
        /// Copy constructor used by <c>with</c> expressions.
        /// </summary>
        /// <param name="original">Source instance (setup state is not copied).</param>
        /// <remarks>
        /// <para>
        /// Leaves <c>_isSetupComplete</c> false so option edits that produce a new instance via
        /// <c>with</c> re-run <see cref="_Setup"/> on the next preview (cached setup data must not stick).
        /// </para>
        /// </remarks>
        protected BaseFilter(BaseFilter original)
        {
            _ = original;
        }

        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        [JsonIgnore]
        public abstract string Type { get; }

        /// <summary>
        /// Runs one-time preparation before this filter is applied.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Idempotent: subsequent calls are no-ops. Call once per instance before the first
        /// <see cref="Apply"/> (typically via <see cref="FilterChain.SetupFilters"/>).
        /// </para>
        /// </remarks>
        internal void Setup()
        {
            if (_isSetupComplete)
            {
                return;
            }

            _Setup();
            _isSetupComplete = true;
        }

        internal void Apply(RenameItem item)
        {
            VerifySetupComplete();
            ApplyCore(item);
        }

        /// <summary>
        /// Ensures <see cref="Setup"/> has completed (for helpers such as <c>StringTargetFilter.TransformValue</c>).
        /// </summary>
        protected void VerifySetupComplete()
        {
            Check.That(_isSetupComplete, $"Filter '{Type}' setup must complete before transform.");
        }

        /// <summary>
        /// Applies this filter to the rename item. String-valued field filters subclass <c>StringTargetFilter</c>
        /// (in assembly <c>Mfr.Filters</c>, so preview audio fields can call <c>Mfr.Metadata</c> without a Models↔Metadata cycle).
        /// </summary>
        /// <param name="item">The item whose preview is updated.</param>
        protected internal abstract void ApplyCore(RenameItem item);

        protected virtual void _Setup() { }
    }
}
