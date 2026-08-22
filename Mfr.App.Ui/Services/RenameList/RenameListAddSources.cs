using Mfr.App.Ui.ViewModels.FileList;
using Mfr.Utils;

namespace Mfr.App.Ui.Services.RenameList
{
    /// <summary>
    /// Builds rename sources from File List selection and folder state (masks, add policy).
    /// </summary>
    internal static class RenameListAddSources
    {
        /// <summary>
        /// Resolves engine add sources from the File List's current selection.
        /// </summary>
        /// <param name="fileList">File List pane state.</param>
        /// <param name="addFiles">When true, selected files that pass the File List masks are included.</param>
        /// <param name="addFolders">When true with or without <paramref name="addFiles"/>, selected folders become folder sources.</param>
        /// <returns>File paths and folder sources with a last-segment filename mask.</returns>
        public static IReadOnlyList<string> ResolveSourcesFromSelection(
            FileListViewModel fileList,
            bool addFiles,
            bool addFolders
        )
        {
            ArgumentNullException.ThrowIfNull(fileList);

            var sources = new List<string>();
            foreach (var entry in fileList.SelectedEntries)
            {
                if (!_IsAddablePath(entry.FullPath))
                {
                    continue;
                }

                if (entry.IsDirectory)
                {
                    if (addFiles || addFolders)
                    {
                        sources.Add(_BuildFolderSource(entry.FullPath, fileList.Mask));
                    }

                    continue;
                }

                if (addFiles && fileList.PassesFileMasks(entry.FullPath))
                {
                    sources.Add(entry.FullPath);
                }
            }

            return sources;
        }

        /// <summary>
        /// Resolves engine add sources from the File List's current folder.
        /// </summary>
        /// <param name="fileList">File List pane state.</param>
        /// <param name="addFiles">With <paramref name="addFolders"/>, gates whether the current folder is emitted.</param>
        /// <param name="addFolders">With <paramref name="addFiles"/>, gates whether the current folder is emitted.</param>
        /// <returns>A single folder source with a last-segment filename mask, or empty.</returns>
        public static IReadOnlyList<string> ResolveSourcesFromCurrentFolder(
            FileListViewModel fileList,
            bool addFiles,
            bool addFolders
        )
        {
            ArgumentNullException.ThrowIfNull(fileList);

            if (!fileList.CanAddAllToCurrentFolder || (!addFiles && !addFolders))
            {
                return [];
            }

            return [_BuildFolderSource(fileList.CurrentPath, fileList.Mask)];
        }

        /// <summary>
        /// Builds a directory source whose last segment is the File List include mask.
        /// </summary>
        private static string _BuildFolderSource(string folderPath, string mask)
        {
            var trimmedMask = string.IsNullOrWhiteSpace(mask) ? "*" : mask.Trim();
            return Path.Combine(folderPath, trimmedMask);
        }

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
