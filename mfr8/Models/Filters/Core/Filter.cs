using System.Text.Json.Serialization;

namespace Mfr8.Models
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

        internal abstract string Apply(string segment, FileEntryLite file);
    }
}
