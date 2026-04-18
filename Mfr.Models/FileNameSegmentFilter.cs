namespace Mfr.Models
{
    /// <summary>
    /// Filter that transforms a single string segment for <see cref="FileNameTarget"/> (prefix, extension, or full name).
    /// </summary>
    /// <param name="Target">The filter target (expected to be <see cref="FileNameTarget"/> at apply time).</param>
    public abstract record FileNameSegmentFilter(FilterTarget Target) : BaseFilter
    {
        /// <inheritdoc />
        protected internal sealed override void ApplyCore(RenameItem item)
        {
            if (Target is not FileNameTarget fileTarget)
            {
                throw new NotSupportedException(
                    $"Filter '{Type}' requires a FileName target; got '{Target.GetType().Name}'.");
            }

            var sourceFileEntry = item.Preview;
            var part = fileTarget.FileNamePart;
            var partValue = part switch
            {
                FileNamePart.Prefix => sourceFileEntry.Prefix,
                FileNamePart.Extension => sourceFileEntry.Extension,
                FileNamePart.Full => sourceFileEntry.Prefix + sourceFileEntry.Extension,
                _ => throw new InvalidOperationException($"Unknown fileNamePart '{part}'.")
            };

            var transformedSegment = TransformSegment(partValue, item);
            item.SetPreviewValue(part, transformedSegment);
        }

        internal string TransformSegment(string segment, RenameItem item)
        {
            VerifySetupComplete();
            return _TransformSegment(segment, item);
        }

        /// <summary>
        /// Transforms one file-name segment after <see cref="BaseFilter.Setup"/> has run.
        /// </summary>
        protected abstract string _TransformSegment(string segment, RenameItem item);
    }
}
