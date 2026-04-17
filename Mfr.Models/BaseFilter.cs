using System.Text.Json.Serialization;

namespace Mfr.Models
{
    /// <summary>
    /// Represents a polymorphic filter.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    public abstract record BaseFilter(bool Enabled, FilterTarget Target)
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
            if (!Enabled)
            {
                return;
            }

            if (Target is not FileNameTarget fileTarget)
            {
                throw new NotSupportedException($"Phase 1 only supports target.family='FileName'. Filter '{Type}' got '{Target.Family}'.");
            }

            if (!_isSetupComplete)
            {
                throw new InvalidOperationException($"Filter '{Type}' setup must complete before transform.");
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
            if (!_isSetupComplete)
            {
                throw new InvalidOperationException($"Filter '{Type}' setup must complete before transform.");
            }

            return _TransformSegment(segment, item);
        }

        protected virtual void _Setup()
        {
        }

        protected abstract string _TransformSegment(string segment, RenameItem item);
    }
}
