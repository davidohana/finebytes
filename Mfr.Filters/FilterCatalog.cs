using System.Reflection;
using System.Runtime.CompilerServices;
using Mfr.Utils;

namespace Mfr.Filters
{
    /// <summary>
    /// One row in the Available Filters palette catalog.
    /// </summary>
    /// <param name="Type">JSON <c>type</c> discriminator (must match preset registration).</param>
    /// <param name="Group">Toolbar group that owns this filter.</param>
    /// <param name="DisplayName">Human-readable label shown in the list.</param>
    /// <param name="FilterType">Concrete filter CLR type (parameterless ctor supplies add-to-list defaults).</param>
    public sealed record FilterCatalogEntry(string Type, FilterGroup Group, string DisplayName, Type FilterType);

    /// <summary>
    /// Product catalog of preset filters for the Available Filters palette.
    /// <para>
    /// Built by reflecting concrete <see cref="BaseFilter"/> types that carry
    /// <see cref="FilterPaletteAttribute"/>. <see cref="FilterCatalogEntry.Type"/> comes from each
    /// type's <see cref="BaseFilter.Type"/> override.
    /// </para>
    /// </summary>
    public static class FilterCatalog
    {
        /// <summary>
        /// All discovered filters in stable group order (then by type name).
        /// </summary>
        public static IReadOnlyList<FilterCatalogEntry> Entries { get; } = _DiscoverEntries();

        private static IReadOnlyList<FilterCatalogEntry> _DiscoverEntries()
        {
            return
            [
                .. typeof(FilterCatalog)
                    .Assembly.GetExportedTypes()
                    .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(BaseFilter).IsAssignableFrom(t))
                    .Select(_ToEntry)
                    .OrderBy(e => (int)e.Group)
                    .ThenBy(e => e.Type, StringComparer.Ordinal),
            ];
        }

        private static FilterCatalogEntry _ToEntry(Type filterType)
        {
            var catalog = Check.NotNull(
                filterType.GetCustomAttribute<FilterPaletteAttribute>(),
                $"Filter '{filterType.FullName}' must declare [{nameof(FilterPaletteAttribute)}]."
            );
            Check.That(
                !string.IsNullOrWhiteSpace(catalog.DisplayName),
                $"Filter '{filterType.FullName}' {nameof(FilterPaletteAttribute)} display name must not be empty."
            );

            var typeName = _ReadTypeDiscriminator(filterType);
            return new FilterCatalogEntry(typeName, catalog.Group, catalog.DisplayName, filterType);
        }

        private static string _ReadTypeDiscriminator(Type filterType)
        {
            var instance = (BaseFilter)RuntimeHelpers.GetUninitializedObject(filterType);
            var typeName = instance.Type;
            Check.That(
                !string.IsNullOrWhiteSpace(typeName),
                $"Filter '{filterType.FullName}' returned an empty {nameof(BaseFilter.Type)}."
            );
            return typeName;
        }
    }
}
