namespace Mfr.Utils
{
    /// <summary>
    /// Provides shared <see cref="StringComparer"/> instances for filesystem path comparisons.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use <see cref="Os"/> when comparing paths the way the host filesystem does:
    /// case-insensitive on Windows, case-sensitive elsewhere. This matches the behavior of
    /// <see cref="File"/> and <see cref="Directory"/> path lookups for typical
    /// configurations of NTFS, APFS (default), and ext4.
    /// </para>
    /// </remarks>
    public static class PathComparers
    {
        /// <summary>
        /// Gets the comparer that matches the host filesystem's case sensitivity.
        /// </summary>
        public static StringComparer Os { get; } = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        /// <summary>
        /// Gets the <see cref="StringComparison"/> mode that matches <see cref="Os"/>.
        /// </summary>
        public static StringComparison OsComparison { get; } = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
    }
}
