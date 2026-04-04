using System.Text.Json.Serialization;
using Mfr.Models.Filters.Advanced;
using Mfr.Models.Filters.Text;

namespace Mfr.Models.Filters
{
    /// <summary>
    /// Represents a polymorphic filter.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(LettersCaseFilter), "LettersCase")]
    [JsonDerivedType(typeof(SpaceCharacterFilter), "SpaceCharacter")]
    [JsonDerivedType(typeof(RemoveSpacesFilter), "RemoveSpaces")]
    [JsonDerivedType(typeof(ShrinkSpacesFilter), "ShrinkSpaces")]
    [JsonDerivedType(typeof(TrimLeftFilter), "TrimLeft")]
    [JsonDerivedType(typeof(TrimRightFilter), "TrimRight")]
    [JsonDerivedType(typeof(ExtractLeftFilter), "ExtractLeft")]
    [JsonDerivedType(typeof(ExtractRightFilter), "ExtractRight")]
    [JsonDerivedType(typeof(ReplacerFilter), "Replacer")]
    [JsonDerivedType(typeof(FormatterFilter), "Formatter")]
    [JsonDerivedType(typeof(CounterFilter), "Counter")]
    [JsonDerivedType(typeof(CleanerFilter), "Cleaner")]
    [JsonDerivedType(typeof(FixLeadingZerosFilter), "FixLeadingZeros")]
    [JsonDerivedType(typeof(StripParenthesesFilter), "StripParentheses")]
    public abstract record Filter(bool Enabled, FilterTarget Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        [JsonIgnore]
        public abstract string Type { get; }

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

            var sourceFileEntry = item.Preview ?? item.Original;
            var mode = fileTarget.FileNameMode;
            var segment = mode switch
            {
                FileNameTargetMode.Prefix => sourceFileEntry.Prefix,
                FileNameTargetMode.Extension => sourceFileEntry.Extension,
                FileNameTargetMode.Full => sourceFileEntry.Prefix + sourceFileEntry.Extension,
                _ => throw new InvalidOperationException($"Unknown fileNameMode '{mode}'.")
            };

            var transformedSegment = ApplySegment(segment, item);
            item.SetPreviewSegment(mode, transformedSegment);
        }

        internal abstract string ApplySegment(string segment, RenameItem item);
    }
}
