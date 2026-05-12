namespace Mfr.Models
{
    /// <summary>
    /// Loads embedded-tag values for one absolute filesystem path without mutating callers' snapshots.
    /// </summary>
    /// <param name="absolutePath">Fully qualified file path.</param>
    /// <returns>A detached <see cref="AudioTagOverlay"/> read from the path.</returns>
    /// <exception cref="IOException">The file cannot be read.</exception>
    /// <exception cref="ArgumentException">The path is not a readable regular file.</exception>
    public delegate AudioTagOverlay AudioTagReader(string absolutePath);
}
