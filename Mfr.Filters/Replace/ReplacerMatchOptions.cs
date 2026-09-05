namespace Mfr.Filters.Replace
{
    /// <summary>
    /// Shared match policy for <see cref="ReplacerFilter"/> and <see cref="ReplaceListFilter"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default <see cref="WholeWord"/> differs by filter: <see cref="ForReplacer"/> is
    /// <c>false</c>; <see cref="ForReplaceList"/> is <c>true</c> (MFR7 add-to-list defaults).
    /// </para>
    /// </remarks>
    /// <param name="Mode">Pattern interpretation mode.</param>
    /// <param name="CaseSensitive">Whether matching is case-sensitive.</param>
    /// <param name="ReplaceAll">Whether all matches are replaced.</param>
    /// <param name="WholeWord">Whether matching is constrained to whole words.</param>
    public sealed record ReplacerMatchOptions(ReplacerMode Mode, bool CaseSensitive, bool ReplaceAll, bool WholeWord)
    {
        /// <summary>
        /// Add-to-list defaults for <see cref="ReplacerFilter"/> (whole word off).
        /// </summary>
        public static ReplacerMatchOptions ForReplacer { get; } =
            new(Mode: ReplacerMode.Literal, CaseSensitive: false, ReplaceAll: true, WholeWord: false);

        /// <summary>
        /// Add-to-list defaults for <see cref="ReplaceListFilter"/> (whole word on).
        /// </summary>
        public static ReplacerMatchOptions ForReplaceList { get; } =
            new(Mode: ReplacerMode.Literal, CaseSensitive: false, ReplaceAll: true, WholeWord: true);
    }
}
