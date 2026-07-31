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
        /// Gets the filter type discriminator.
        /// </summary>
        [JsonIgnore]
        public abstract string Type { get; }

        internal void Setup()
        {
            if (_isSetupComplete)
                return;

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

        protected virtual void _Setup()
        {
        }
    }
}
