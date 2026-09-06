using Mfr.Filters.Formatting.Tokens;

namespace Mfr.Filters.Formatting.FormatString
{
    /// <summary>
    /// One FormatEditor catalog row for a canonical formatter token.
    /// </summary>
    /// <param name="DisplayName">Human-readable label.</param>
    /// <param name="GroupPath">Menu group path (<c>\</c>-separated).</param>
    /// <param name="ShortDescription">Tooltip / hint text.</param>
    /// <param name="InsertText">Default insert text including angle brackets.</param>
    /// <param name="CanonicalName">Primary token name (<see cref="IFormatToken.Names"/>[0]).</param>
    public sealed record FormatTokenCatalogEntry(
        string DisplayName,
        string GroupPath,
        string ShortDescription,
        string InsertText,
        string CanonicalName
    );

    /// <summary>
    /// Public catalog of formatter tokens for FormatEditor insert UI.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lists <strong>canonical</strong> names only (aliases still compile). Entries are sorted by
    /// <see cref="FormatTokenCatalogEntry.GroupPath"/> then <see cref="FormatTokenCatalogEntry.DisplayName"/>.
    /// </para>
    /// </remarks>
    public static class FormatTokenCatalog
    {
        /// <summary>
        /// Gets every discovered token that carries <see cref="FormatTokenInfoAttribute"/>.
        /// </summary>
        public static IReadOnlyList<FormatTokenCatalogEntry> Entries => FormatTokenRegistry.CatalogEntries;
    }
}
