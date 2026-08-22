using Mfr.App.Ui.ViewModels.FileList;
using Mfr.Utils;

namespace Mfr.App.Ui.Services.RenameList
{
    /// <summary>
    /// Maps File List selection and current-folder state to engine add sources (no expansion).
    /// </summary>
    internal static class RenameListAddSources
    {
        /// <summary>
        /// Resolves engine add sources from File List selection.
        /// </summary>
        /// <param name="selectedEntries">Selected File List rows.</param>
        /// <param name="mask">File List include mask used as the last segment of folder sources.</param>
        /// <param name="addFiles">When true, selected files are included.</param>
        /// <param name="addFolders">When true with or without <paramref name="addFiles"/>, selected folders become folder sources.</param>
        /// <returns>File paths and folder sources with a last-segment filename mask.</returns>
        public static IReadOnlyList<string> ResolveSourcesFromSelection(
            IReadOnlyList<FileListEntry> selectedEntries,
            string mask,
            bool addFiles,
            bool addFolders
        )
        {
            ArgumentNullException.ThrowIfNull(selectedEntries);
            return [.. _EnumerateSelectionSources(selectedEntries, mask, addFiles, addFolders)];
        }

        /// <summary>
        /// Returns whether selection would produce at least one engine add source.
        /// </summary>
        /// <param name="selectedEntries">Selected File List rows.</param>
        /// <param name="mask">File List include mask used as the last segment of folder sources.</param>
        /// <param name="addFiles">When true, selected files are included.</param>
        /// <param name="addFolders">When true with or without <paramref name="addFiles"/>, selected folders become folder sources.</param>
        /// <returns><see langword="true"/> when at least one source would be emitted.</returns>
        public static bool CanResolveFromSelection(
            IReadOnlyList<FileListEntry> selectedEntries,
            string mask,
            bool addFiles,
            bool addFolders
        )
        {
            ArgumentNullException.ThrowIfNull(selectedEntries);
            return _EnumerateSelectionSources(selectedEntries, mask, addFiles, addFolders).Any();
        }

        /// <summary>
        /// Resolves engine add sources from the File List's current folder.
        /// </summary>
        /// <param name="currentPath">Current File List folder path.</param>
        /// <param name="mask">File List include mask used as the last segment of the folder source.</param>
        /// <param name="canAddAllToCurrentFolder">Whether Add All is allowed for the current folder.</param>
        /// <param name="addFiles">With <paramref name="addFolders"/>, gates whether the current folder is emitted.</param>
        /// <param name="addFolders">With <paramref name="addFiles"/>, gates whether the current folder is emitted.</param>
        /// <returns>A single folder source with a last-segment filename mask, or empty.</returns>
        public static IReadOnlyList<string> ResolveSourcesFromCurrentFolder(
            string currentPath,
            string mask,
            bool canAddAllToCurrentFolder,
            bool addFiles,
            bool addFolders
        )
        {
            if (!CanResolveFromCurrentFolder(canAddAllToCurrentFolder, addFiles, addFolders))
            {
                return [];
            }

            return [_BuildFolderSource(currentPath, mask)];
        }

        /// <summary>
        /// Returns whether Add All would produce a current-folder engine source.
        /// </summary>
        /// <param name="canAddAllToCurrentFolder">Whether Add All is allowed for the current folder.</param>
        /// <param name="addFiles">With <paramref name="addFolders"/>, gates whether the current folder is emitted.</param>
        /// <param name="addFolders">With <paramref name="addFiles"/>, gates whether the current folder is emitted.</param>
        /// <returns><see langword="true"/> when a current-folder source would be emitted.</returns>
        public static bool CanResolveFromCurrentFolder(bool canAddAllToCurrentFolder, bool addFiles, bool addFolders)
        {
            return canAddAllToCurrentFolder && (addFiles || addFolders);
        }

        /// <summary>
        /// Yields engine add sources for each addable selected File List entry.
        /// </summary>
        /// <param name="selectedEntries">Selected File List rows.</param>
        /// <param name="mask">File List include mask used as the last segment of folder sources.</param>
        /// <param name="addFiles">When true, selected files are included.</param>
        /// <param name="addFolders">When true with or without <paramref name="addFiles"/>, selected folders become folder sources.</param>
        /// <returns>File paths and folder sources with a last-segment filename mask.</returns>
        private static IEnumerable<string> _EnumerateSelectionSources(
            IReadOnlyList<FileListEntry> selectedEntries,
            string mask,
            bool addFiles,
            bool addFolders
        )
        {
            if (!addFiles && !addFolders)
            {
                yield break;
            }

            foreach (var entry in selectedEntries)
            {
                if (!_IsAddablePath(entry.FullPath))
                {
                    continue;
                }

                if (entry.IsDirectory)
                {
                    yield return _BuildFolderSource(entry.FullPath, mask);
                    continue;
                }

                if (!addFiles)
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
        /// <see langword="true"/> when the path is non-blank, resolvable, and not a filesystem root;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool _IsAddablePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            try
            {
                var fullPath = Path.GetFullPath(path);
                var root = Path.GetPathRoot(fullPath);
                return !string.IsNullOrEmpty(root) && !string.Equals(root, fullPath, PathComparers.OsComparison);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
            {
                return false;
            }
        }
    }
}
