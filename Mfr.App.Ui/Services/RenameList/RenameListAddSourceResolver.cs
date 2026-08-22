using Mfr.App.Ui.ViewModels.FileList;
using Mfr.Models.Config;
using Mfr.Utils;

namespace Mfr.App.Ui.Services.RenameList
{
    /// <summary>
    /// Maps File List selection and current-folder state to engine add sources (no expansion).
    /// </summary>
    internal static class RenameListAddSourceResolver
    {
        /// <summary>
        /// Resolves engine add sources from File List selection.
        /// </summary>
        /// <param name="selectedEntries">Selected File List rows.</param>
        /// <param name="mask">File List include mask used as the last segment of folder sources.</param>
        /// <param name="addMode">Which path kinds may contribute sources (files and/or folders).</param>
        /// <returns>File paths and folder sources with a last-segment filename mask.</returns>
        public static IReadOnlyList<string> ResolveSourcesFromSelection(
            IReadOnlyList<FileListEntry> selectedEntries,
            string mask,
            RenameListAddMode addMode
        )
        {
            ArgumentNullException.ThrowIfNull(selectedEntries);
            return [.. _EnumerateSelectionSources(selectedEntries, mask, addMode)];
        }

        /// <summary>
        /// Returns whether selection would produce at least one engine add source.
        /// </summary>
        /// <param name="selectedEntries">Selected File List rows.</param>
        /// <param name="mask">File List include mask used as the last segment of folder sources.</param>
        /// <param name="addMode">Which path kinds may contribute sources (files and/or folders).</param>
        /// <returns><see langword="true"/> when at least one source would be emitted.</returns>
        public static bool CanResolveFromSelection(
            IReadOnlyList<FileListEntry> selectedEntries,
            string mask,
            RenameListAddMode addMode
        )
        {
            ArgumentNullException.ThrowIfNull(selectedEntries);
            return _EnumerateSelectionSources(selectedEntries, mask, addMode).Any();
        }

        /// <summary>
        /// Resolves engine add sources from the File List's current folder.
        /// </summary>
        /// <param name="currentPath">Current File List folder path.</param>
        /// <param name="mask">File List include mask used as the last segment of the folder source.</param>
        /// <returns>A single folder source with a last-segment filename mask, or empty.</returns>
        public static IReadOnlyList<string> ResolveSourcesFromCurrentFolder(string currentPath, string mask)
        {
            if (!_IsAddablePath(currentPath))
            {
                return [];
            }

            return [_BuildFolderSource(currentPath, mask)];
        }

        /// <summary>
        /// Returns whether the current folder would produce an engine add source.
        /// </summary>
        /// <param name="currentPath">Current File List folder path.</param>
        /// <returns><see langword="true"/> when the folder is addable.</returns>
        public static bool CanResolveFromCurrentFolder(string currentPath)
        {
            return _IsAddablePath(currentPath);
        }

        /// <summary>
        /// Yields engine add sources for each addable selected File List entry.
        /// </summary>
        /// <param name="selectedEntries">Selected File List rows.</param>
        /// <param name="mask">File List include mask used as the last segment of folder sources.</param>
        /// <param name="addMode">Which path kinds may contribute sources (files and/or folders).</param>
        /// <returns>File paths and folder sources with a last-segment filename mask.</returns>
        private static IEnumerable<string> _EnumerateSelectionSources(
            IReadOnlyList<FileListEntry> selectedEntries,
            string mask,
            RenameListAddMode addMode
        )
        {
            var includeFiles = addMode.IncludesFiles();

            foreach (var entry in selectedEntries)
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
