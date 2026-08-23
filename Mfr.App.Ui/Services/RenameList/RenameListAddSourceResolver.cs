using Mfr.App.Ui.ViewModels.FileList;
using Mfr.Models.Config;
using Mfr.Utils;

namespace Mfr.App.Ui.Services.RenameList
{
    /// <summary>
    /// Maps File List rows to engine add sources (no expansion).
    /// </summary>
    internal static class RenameListAddSourceResolver
    {
        /// <summary>
        /// Resolves engine add sources from File List rows (selection or all listed entries).
        /// </summary>
        /// <param name="entries">File List rows to turn into add sources.</param>
        /// <param name="mask">File List include mask used as the last segment of folder sources.</param>
        /// <param name="addMode">Which path kinds may contribute sources (files and/or folders).</param>
        /// <returns>File paths and folder sources with a last-segment filename mask.</returns>
        public static IReadOnlyList<string> ResolveSourcesFromSelection(
            IReadOnlyList<FileListEntry> entries,
            string mask,
            RenameListAddMode addMode
        )
        {
            ArgumentNullException.ThrowIfNull(entries);
            return [.. _EnumerateSelectionSources(entries, mask, addMode)];
        }

        /// <summary>
        /// Returns whether the given File List rows would produce at least one engine add source.
        /// </summary>
        /// <param name="entries">File List rows to inspect.</param>
        /// <param name="mask">File List include mask used as the last segment of folder sources.</param>
        /// <param name="addMode">Which path kinds may contribute sources (files and/or folders).</param>
        /// <returns><see langword="true"/> when at least one source would be emitted.</returns>
        public static bool CanResolveFromSelection(
            IReadOnlyList<FileListEntry> entries,
            string mask,
            RenameListAddMode addMode
        )
        {
            ArgumentNullException.ThrowIfNull(entries);
            return _EnumerateSelectionSources(entries, mask, addMode).Any();
        }

        /// <summary>
        /// Returns whether Add All may run from this File List location.
        /// </summary>
        /// <param name="currentPath">Current File List folder path.</param>
        /// <returns>
        /// <see langword="true"/> when the location is not This PC or Network.
        /// Drive roots are allowed: Add All targets listed child rows, not the root itself.
        /// </returns>
        public static bool IsAddableLocation(string currentPath)
        {
            return !FileListPath.IsComputerPath(currentPath) && !FileListPath.IsNetworkPath(currentPath);
        }

        /// <summary>
        /// Yields engine add sources for each addable File List entry.
        /// </summary>
        /// <param name="entries">File List rows to turn into add sources.</param>
        /// <param name="mask">File List include mask used as the last segment of folder sources.</param>
        /// <param name="addMode">Which path kinds may contribute sources (files and/or folders).</param>
        /// <returns>File paths and folder sources with a last-segment filename mask.</returns>
        private static IEnumerable<string> _EnumerateSelectionSources(
            IReadOnlyList<FileListEntry> entries,
            string mask,
            RenameListAddMode addMode
        )
        {
            var includeFiles = addMode.IncludesFiles();

            foreach (var entry in entries)
            {
                if (!_IsAddablePath(entry.FullPath))
                {
                    continue;
                }

                if (entry.IsDirectory)
                {
                    // Folder sources expand under includeFiles / includeFolders at the engine layer.
                    yield return _BuildFolderSource(entry.FullPath, mask);
                    continue;
                }

                if (!includeFiles)
                {
                    continue;
                }

                yield return entry.FullPath;
            }
        }

        /// <summary>
        /// Builds a directory source whose last segment is the File List include mask.
        /// </summary>
        private static string _BuildFolderSource(string folderPath, string mask)
        {
            var trimmedMask = string.IsNullOrWhiteSpace(mask) ? "*" : mask.Trim();
            return Path.Combine(folderPath, trimmedMask);
        }

        /// <summary>
        /// Returns whether <paramref name="path"/> can be used as an engine add source.
        /// </summary>
        /// <param name="path">Candidate file or folder path from the File List.</param>
        /// <returns>
        /// <see langword="true"/> when the path is resolvable and not This PC, Network, or a filesystem root;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool _IsAddablePath(string path)
        {
            if (FileListPath.IsComputerPath(path) || FileListPath.IsNetworkPath(path))
            {
                return false;
            }

            try
            {
                var fullPath = Path.GetFullPath(path);
                var root = Path.GetPathRoot(fullPath);
                return !string.IsNullOrEmpty(root) && !string.Equals(root, fullPath, PathComparers.OsComparison);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or IOException)
            {
                return false;
            }
        }
    }
}
