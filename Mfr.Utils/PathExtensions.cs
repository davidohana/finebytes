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
    }
}
