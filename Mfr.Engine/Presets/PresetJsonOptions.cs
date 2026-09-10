using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Mfr.Filters;

namespace Mfr.Engine.Presets
{
    internal static class PresetJsonOptions
    {
        /// <summary>
        /// Concrete <see cref="BaseFilter"/> types registered for preset JSON polymorphism.
        /// <para>
        /// Built from <see cref="FilterCatalog.Entries"/> (CLR type + Type discriminator).
        /// </para>
        /// </summary>
        internal static IReadOnlyList<JsonDerivedType> BaseFilterDerivedTypes { get; } = _BuildBaseFilterDerivedTypes();

        internal static JsonSerializerOptions Default { get; } = _CreateOptions();

        private static IReadOnlyList<JsonDerivedType> _BuildBaseFilterDerivedTypes()
        {
            return [.. FilterCatalog.Entries.Select(entry => new JsonDerivedType(entry.FilterType, entry.Type))];
        }

        private static JsonSerializerOptions _CreateOptions()
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };
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
            foreach (var derived in BaseFilterDerivedTypes)
            {
                poly.DerivedTypes.Add(derived);
            }

            typeInfo.PolymorphismOptions = poly;
        }
    }
}
