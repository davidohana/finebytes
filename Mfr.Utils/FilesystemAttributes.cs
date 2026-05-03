namespace Mfr.Utils
{
    /// <summary>
    /// Helpers for interpreting filesystem attribute flags for paths on disk.
    /// </summary>
    public static class FilesystemAttributes
    {
        /// <summary>
        /// Whether the observed attribute flags denote a directory entry.
        /// </summary>
        /// <param name="attributes">Attribute flags for one resolved path.</param>
        /// <returns><c>true</c> when <paramref name="attributes"/> includes <see cref="FileAttributes.Directory"/>.</returns>
        public static bool IsDirectory(FileAttributes attributes)
        {
            return attributes.HasFlag(FileAttributes.Directory);
        }

        /// <summary>
        /// Whether <paramref name="path"/> denotes a directory according to current on-disk metadata.
        /// </summary>
        /// <para>
        /// Equivalent to inspecting <see cref="FileAttributes.Directory"/> via <see cref="File.GetAttributes(string)"/>.
        /// </para>
        /// <param name="path">Path whose metadata should be read.</param>
        /// <returns><c>true</c> when attributes denote a directory.</returns>
        /// <exception cref="FileNotFoundException">Thrown when <paramref name="path"/> cannot be found.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when <paramref name="path"/> refers to a missing parent segment.</exception>
        /// <exception cref="IOException">Thrown when attributes cannot be read.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when access is denied.</exception>
        public static bool IsDirectory(string path)
        {
            return IsDirectory(File.GetAttributes(path));
        }
    }
}
