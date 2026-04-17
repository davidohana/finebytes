using System.Text.Json.Serialization;
using Mfr.Filters.Case;
using Mfr.Filters.Formatting;
using Mfr.Filters.Misc;
using Mfr.Filters.Replace;
using Mfr.Filters.Space;
using Mfr.Filters.Trimming;

namespace Mfr.Models
{
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
    [JsonDerivedType(typeof(ReplaceListFilter), "ReplaceList")]
    [JsonDerivedType(typeof(FixLeadingZerosFilter), "FixLeadingZeros")]
    [JsonDerivedType(typeof(StripParenthesesFilter), "StripParentheses")]
    [JsonDerivedType(typeof(CapitalizeAfterFilter), "CapitalizeAfter")]
    [JsonDerivedType(typeof(UppercaseInitialsFilter), "UppercaseInitials")]
    [JsonDerivedType(typeof(StripSpacesLeftFilter), "StripSpacesLeft")]
    [JsonDerivedType(typeof(StripSpacesRightFilter), "StripSpacesRight")]
    [JsonDerivedType(typeof(TrimBetweenFilter), "TrimBetween")]
    public abstract partial record BaseFilter;
}
