namespace Mfr.Utils
{
    /// <summary>
    /// Deletes older files in a directory so at most N remain (newest by creation time, then name).
    /// </summary>
    public static class NewestFilesPruner
    {
        /// <summary>
        /// Keeps the newest <paramref name="keepCount"/> files matching <paramref name="searchPattern"/>;
        /// deletes the rest. Does nothing when the directory is missing.
        /// <para>
        /// When <paramref name="keepCount"/> is <c>0</c>, deletes all matching files.
        /// Callers decide no-op gates (e.g. unlimited retention) before invoking.
        /// </para>
        /// </summary>
        /// <param name="directoryPath">Directory to prune.</param>
        /// <param name="keepCount">Maximum files to keep (newest by <see cref="FileSystemInfo.CreationTimeUtc"/>, then name).</param>
        /// <param name="searchPattern">
        /// <see cref="Directory.EnumerateFiles(string, string, SearchOption)"/> pattern (e.g. <c>*.mfrlog</c>).
        /// </param>
        /// <param name="onDeleted">Optional callback after a successful delete (full path).</param>
        /// <param name="onFailed">Optional callback when delete throws (full path + exception).</param>
        public static void PruneByCreationTimeUtc(
            string directoryPath,
            int keepCount,
            string searchPattern,
            Action<string>? onDeleted = null,
            Action<string, Exception>? onFailed = null
        )
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
            ArgumentNullException.ThrowIfNull(searchPattern);

            if (keepCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(keepCount));
            }

            if (!Directory.Exists(directoryPath))
            {
                return;
            }

            var fileInfos = Directory
                .EnumerateFiles(directoryPath, searchPattern, SearchOption.TopDirectoryOnly)
                .Select(path => new FileInfo(path))
                .OrderByDescending(fileInfo => fileInfo.CreationTimeUtc)
                .ThenByDescending(fileInfo => fileInfo.Name, StringComparer.Ordinal)
                .ToList();

            if (fileInfos.Count <= keepCount)
            {
                return;
            }

            foreach (var fileInfo in fileInfos.Skip(keepCount))
            {
                try
                {
                    fileInfo.Delete();
                    onDeleted?.Invoke(fileInfo.FullName);
                }
                catch (Exception ex)
                {
                    onFailed?.Invoke(fileInfo.FullName, ex);
                }
            }
        }
    }
}
