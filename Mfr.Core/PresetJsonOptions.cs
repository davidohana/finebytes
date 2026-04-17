using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Mfr.Filters.Case;
using Mfr.Filters.Formatting;
using Mfr.Filters.Misc;
using Mfr.Filters.Replace;
using Mfr.Filters.Space;
using Mfr.Filters.Trimming;
using Mfr.Models;

namespace Mfr.Core
{
    internal static class PresetJsonOptions
    {
        internal static JsonSerializerOptions Default { get; } = _CreateOptions();

        private static JsonSerializerOptions _CreateOptions()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
            options.Converters.Add(new JsonStringEnumConverter());

            var resolver = new DefaultJsonTypeInfoResolver();
            resolver.Modifiers.Add(_ConfigureBaseFilterPolymorphism);
            options.TypeInfoResolver = resolver;
            return options;
        }

        private static void _ConfigureBaseFilterPolymorphism(JsonTypeInfo typeInfo)
        {
            if (typeInfo.Type != typeof(BaseFilter))
            {
                return;
            }

            typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
            {
                TypeDiscriminatorPropertyName = "type",
                DerivedTypes =
                {
                    new JsonDerivedType(typeof(LettersCaseFilter), "LettersCase"),
                    new JsonDerivedType(typeof(SpaceCharacterFilter), "SpaceCharacter"),
                    new JsonDerivedType(typeof(RemoveSpacesFilter), "RemoveSpaces"),
                    new JsonDerivedType(typeof(ShrinkSpacesFilter), "ShrinkSpaces"),
                    new JsonDerivedType(typeof(TrimLeftFilter), "TrimLeft"),
                    new JsonDerivedType(typeof(TrimRightFilter), "TrimRight"),
                    new JsonDerivedType(typeof(ExtractLeftFilter), "ExtractLeft"),
                    new JsonDerivedType(typeof(ExtractRightFilter), "ExtractRight"),
                    new JsonDerivedType(typeof(ReplacerFilter), "Replacer"),
                    new JsonDerivedType(typeof(ShrinkDuplicateCharactersFilter), "ShrinkDuplicateCharacters"),
                    new JsonDerivedType(typeof(FormatterFilter), "Formatter"),
                    new JsonDerivedType(typeof(CounterFilter), "Counter"),
                    new JsonDerivedType(typeof(CleanerFilter), "Cleaner"),
                    new JsonDerivedType(typeof(ReplaceListFilter), "ReplaceList"),
                    new JsonDerivedType(typeof(FixLeadingZerosFilter), "FixLeadingZeros"),
                    new JsonDerivedType(typeof(StripParenthesesFilter), "StripParentheses"),
                    new JsonDerivedType(typeof(CapitalizeAfterFilter), "CapitalizeAfter"),
                    new JsonDerivedType(typeof(UppercaseInitialsFilter), "UppercaseInitials"),
                    new JsonDerivedType(typeof(StripSpacesLeftFilter), "StripSpacesLeft"),
                    new JsonDerivedType(typeof(StripSpacesRightFilter), "StripSpacesRight"),
                    new JsonDerivedType(typeof(TrimBetweenFilter), "TrimBetween")
                }
            };
        }
    }
}
