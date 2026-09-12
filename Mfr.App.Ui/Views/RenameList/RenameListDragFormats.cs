using Avalonia.Input;

namespace Mfr.App.Ui.Views.RenameList
{
    /// <summary>
    /// Shared Avalonia data formats for Rename List row drags (reorder and drag-back to File List).
    /// </summary>
    internal static class RenameListDragFormats
    {
        /// <summary>
        /// Application format for dragging Rename List rows to reorder within the grid or remove via File List.
        /// </summary>
        public static readonly DataFormat<string> InternalReorder = DataFormat.CreateStringApplicationFormat(
            "mfr-rename-list-reorder"
        );
    }
}
