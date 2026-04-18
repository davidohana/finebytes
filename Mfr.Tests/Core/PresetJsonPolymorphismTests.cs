using Mfr.Core;
using Mfr.Filters.Case;
using Mfr.Models;

namespace Mfr.Tests.Core
{
    /// <summary>
    /// Guards <see cref="PresetJsonOptions"/> against drift when new <see cref="BaseFilter"/> types ship.
    /// </summary>
    public sealed class PresetJsonPolymorphismTests
    {
        [Fact]
        public void Every_shipped_BaseFilter_is_registered_for_preset_JSON()
        {
            var registered = PresetJsonOptions.BaseFilterDerivedTypes
                .Select(d => d.DerivedType)
                .ToHashSet();

            var shipped = typeof(LettersCaseFilter).Assembly.GetExportedTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(BaseFilter).IsAssignableFrom(t))
                .ToHashSet();

            Assert.Equal(shipped, registered);
        }

        [Fact]
        public void Preset_JSON_discriminators_are_unique()
        {
            var names = PresetJsonOptions.BaseFilterDerivedTypes
                .Select(d => d.TypeDiscriminator!.ToString()!)
                .ToList();
            Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count());
        }
    }
}
