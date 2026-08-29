using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Media;

namespace Mfr.App.Ui.Views.DragAndDrop
{
    /// <summary>
    /// Salmon insert marker for ListBox drag-and-drop: row class, or a line after the last item.
    /// </summary>
    internal sealed class ListBoxDropMark
    {
        private const string _IndicatorBrushKey = "RenameListDropIndicatorBrush";
        private static readonly IBrush _FallbackBrush = new SolidColorBrush(Color.Parse("#FA8072"));

        private ListBoxItem? _dropMarkItem;
        private ListBox? _dropMarkList;
        private int? _dropMarkInsertIndex;
        private Canvas? _appendMarkHost;
        private Rectangle? _appendMarkLine;

        /// <summary>
        /// Resolves the insert-marker brush from <paramref name="owner"/>'s theme, with a salmon fallback.
        /// </summary>
        private static IBrush _ResolveBrush(StyledElement owner)
        {
            if (
                owner.TryGetResource(_IndicatorBrushKey, owner.ActualThemeVariant, out var resource)
                && resource is IBrush brush
            )
            {
                return brush;
            }

            return _FallbackBrush;
        }

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
                return;
            }

            Clear();
            _dropMarkList = listBox;
            _dropMarkInsertIndex = insertIndex;

            if (insertIndex < listBox.ItemCount)
            {
                if (listBox.ContainerFromIndex(insertIndex) is ListBoxItem item)
                {
                    item.Classes.Set("drop-mark", true);
                    _dropMarkItem = item;
                }

                return;
            }

            _ShowAppendMark(listBox);
        }

        /// <summary>
        /// Hides the insert marker.
        /// </summary>
        public void Clear()
        {
            _dropMarkItem?.Classes.Set("drop-mark", false);
            _dropMarkItem = null;

            if (_dropMarkList is { } markedList)
            {
                AdornerLayer.SetAdorner(markedList, null);
            }

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

        private void _ShowAppendMark(ListBox listBox)
        {
            var y = 2.0;
            if (
                listBox.ItemCount > 0
                && listBox.ContainerFromIndex(listBox.ItemCount - 1) is Control lastItem
                && lastItem.TranslatePoint(default, listBox) is { } origin
            )
            {
                y = origin.Y + lastItem.Bounds.Height;
            }

            _appendMarkHost ??= new Canvas { IsHitTestVisible = false };
            _appendMarkLine ??= new Rectangle
            {
                Height = 3,
                IsHitTestVisible = false,
                Fill = _ResolveBrush(listBox),
            };

            if (_appendMarkLine.Parent is null)
            {
                _appendMarkHost.Children.Add(_appendMarkLine);
            }

            _appendMarkLine.Width = Math.Max(0, listBox.Bounds.Width - 4);
            Canvas.SetLeft(_appendMarkLine, 2);
            Canvas.SetTop(_appendMarkLine, Math.Clamp(y - 1.5, 0, Math.Max(0, listBox.Bounds.Height - 3)));
            AdornerLayer.SetAdorner(listBox, _appendMarkHost);
        }
    }
}
