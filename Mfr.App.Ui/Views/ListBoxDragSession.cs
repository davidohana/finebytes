using Avalonia.Controls;
using Avalonia.Input;

namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// Transfer and effect for a ListBox drag that exceeds the movement threshold.
    /// </summary>
    /// <param name="Transfer">Data passed to <see cref="DragDrop.DoDragDropAsync"/>.</param>
    /// <param name="Effect">Allowed drag effect for the operation.</param>
    internal readonly record struct ListBoxDragStart(DataTransfer Transfer, DragDropEffects Effect);

    /// <summary>
    /// Press-to-drag state machine for a single ListBox (or one active list in a multi-list view).
    /// </summary>
    internal sealed class ListBoxDragSession
    {
        private ListBox? Source { get; set; }
        private ListBoxDragPress? Press { get; set; }

        /// <summary>
        /// List that received the active press, or <see langword="null"/> when idle.
        /// </summary>
        public ListBox? SourceList => Source;

        /// <summary>
        /// Selected indexes snapshotted on press for multi-select collapse undo.
        /// </summary>
        public IReadOnlyList<int>? SelectionSnapshot => Press?.SelectionSnapshot;

        /// <summary>
        /// Pressed row index when <see cref="SelectionSnapshot"/> is set.
        /// </summary>
        public int? HitIndex => Press?.HitIndex;

        /// <summary>
        /// Starts tracking a left-button press on a list row.
        /// </summary>
        /// <param name="listBox">List that received the press.</param>
        /// <param name="e">Tunnel pointer press.</param>
        /// <returns><see langword="true"/> when the press is on a row with the left button.</returns>
        public bool Capture(ListBox listBox, PointerPressedEventArgs e)
        {
            if (!ListBoxDrag.TryCapturePress(listBox, e, out var press))
            {
                return false;
            }

            Source = listBox;
            Press = press;
            return true;
        }

        /// <summary>
        /// Clears press state (capture lost, button up without drag, etc.).
        /// </summary>
        public void Clear()
        {
            Source = null;
            Press = null;
        }

        /// <summary>
        /// When pointer travel exceeds threshold, builds transfer and runs <see cref="DragDrop.DoDragDropAsync"/>.
        /// </summary>
        /// <param name="listBox">List being dragged from (must match the captured press).</param>
        /// <param name="e">Tunnel pointer move.</param>
        /// <param name="buildDrag">Builds transfer and effect; return <see langword="null"/> to abort.</param>
        /// <param name="afterDrag">Optional cleanup after drag completes (e.g. clear drop mark).</param>
        public async Task TryBeginDragAsync(
            ListBox listBox,
            PointerEventArgs e,
            Func<ListBoxDragStart?> buildDrag,
            Action? afterDrag = null
        )
        {
            if (Press is null || Source is null || !ReferenceEquals(listBox, Source))
            {
                return;
            }

            if (!e.GetCurrentPoint(listBox).Properties.IsLeftButtonPressed)
            {
                Clear();
                return;
            }

            if (ListBoxDrag.IsBelowThreshold(Press.Value.StartPoint, e.GetPosition(listBox)))
            {
                return;
            }

            var drag = buildDrag();
            if (drag is null)
            {
                Clear();
                return;
            }

            var dragArgs = Press.Value.StartArgs;
            Clear();

            try
            {
                await DragDrop.DoDragDropAsync(dragArgs, drag.Value.Transfer, drag.Value.Effect).ConfigureAwait(true);
            }
            finally
            {
                afterDrag?.Invoke();
            }
        }

        /// <summary>
        /// Handles pointer release when no drag started.
        /// </summary>
        /// <param name="onNoDrag">
        /// Invoked with list, snapshot, and hit index when a multi-select snapshot exists; otherwise skipped.
        /// </param>
        public void OnReleased(Action<ListBox, IReadOnlyList<int>, int>? onNoDrag = null)
        {
            if (
                Source is ListBox sourceList
                && Press?.SelectionSnapshot is { Count: > 0 } snapshot
                && Press?.HitIndex is int hit
            )
            {
                onNoDrag?.Invoke(sourceList, snapshot, hit);
            }

            Clear();
        }
    }
}
