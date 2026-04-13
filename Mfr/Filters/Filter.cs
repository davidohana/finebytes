using System.Text.Json.Serialization;
using Mfr.Filters.Case;
using Mfr.Filters.Formatting;
using Mfr.Filters.Misc;
using Mfr.Filters.Replace;
using Mfr.Filters.Space;
using Mfr.Filters.Trimming;
using Mfr.Models;

namespace Mfr.Filters
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
    [JsonDerivedType(typeof(ShrinkDuplicateCharactersFilter), "ShrinkDuplicateCharacters")]
    [JsonDerivedType(typeof(FormatterFilter), "Formatter")]
    [JsonDerivedType(typeof(CounterFilter), "Counter")]
    [JsonDerivedType(typeof(CleanerFilter), "Cleaner")]
    [JsonDerivedType(typeof(FixLeadingZerosFilter), "FixLeadingZeros")]
    [JsonDerivedType(typeof(StripParenthesesFilter), "StripParentheses")]
    [JsonDerivedType(typeof(CapitalizeAfterFilter), "CapitalizeAfter")]
    [JsonDerivedType(typeof(UppercaseInitialsFilter), "UppercaseInitials")]
    [JsonDerivedType(typeof(StripSpacesLeftFilter), "StripSpacesLeft")]
    [JsonDerivedType(typeof(StripSpacesRightFilter), "StripSpacesRight")]
    [JsonDerivedType(typeof(TrimBetweenFilter), "TrimBetween")]
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

        internal abstract string TransformSegment(string segment, RenameItem item);
    }
}
