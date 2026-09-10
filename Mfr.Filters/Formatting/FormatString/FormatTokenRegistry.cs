using System.Reflection;
using Mfr.Filters.Formatting.Tokens;
using Mfr.Filters.Formatting.Tokens.Audio;
using Mfr.Models.Tags;

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
                        DisplayName: _ResolveCatalogDisplayName(token, info, tokenType),
                        GroupPath: info.Group,
                        ShortDescription: _ResolveCatalogShortDescription(token, info),
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

        /// <summary>
        /// Resolves the picker label: semantic audio tokens use <see cref="SemanticAudioFieldLabels"/>;
        /// others require a non-empty <see cref="FormatTokenInfoAttribute.DisplayName"/>.
        /// </summary>
        private static string _ResolveCatalogDisplayName(
            IFormatToken token,
            FormatTokenInfoAttribute info,
            Type tokenType
        )
        {
            if (token is SemanticAudioFieldTokenBase audio)
            {
                return SemanticAudioFieldLabels.For(audio.Field);
            }

            if (string.IsNullOrEmpty(info.DisplayName))
            {
                throw new InvalidOperationException(
                    $"Format token type '{tokenType.FullName}' has no DisplayName and no shared label owner."
                );
            }

            return info.DisplayName;
        }

        /// <summary>
        /// Resolves the picker tip: semantic audio tokens use <see cref="SemanticAudioFieldTips"/>;
        /// others keep <see cref="FormatTokenInfoAttribute.ShortDescription"/>.
        /// </summary>
        private static string _ResolveCatalogShortDescription(IFormatToken token, FormatTokenInfoAttribute info)
        {
            if (token is SemanticAudioFieldTokenBase audio)
            {
                return SemanticAudioFieldTips.For(audio.Field) ?? info.ShortDescription;
            }

            return info.ShortDescription;
        }

        private sealed record RegistryData(
            IReadOnlyDictionary<string, IFormatToken> NameToToken,
            IReadOnlyList<FormatTokenCatalogEntry> CatalogEntries
        );
    }
}
