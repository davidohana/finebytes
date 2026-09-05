using System.Reflection;
using Mfr.Filters;

namespace Mfr.Tests.Models.Filters
{
    /// <summary>
    /// Guards <see cref="FilterCatalog"/> against drift from preset JSON registration.
    /// </summary>
    public sealed class FilterCatalogTests
    {
        /// <summary>
        /// Verifies every preset JSON discriminator has a catalog row and vice versa.
        /// </summary>
        [Fact]
        public void Catalog_Types_Match_Preset_Json_Discriminators()
        {
            var registered = PresetJsonOptions
                .BaseFilterDerivedTypes.Select(d => d.TypeDiscriminator!.ToString()!)
                .OrderBy(t => t, StringComparer.Ordinal)
                .ToList();

            var catalog = FilterCatalog.Entries.Select(e => e.Type).OrderBy(t => t, StringComparer.Ordinal).ToList();

            Assert.Equal(registered, catalog);
        }

        /// <summary>
        /// Verifies catalog type strings are unique.
        /// </summary>
        [Fact]
        public void Catalog_Types_Are_Unique()
        {
            var types = FilterCatalog.Entries.Select(e => e.Type).ToList();
            Assert.Equal(types.Count, types.Distinct(StringComparer.Ordinal).Count());
        }

        /// <summary>
        /// Verifies every concrete shipped filter declares catalog metadata.
        /// </summary>
        [Fact]
        public void Every_Concrete_Filter_Has_Catalog_Attribute()
        {
            var missing = typeof(FilterCatalog)
                .Assembly.GetExportedTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(BaseFilter).IsAssignableFrom(t))
                .Where(t => t.GetCustomAttribute<FilterPaletteAttribute>() is null)
                .Select(t => t.Name)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToList();

            Assert.Empty(missing);
        }

        /// <summary>
        /// Verifies every catalog type constructs via parameterless ctor and returns matching <see cref="BaseFilter.Type"/>.
        /// </summary>
        [Fact]
        public void Every_Catalog_Type_Constructs_With_Default_Ctor()
        {
            foreach (var entry in FilterCatalog.Entries)
            {
                var instance = FilterCatalog.CreateDefault(entry);
                Assert.Equal(entry.Type, instance.Type);
            }
        }

        /// <summary>
        /// Verifies MFR 7 display-name exceptions and group membership for known filters.
        /// </summary>
        [Fact]
        public void Known_Display_Names_And_Groups()
        {
            var byType = FilterCatalog.Entries.ToDictionary(e => e.Type, StringComparer.Ordinal);

            Assert.Equal("Audio Tag Remover", byType["TagRemover"].DisplayName);
            Assert.Equal(FilterGroup.Audio, byType["TagRemover"].Group);

            Assert.Equal("Path Mover", byType["PathMover"].DisplayName);
            Assert.Equal(FilterGroup.Misc, byType["PathMover"].Group);

            Assert.Equal("Fix Leading 0's", byType["FixLeadingZeros"].DisplayName);
            Assert.Equal(FilterGroup.Misc, byType["FixLeadingZeros"].Group);

            Assert.Equal("ID3v2 Field Setter", byType["Id3v2FieldSetter"].DisplayName);
            Assert.Equal(FilterGroup.Audio, byType["Id3v2FieldSetter"].Group);

            Assert.Equal("Audio Tag Setter", byType["AudioTagSetter"].DisplayName);
            Assert.Equal("Letters Case", byType["LettersCase"].DisplayName);
            Assert.Equal(FilterGroup.Case, byType["LettersCase"].Group);
            Assert.Equal(FilterGroup.Space, byType["SpaceCharacter"].Group);
            Assert.Equal(FilterGroup.Trimming, byType["ExtractLeft"].Group);
            Assert.Equal(FilterGroup.Replace, byType["Cleaner"].Group);
            Assert.Equal(FilterGroup.Formatting, byType["Counter"].Group);
            Assert.Equal(FilterGroup.Attributes, byType["DateTimeSetter"].Group);
        }

        /// <summary>
        /// Verifies the catalog covers all eight palette groups.
        /// </summary>
        [Fact]
        public void Catalog_Uses_Every_FilterGroup()
        {
            var used = FilterCatalog.Entries.Select(e => e.Group).ToHashSet();
            foreach (var group in Enum.GetValues<FilterGroup>())
            {
                Assert.Contains(group, used);
            }
        }
    }
}
