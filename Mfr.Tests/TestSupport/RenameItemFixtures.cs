namespace Mfr.Tests.TestSupport
{
    /// <summary>
    /// Builds unmarked <see cref="RenameItem"/> rows from committed test fixtures.
    /// </summary>
    internal static class RenameItemFixtures
    {
        /// <summary>
        /// Creates a <see cref="RenameItem"/> for a fixture file with no metadata load flags set.
        /// </summary>
        /// <param name="fileName">File name under <c>Fixtures/</c>.</param>
        /// <returns>
        /// An unmarked item (<see cref="RenameItem.ImagePropertiesLoadAttempted"/> is false) backed by
        /// <see cref="FileMeta"/> for that path.
        /// </returns>
        public static RenameItem Unmarked(string fileName)
        {
            return UnmarkedFromPath(FixturePaths.Require(fileName));
        }

        /// <summary>
        /// Creates a <see cref="RenameItem"/> for an absolute path with no metadata load flags set.
        /// </summary>
        /// <param name="fullPath">Fully qualified filesystem path.</param>
        /// <returns>An unmarked <see cref="RenameItem"/> for that path.</returns>
        public static RenameItem UnmarkedFromPath(string fullPath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fullPath);

            var absolute = Path.GetFullPath(fullPath);
            var directory = Path.GetDirectoryName(absolute)!;
            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: directory,
                fileName: Path.GetFileNameWithoutExtension(absolute),
                extension: FileMeta.ExtensionWithoutDot(absolute),
                fileSize: File.Exists(absolute) ? new FileInfo(absolute).Length : 0
            );

            return new RenameItem(meta);
        }
    }
}
