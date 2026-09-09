using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mfr.App.Ui.Views.DragAndDrop;

namespace Mfr.App.Ui.Views.FilterEditors
{
    /// <summary>
    /// Shared File List / Explorer folder-drop helpers for Filter Configuration editors.
    /// </summary>
    internal static class FilterEditorFileDrop
    {
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
            e.DragEffects = LocalFileDrop.HasFiles(e) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private static void _OnFolderDrop(DragEventArgs e, Action<string> applyFolderPath)
        {
            e.Handled = true;
            if (!LocalFileDrop.HasFiles(e))
            {
                e.DragEffects = DragDropEffects.None;
                return;
            }

            var folder = TryResolveFolderPath(LocalFileDrop.ReadLocalPaths(e));
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
