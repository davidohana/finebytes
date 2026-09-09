using Avalonia.Input;
using Avalonia.Platform.Storage;

namespace Mfr.App.Ui.Views.DragAndDrop
{
    /// <summary>
    /// Shared helpers for reading local filesystem paths from an Avalonia file-drop transfer.
    /// </summary>
    internal static class LocalFileDrop
    {
        /// <summary>
        /// Returns whether <paramref name="e"/> carries filesystem items.
        /// </summary>
        /// <param name="e">Drag event.</param>
        /// <returns><see langword="true"/> when a file/folder payload is present.</returns>
        public static bool HasFiles(DragEventArgs e)
        {
            return e.DataTransfer?.Formats.Contains(DataFormat.File) == true;
        }

        /// <summary>
        /// Reads local filesystem paths from a file-drop transfer.
        /// </summary>
        /// <param name="e">Drag event.</param>
        /// <returns>Local paths in drop order; empty when none resolve.</returns>
        public static IReadOnlyList<string> ReadLocalPaths(DragEventArgs e)
        {
            var files = e.DataTransfer?.TryGetFiles();
            if (files is null || files.Length == 0)
            {
                return [];
            }

            var paths = new List<string>(files.Length);
            foreach (var file in files)
            {
                var path = file.TryGetLocalPath();
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                paths.Add(path);
            }

            return paths;
        }
    }
}
