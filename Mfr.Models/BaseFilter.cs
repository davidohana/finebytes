using System.Text.Json.Serialization;

namespace Mfr.Models
{
    /// <summary>
    /// Represents a polymorphic filter.
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    public abstract record BaseFilter(FilterTarget Target)
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
        /// Ensures <see cref="Setup"/> has completed (for helpers such as <see cref="FileNameSegmentFilter.TransformSegment"/>).
        /// </summary>
        protected void VerifySetupComplete()
        {
            if (!_isSetupComplete)
            {
                throw new InvalidOperationException($"Filter '{Type}' setup must complete before transform.");
            }
        }

        /// <summary>
        /// Applies this filter to the rename item. File-name text filters use <see cref="FileNameSegmentFilter"/>.
        /// </summary>
        /// <param name="item">The item whose preview is updated.</param>
        protected internal abstract void ApplyCore(RenameItem item);

        protected virtual void _Setup()
        {
        }
    }
}
