using Mfr.App.Ui.ViewModels.FileList;
using Mfr.Models.Config;
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
        /// <param name="ui">Rename List add-policy flags.</param>
        /// <returns>File paths and folder sources with a last-segment filename mask.</returns>
        public static IReadOnlyList<string> ResolveSourcesFromSelection(FileListViewModel fileList, UiConfig ui)
        {
            ArgumentNullException.ThrowIfNull(fileList);
            ArgumentNullException.ThrowIfNull(ui);

            var sources = new List<string>();
            foreach (var entry in fileList.SelectedEntries)
            {
                if (!_IsAddablePath(entry.FullPath))
                {
                    continue;
                }

                if (entry.IsDirectory)
                {
                    if (ui.AddFiles || ui.AddFolders)
                    {
                        var folderSource = _BuildFolderSource(entry.FullPath, fileList.Mask);
                        sources.Add(folderSource);
                    }

                    continue;
                }

                if (ui.AddFiles && fileList.PassesFileMasks(entry.FullPath))
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
        /// <param name="ui">Rename List add-policy flags.</param>
        /// <returns>A single folder source with a last-segment filename mask, or empty.</returns>
        public static IReadOnlyList<string> ResolveSourcesFromCurrentFolder(FileListViewModel fileList, UiConfig ui)
        {
            ArgumentNullException.ThrowIfNull(fileList);
            ArgumentNullException.ThrowIfNull(ui);

            if (!fileList.CanAddAllToCurrentFolder)
            {
                return [];
            }

            if (!ui.AddFiles && !ui.AddFolders)
            {
                return [];
            }

            return [_BuildFolderSource(fileList.CurrentPath, fileList.Mask)];
        }

        /// <summary>
        /// Whether <paramref name="path"/> can be passed to the engine add API.
        /// </summary>
        /// <param name="path">Candidate filesystem path.</param>
        /// <returns><see langword="false"/> for blank or root paths the engine rejects.</returns>
        public static bool IsAddablePath(string path)
        {
            return _IsAddablePath(path);
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
                if (string.IsNullOrEmpty(root))
                {
                    return false;
                }

                return !string.Equals(root, fullPath, PathComparers.OsComparison);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
            {
                return false;
            }
        }
    }
}
