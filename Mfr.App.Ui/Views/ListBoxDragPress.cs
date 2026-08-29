using Avalonia;
using Avalonia.Input;

namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// Pointer-press state for a ListBox drag that may preserve a multi-selection.
    /// </summary>
    /// <param name="StartPoint">Press origin relative to the list.</param>
    /// <param name="StartArgs">Press args passed to <see cref="DragDrop.DoDragDropAsync"/>.</param>
    /// <param name="SelectionSnapshot">
    /// Selected indexes when pressing an already-selected row in a multi-selection; otherwise <see langword="null"/>.
    /// </param>
    /// <param name="HitIndex">Pressed row index when <paramref name="SelectionSnapshot"/> is set.</param>
    internal readonly record struct ListBoxDragPress(
        Point StartPoint,
        PointerEventArgs StartArgs,
        IReadOnlyList<int>? SelectionSnapshot,
        int? HitIndex
    );
}
