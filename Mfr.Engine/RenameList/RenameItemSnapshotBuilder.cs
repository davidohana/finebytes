using Mfr.Utils;

namespace Mfr.Engine.RenameList
{
    /// <summary>
    /// Factory for dissecting file paths into metadata components and constructing <see cref="RenameItem"/> instances.
    /// </summary>
    internal static class RenameItemSnapshotBuilder
    {
        /// <summary>
        /// Creates a new <see cref="RenameItem"/> instance from a given file path and attributes.
        /// </summary>
        /// <param name="fullPath">The fully qualified path of the file or directory.</param>
        /// <param name="attrs">The file attributes to determine if it is a directory or file.</param>
        /// <returns>A new <see cref="RenameItem"/>.</returns>
        public static RenameItem CreateRenameItem(string fullPath, FileAttributes attrs)
        {
            return new RenameItem(CreateOriginalSnapshot(fullPath, attrs));
        }

        /// <summary>
        /// Creates a <see cref="FileMeta"/> snapshot containing original filesystem properties.
        /// </summary>
        /// <param name="fullPath">The fully qualified path of the file or directory.</param>
        /// <param name="attrs">The file attributes.</param>
        /// <returns>A <see cref="FileMeta"/> object initialized with the path's properties.</returns>
        public static FileMeta CreateOriginalSnapshot(string fullPath, FileAttributes attrs)
        {
            var isDirectory = attrs.IsDirectory();
            var (directoryPath, prefix, extension) = isDirectory
                ? _SplitRenamePathForDirectory(fullPath)
                : _SplitRenamePathForFile(fullPath);

            return new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: directoryPath,
                prefix: prefix,
                extension: extension,
                attributes: attrs,
                creationTime: File.GetCreationTime(fullPath),
                lastWriteTime: File.GetLastWriteTime(fullPath),
                lastAccessTime: File.GetLastAccessTime(fullPath),
                fileSize: isDirectory ? 0 : new FileInfo(fullPath).Length
            );
        }

        private static (string DirectoryPath, string Prefix, string Extension) _SplitRenamePathForFile(string fullPath)
        {
            var directoryPath = Path.GetDirectoryName(fullPath) ?? "";
            var prefix = Path.GetFileNameWithoutExtension(fullPath);
            var extension = Path.GetExtension(fullPath);
            return (directoryPath, prefix, extension);
        }

        private static (string DirectoryPath, string Prefix, string Extension) _SplitRenamePathForDirectory(
            string fullPath
        )
        {
            var trimmed = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var directoryPath = Path.GetDirectoryName(trimmed) ?? "";
            var prefix = Path.GetFileName(trimmed);
            return (directoryPath, prefix, string.Empty);
        }
    }
}
