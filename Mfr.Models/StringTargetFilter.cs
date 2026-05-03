namespace Mfr.Models
{
    /// <summary>
    /// Filter that transforms one string-valued preview field identified by <see cref="FilterTarget"/>.
    /// </summary>
    /// <param name="Target">Polymorphic target (for example <see cref="FilePrefixTarget"/>).</param>
    public abstract record StringTargetFilter(FilterTarget Target) : BaseFilter
    {
        /// <inheritdoc />
        protected internal sealed override void ApplyCore(RenameItem item)
        {
            var meta = item.Preview;
            var current = meta.GetTargetString(Target);
            var transformed = TransformValue(current, item);
            meta.SetTargetString(Target, transformed);
        }

        internal string TransformValue(string value, RenameItem item)
        {
            VerifySetupComplete();
            return _TransformValue(value, item);
        }

        /// <summary>
        /// Transforms one string after <see cref="BaseFilter.Setup"/> has completed.
        /// </summary>
        /// <param name="value">The current preview string for this filter's target.</param>
        /// <param name="item">The item being renamed.</param>
        /// <returns>The transformed string to write back to the preview field.</returns>
        protected abstract string _TransformValue(string value, RenameItem item);
    }
}
