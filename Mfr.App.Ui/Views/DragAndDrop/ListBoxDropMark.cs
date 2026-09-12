using Avalonia;
using Avalonia.Controls;

namespace Mfr.App.Ui.Views.DragAndDrop
{
    /// <summary>
    /// Salmon insert-line marker for ListBox drag-and-drop reorder.
    /// </summary>
    internal sealed class ListBoxDropMark
    {
        private readonly DropInsertLineAdorner _line = new();
        private ListBox? _dropMarkList;
        private int? _dropMarkInsertIndex;

        /// <summary>
        /// Shows or moves the insert marker for a drop at <paramref name="position"/>.
        /// </summary>
        /// <param name="listBox">Drop target list.</param>
        /// <param name="position">Pointer position relative to <paramref name="listBox"/>.</param>
        public void Update(ListBox listBox, Point position)
        {
            var insertIndex = ListBoxDrag.GetDropIndex(listBox, position);
            if (ReferenceEquals(listBox, _dropMarkList) && _dropMarkInsertIndex == insertIndex)
            {
                // Refresh Y when the same slot stays marked (layout / virtualization).
                _line.Show(listBox, _GetInsertLineY(listBox, insertIndex));
                return;
            }

            Clear();
            _dropMarkList = listBox;
            _dropMarkInsertIndex = insertIndex;
            _line.Show(listBox, _GetInsertLineY(listBox, insertIndex));
        }

        /// <summary>
        /// Hides the insert marker.
        /// </summary>
        public void Clear()
        {
            _line.Clear(_dropMarkList);
            _dropMarkList = null;
            _dropMarkInsertIndex = null;
        }

        /// <summary>
        /// Hides the insert marker when it is shown on <paramref name="listBox"/>.
        /// </summary>
        /// <param name="listBox">List that raised drag-leave.</param>
        public void ClearIfHost(ListBox listBox)
        {
            if (ReferenceEquals(listBox, _dropMarkList))
            {
                Clear();
            }
        }

        /// <summary>
        /// Vertical position for the insert line at <paramref name="insertIndex"/> (top of row, or below last).
        /// </summary>
        private static double _GetInsertLineY(ListBox listBox, int insertIndex)
        {
            if (listBox.ItemCount == 0)
            {
                return 2.0;
            }

            if (insertIndex < listBox.ItemCount)
            {
                if (
                    listBox.ContainerFromIndex(insertIndex) is Control item
                    && item.TranslatePoint(default, listBox) is { } origin
                )
                {
                    return origin.Y;
                }

                return 2.0;
            }

            if (
                listBox.ContainerFromIndex(listBox.ItemCount - 1) is Control lastItem
                && lastItem.TranslatePoint(default, listBox) is { } lastOrigin
            )
            {
                return lastOrigin.Y + lastItem.Bounds.Height;
            }

            return 2.0;
        }
    }
}
