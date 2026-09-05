using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace Mfr.App.Ui.Views.FilterEditors
{
    /// <summary>
    /// Shared File List / Explorer file-drop helpers for Filter Configuration editors.
    /// </summary>
    internal static class FilterEditorFileDrop
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

        /// <summary>
        /// Resolves the first dropped path to a folder: directories as-is; files use their parent directory.
        /// </summary>
        /// <param name="paths">Dropped local paths.</param>
        /// <returns>Absolute folder path, or <see langword="null"/> when none can be resolved.</returns>
        public static string? TryResolveFolderPath(IReadOnlyList<string> paths)
        {
            if (paths.Count == 0)
            {
                return null;
            }

            var path = paths[0];
            if (Directory.Exists(path))
            {
                return path;
            }

            var parent = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(parent))
            {
                return null;
            }

            return parent;
        }

        /// <summary>
        /// Enables file drops on <paramref name="target"/> and applies the first resolved folder path.
        /// </summary>
        /// <param name="target">Control that accepts the drop.</param>
        /// <param name="applyFolderPath">Receives the resolved absolute folder path.</param>
        public static void AttachFolderDrop(Control target, Action<string> applyFolderPath)
        {
            DragDrop.SetAllowDrop(target, true);
            target.AddHandler(DragDrop.DragOverEvent, _OnFolderDragOver);
            target.AddHandler(
                DragDrop.DropEvent,
                (sender, e) => _OnFolderDrop(e, applyFolderPath),
                RoutingStrategies.Bubble
            );
        }

        private static void _OnFolderDragOver(object? sender, DragEventArgs e)
        {
            e.DragEffects = HasFiles(e) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private static void _OnFolderDrop(DragEventArgs e, Action<string> applyFolderPath)
        {
            e.Handled = true;
            if (!HasFiles(e))
            {
                e.DragEffects = DragDropEffects.None;
                return;
            }

            var folder = TryResolveFolderPath(ReadLocalPaths(e));
            if (folder is null)
            {
                e.DragEffects = DragDropEffects.None;
                return;
            }

            e.DragEffects = DragDropEffects.Copy;
            applyFolderPath(folder);
        }
    }
}
