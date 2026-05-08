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
        public static bool IsDirectory(this FileAttributes attributes)
        {
            return attributes.HasFlag(FileAttributes.Directory);
        }

    }
}
