using System.Reflection;
using Mfr.Filters.Formatting.Tokens;

namespace Mfr.Filters.Formatting.FormatString
{
    /// <summary>
    /// Discovers concrete <see cref="IFormatToken"/> types once for the compiler and catalog.
    /// </summary>
    internal static class FormatTokenRegistry
    {
        private static readonly Lazy<RegistryData> _data = new(_Build);

        /// <summary>
        /// Gets the name → token map used by <see cref="FormatStringCompiler"/>.
        /// </summary>
        internal static IReadOnlyDictionary<string, IFormatToken> NameToToken => _data.Value.NameToToken;

        /// <summary>
        /// Gets catalog rows built from <see cref="FormatTokenInfoAttribute"/> on each concrete token type.
        /// </summary>
        internal static IReadOnlyList<FormatTokenCatalogEntry> CatalogEntries => _data.Value.CatalogEntries;

        /// <summary>
        /// Discovers tokens, builds the name map, and materializes sorted catalog rows.
        /// </summary>
        private static RegistryData _Build()
        {
            var nameToToken = new Dictionary<string, IFormatToken>(StringComparer.Ordinal);
            var catalog = new List<FormatTokenCatalogEntry>();

            var tokenTypes = typeof(FormatTokenRegistry)
                .Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IFormatToken).IsAssignableFrom(t));

            foreach (var tokenType in tokenTypes)
            {
                var token = (IFormatToken)Activator.CreateInstance(tokenType)!;
                foreach (var name in token.Names)
                {
                    if (nameToToken.TryGetValue(name, out var registeredToken))
                    {
                        throw new InvalidOperationException(
                            $"Format token name '{name}' is registered by multiple types "
                                + $"('{registeredToken.GetType().FullName}' and '{tokenType.FullName}')."
                        );
                    }

                    nameToToken[name] = token;
                }

                var info =
                    tokenType.GetCustomAttribute<FormatTokenInfoAttribute>()
                    ?? throw new InvalidOperationException(
                        $"Format token type '{tokenType.FullName}' is missing [FormatTokenInfo]."
                    );
                var canonicalName = token.Names[0];
                catalog.Add(
                    new FormatTokenCatalogEntry(
                        DisplayName: info.DisplayName,
                        GroupPath: info.Group,
                        ShortDescription: info.ShortDescription,
                        InsertText: "<" + info.Initial + ">",
                        CanonicalName: canonicalName
                    )
                );
            }

            catalog.Sort(
                static (a, b) =>
                {
                    var groupCmp = string.Compare(a.GroupPath, b.GroupPath, StringComparison.OrdinalIgnoreCase);
                    if (groupCmp != 0)
                    {
                        return groupCmp;
                    }

                    return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
                }
            );

            return new RegistryData(nameToToken, [.. catalog]);
        }

        private sealed record RegistryData(
            IReadOnlyDictionary<string, IFormatToken> NameToToken,
            IReadOnlyList<FormatTokenCatalogEntry> CatalogEntries
        );
    }
}
