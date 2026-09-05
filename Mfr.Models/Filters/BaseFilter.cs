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
        /// <param name="original">Source instance (setup-complete flag is not copied).</param>
        /// <remarks>
        /// <para>
        /// Leaves <c>_isSetupComplete</c> false so option edits that produce a new instance via
        /// <c>with</c> re-run <see cref="_Setup"/> on the next preview. Derived private cache fields
        /// are still copied by the record <c>with</c> clone — <see cref="_Setup"/> must overwrite
        /// every one of them (see remarks on <see cref="_Setup"/>).
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

        /// <summary>
        /// Prepares this instance once before the first <see cref="Apply"/> (compile templates, build maps, validate).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Record <c>with</c> clones copy derived private fields but reset the setup-complete flag, so this
        /// method runs again on the clone. Assign every instance cache field unconditionally here —
        /// including clearing to <see langword="null"/> / default when an option is empty. Do not assign
        /// only inside an <c>if</c> for optional options; a prior non-empty cache will stick after clear.
        /// </para>
        /// </remarks>
        protected virtual void _Setup() { }
    }
}
