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
        /// <returns>Paths and glob patterns to pass to the engine add API.</returns>
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
                    _AddFolderSource(
                        sources: sources,
                        folderPath: entry.FullPath,
                        ui: ui,
                        isAddAll: false,
                        mask: fileList.Mask,
                        excludeMasksEnabled: fileList.ExcludeMasksEnabled,
                        excludeMasks: fileList.ExcludeMasks,
                        passesFileMasks: fileList.PassesFileMasks
                    );
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
        /// <returns>Paths and glob patterns to pass to the engine add API.</returns>
        public static IReadOnlyList<string> ResolveSourcesFromCurrentFolder(FileListViewModel fileList, UiConfig ui)
        {
            ArgumentNullException.ThrowIfNull(fileList);
            ArgumentNullException.ThrowIfNull(ui);

            if (!fileList.CanAddAllToCurrentFolder)
            {
                return [];
            }

            var sources = new List<string>();
            _AddFolderSource(
                sources: sources,
                folderPath: fileList.CurrentPath,
                ui: ui,
                isAddAll: true,
                mask: fileList.Mask,
                excludeMasksEnabled: fileList.ExcludeMasksEnabled,
                excludeMasks: fileList.ExcludeMasks,
                passesFileMasks: fileList.PassesFileMasks
            );
            return sources;
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

        private static void _AddFolderSource(
            List<string> sources,
            string folderPath,
            UiConfig ui,
            bool isAddAll,
            string mask,
            bool excludeMasksEnabled,
            IReadOnlyList<string> excludeMasks,
            Func<string, bool> passesFileMasks
        )
        {
            if (ui.AddFolders)
            {
                sources.Add(folderPath);
            }

            if (!ui.AddFiles)
            {
                return;
            }

            var includeSubdirs = _ResolveIncludeSubdirs(ui: ui, isAddAll: isAddAll);
            if (_CanUseMaskGlob(mask, excludeMasksEnabled, excludeMasks))
            {
                sources.Add(_BuildMaskGlob(folderPath: folderPath, mask: mask, includeSubdirs: includeSubdirs));
                return;
            }

            foreach (var filePath in _EnumerateFiles(folderPath: folderPath, includeSubdirs: includeSubdirs))
            {
                if (passesFileMasks(filePath))
                {
                    sources.Add(filePath);
                }
            }
        }

        private static bool _ResolveIncludeSubdirs(UiConfig ui, bool isAddAll)
        {
            if (ui.AddFolderContents)
            {
                return true;
            }

            if (isAddAll)
            {
                return false;
            }

            return false;
        }

        private static bool _CanUseMaskGlob(string mask, bool excludeMasksEnabled, IReadOnlyList<string> excludeMasks)
        {
            if (excludeMasksEnabled && excludeMasks.Count > 0)
            {
                return false;
            }

            return !string.IsNullOrWhiteSpace(mask);
        }

        private static string _BuildMaskGlob(string folderPath, string mask, bool includeSubdirs)
        {
            var trimmedMask = mask.Trim();
            if (!includeSubdirs)
            {
                return Path.Combine(folderPath, trimmedMask);
            }

            if (trimmedMask == "*")
            {
                return Path.Combine(folderPath, "**", "*");
            }

            return Path.Combine(folderPath, "**", trimmedMask);
        }

        private static IEnumerable<string> _EnumerateFiles(string folderPath, bool includeSubdirs)
        {
            var searchOption = includeSubdirs ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            return Directory.EnumerateFiles(folderPath, "*", searchOption);
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
