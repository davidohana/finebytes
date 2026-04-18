using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Mfr.Filters.Attributes;
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
        /// <summary>
        /// Concrete <see cref="BaseFilter"/> types registered for preset JSON (single source of truth for polymorphism).
        /// </summary>
        internal static IReadOnlyList<JsonDerivedType> BaseFilterDerivedTypes => s_BaseFilterDerivedTypes;

        private static readonly JsonDerivedType[] s_BaseFilterDerivedTypes =
        [
            new(typeof(LettersCaseFilter), "LettersCase"),
            new(typeof(SpaceCharacterFilter), "SpaceCharacter"),
            new(typeof(RemoveSpacesFilter), "RemoveSpaces"),
            new(typeof(SeparateCapitalizedTextFilter), "SeparateCapitalizedText"),
            new(typeof(ShrinkSpacesFilter), "ShrinkSpaces"),
            new(typeof(TrimLeftFilter), "TrimLeft"),
            new(typeof(TrimRightFilter), "TrimRight"),
            new(typeof(ExtractLeftFilter), "ExtractLeft"),
            new(typeof(ExtractRightFilter), "ExtractRight"),
            new(typeof(ReplacerFilter), "Replacer"),
            new(typeof(ShrinkDuplicateCharactersFilter), "ShrinkDuplicateCharacters"),
            new(typeof(FormatterFilter), "Formatter"),
            new(typeof(CounterFilter), "Counter"),
            new(typeof(InserterFilter), "Inserter"),
            new(typeof(TokenMoverFilter), "TokenMover"),
            new(typeof(NameListFilter), "NameList"),
            new(typeof(CleanerFilter), "Cleaner"),
            new(typeof(ReplaceListFilter), "ReplaceList"),
            new(typeof(FixLeadingZerosFilter), "FixLeadingZeros"),
            new(typeof(StripParenthesesFilter), "StripParentheses"),
            new(typeof(CapitalizeAfterFilter), "CapitalizeAfter"),
            new(typeof(UppercaseInitialsFilter), "UppercaseInitials"),
            new(typeof(CasingListFilter), "CasingList"),
            new(typeof(SentenceEndCharactersFilter), "SentenceEndCharacters"),
            new(typeof(StripSpacesLeftFilter), "StripSpacesLeft"),
            new(typeof(StripSpacesRightFilter), "StripSpacesRight"),
            new(typeof(TrimBetweenFilter), "TrimBetween"),
            new(typeof(AttributesSetterFilter), "AttributesSetter")
        ];

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

            var poly = new JsonPolymorphismOptions { TypeDiscriminatorPropertyName = "type" };
            foreach (var derived in s_BaseFilterDerivedTypes)
            {
                poly.DerivedTypes.Add(derived);
            }

            typeInfo.PolymorphismOptions = poly;
        }
    }
}
