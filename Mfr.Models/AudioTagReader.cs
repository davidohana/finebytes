namespace Mfr.Models
{
    /// <summary>
    /// Loads embedded-tag values for one absolute filesystem path without mutating callers' snapshots.
    /// </summary>
    /// <param name="absolutePath">Fully qualified file path.</param>
    /// <returns>
    /// A detached <see cref="AudioTagOverlay"/> when read succeeds; <see langword="null"/> when the path cannot supply tags (not a candidate, unreadable format, permission errors, etc.).
    /// </returns>
    public delegate AudioTagOverlay? AudioTagReader(string absolutePath);

    /// <summary>
    /// Shared <see cref="AudioTagReader"/> instances supplied by hosts and tests.
    /// </summary>
    internal static class AudioTagReaders
    {
        /// <summary>
        /// Reader that always returns <see langword="null"/> for any path (no disk I/O).
        /// </summary>
        internal static AudioTagReader NullReader { get; } = static _ => null;
    }
}
