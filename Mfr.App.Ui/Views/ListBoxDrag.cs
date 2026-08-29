using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// Shared ListBox drag-and-drop geometry, press capture, and selection restore.
    /// </summary>
    internal static class ListBoxDrag
    {
        /// <summary>
        /// Pointer travel (device-independent pixels) before a press becomes a drag.
        /// </summary>
        public const double Threshold = 4;

        /// <summary>
        /// Captures a left-button press on a list item, snapshotting a multi-selection that Avalonia would collapse.
        /// </summary>
        /// <param name="listBox">List that received the press.</param>
        /// <param name="e">Tunnel pointer press.</param>
        /// <param name="press">Press state when the gesture should start a drag.</param>
        /// <returns><see langword="true"/> when the press is on a row with the left button.</returns>
        public static bool TryCapturePress(ListBox listBox, PointerPressedEventArgs e, out ListBoxDragPress press)
        {
            press = default;
            if (!e.GetCurrentPoint(listBox).Properties.IsLeftButtonPressed)
            {
                return false;
            }

            if (FindItemFromSource(e.Source) is not { } item)
            {
                return false;
            }

            var hitIndex = listBox.IndexFromContainer(item);
            var selectedIndices = ReadSelectedIndices(listBox);
            var isExtendingSelection =
                e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Shift);

            IReadOnlyList<int>? snapshot = null;
            int? hit = null;
            if (
                hitIndex >= 0
                && selectedIndices.Count > 1
                && !isExtendingSelection
                && selectedIndices.Contains(hitIndex)
            )
            {
                snapshot = selectedIndices;
                hit = hitIndex;
            }

            press = new ListBoxDragPress(e.GetPosition(listBox), e, snapshot, hit);
            return true;
        }

        /// <summary>
        /// Gets whether pointer travel from <paramref name="start"/> is still inside <see cref="Threshold"/>.
        /// </summary>
        /// <param name="start">Press origin relative to the list.</param>
        /// <param name="current">Current pointer position relative to the list.</param>
        /// <returns><see langword="true"/> when a drag should not start yet.</returns>
        public static bool IsBelowThreshold(Point start, Point current)
        {
            var delta = current - start;
            return Math.Abs(delta.X) < Threshold && Math.Abs(delta.Y) < Threshold;
        }

        /// <summary>
        /// Walks visual parents from <paramref name="source"/> to the owning <see cref="ListBoxItem"/>.
        /// </summary>
        /// <param name="source">Event source.</param>
        /// <returns>The list item, or <see langword="null"/>.</returns>
        public static ListBoxItem? FindItemFromSource(object? source)
        {
            for (var current = source as Visual; current is not null; current = current.GetVisualParent())
            {
                if (current is ListBoxItem item)
                {
                    return item;
                }
            }

            return null;
        }

        /// <summary>
        /// Reads selected row indices in list order.
        /// </summary>
        /// <param name="listBox">Source list.</param>
        /// <returns>Non-negative selected indexes, sorted.</returns>
        public static IReadOnlyList<int> ReadSelectedIndices(ListBox listBox)
        {
            return [.. listBox.Selection.SelectedIndexes.Where(index => index >= 0).OrderBy(index => index)];
        }

        /// <summary>
        /// Resolves the insert index for a drop at <paramref name="position"/> (append when missing a row hit).
        /// </summary>
        /// <param name="listBox">Drop target list.</param>
        /// <param name="position">Pointer position relative to <paramref name="listBox"/>.</param>
        /// <returns>Index in <c>[0, ItemCount]</c>.</returns>
        public static int GetDropIndex(ListBox listBox, Point position)
        {
            var item = HitTestItem(listBox, position);
            if (item is null)
            {
                return listBox.ItemCount;
            }

            var itemIndex = listBox.IndexFromContainer(item);
            if (itemIndex < 0)
            {
                return listBox.ItemCount;
            }

            var itemOrigin = item.TranslatePoint(default, listBox);
            if (itemOrigin is null)
            {
                return itemIndex;
            }

            var midpoint = itemOrigin.Value.Y + (item.Bounds.Height / 2);
            return position.Y >= midpoint ? itemIndex + 1 : itemIndex;
        }

        /// <summary>
        /// Hits the list item whose bounds contain <paramref name="position"/>.
        /// </summary>
        /// <param name="listBox">List to search.</param>
        /// <param name="position">Pointer position relative to <paramref name="listBox"/>.</param>
        /// <returns>The item under the pointer, or <see langword="null"/>.</returns>
        public static ListBoxItem? HitTestItem(ListBox listBox, Point position)
        {
            foreach (var item in listBox.GetVisualDescendants().OfType<ListBoxItem>())
            {
                var origin = item.TranslatePoint(default, listBox);
                if (origin is null)
                {
                    continue;
                }

                if (new Rect(origin.Value, item.Bounds.Size).Contains(position))
                {
                    return item;
                }
            }

            return null;
        }

        /// <summary>
        /// Applies <paramref name="indices"/> to <paramref name="listBox"/> without collapsing to the anchor row.
        /// </summary>
        /// <param name="listBox">List to update.</param>
        /// <param name="indices">Desired selected indexes.</param>
        /// <param name="anchorIndex">Selection anchor, or <c>-1</c>.</param>
        public static void RestoreSelection(ListBox listBox, IReadOnlyList<int> indices, int anchorIndex)
        {
            var itemCount = listBox.ItemCount;
            var desired = indices.Where(index => index >= 0 && index < itemCount).ToList();
            if (_SelectionMatches(listBox, desired))
            {
                if (anchorIndex >= 0 && anchorIndex < itemCount)
                {
                    listBox.Selection.AnchorIndex = anchorIndex;
                }

                return;
            }

            var selection = listBox.Selection;
            selection.BeginBatchUpdate();
            try
            {
                selection.Clear();
                foreach (var index in desired)
                {
                    selection.Select(index);
                }

                if (anchorIndex >= 0 && anchorIndex < itemCount && desired.Contains(anchorIndex))
                {
                    selection.AnchorIndex = anchorIndex;
                    selection.Select(anchorIndex);
                }
            }
            finally
            {
                selection.EndBatchUpdate();
            }
        }

        private static bool _SelectionMatches(ListBox listBox, IReadOnlyList<int> desired)
        {
            var current = listBox.Selection.SelectedIndexes.Where(index => index >= 0).OrderBy(index => index).ToList();
            return desired.SequenceEqual(current);
        }
    }
}
