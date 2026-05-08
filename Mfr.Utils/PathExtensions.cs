namespace Mfr.Utils
{
    /// <summary>
    /// Provides convenience extensions for composing file-system paths.
    /// </summary>
    public static class PathExtensions
    {
        /// <summary>
        /// Combines a root path with one or more child segments.
        /// </summary>
        /// <param name="root">The base path.</param>
        /// <param name="segments">Child path segments to append.</param>
        /// <returns>The combined path.</returns>
        public static string CombinePath(this string root, params string[] segments)
        {
            return Path.Combine([root, .. segments]);
        }

        /// <summary>
        /// Whether this path denotes a directory according to current on-disk metadata.
        /// </summary>
        /// <para>
        /// Equivalent to reading <see cref="File.GetAttributes(string)"/> and checking
        /// <see cref="FileAttributes.Directory"/>.
        /// </para>
        /// <param name="path">Path whose metadata should be read.</param>
        /// <returns><c>true</c> when attributes denote a directory.</returns>
        /// <exception cref="FileNotFoundException">Thrown when <paramref name="path"/> cannot be found.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when <paramref name="path"/> refers to a missing parent segment.</exception>
        /// <exception cref="IOException">Thrown when attributes cannot be read.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when access is denied.</exception>
        public static bool IsDirectory(this string path)
        {
            return File.GetAttributes(path).IsDirectory();
        }

        /// <summary>
        /// Returns the path with any trailing directory separator removed.
        /// </summary>
        /// <param name="path">Path to normalize.</param>
        /// <returns>The same path without a trailing separator.</returns>
        public static string TrimTrailingSeparator(this string path)
        {
            return Path.TrimEndingDirectorySeparator(path);
        }
    }
}
