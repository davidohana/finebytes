using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;

namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// Uniform-size wrapping panel that realizes only items in the effective viewport.
    /// </summary>
    public sealed class VirtualizingWrapPanel : VirtualizingPanel
    {
        /// <summary>
        /// Defines the <see cref="ItemWidth"/> property.
        /// </summary>
        public static readonly StyledProperty<double> ItemWidthProperty =
            AvaloniaProperty.Register<VirtualizingWrapPanel, double>(nameof(ItemWidth), double.NaN);

        /// <summary>
        /// Defines the <see cref="ItemHeight"/> property.
        /// </summary>
        public static readonly StyledProperty<double> ItemHeightProperty =
            AvaloniaProperty.Register<VirtualizingWrapPanel, double>(nameof(ItemHeight), double.NaN);

        private const int _ViewportRowBuffer = 3;

        private readonly Dictionary<int, Control> _indexToContainer = [];
        private readonly Dictionary<Control, int> _containerToIndex = [];
        private readonly Dictionary<object, Stack<Control>> _recycleKeyToPool = [];
        private readonly HashSet<Control> _ownContainers = [];
        private readonly Size _measuredItemSize = new(104, 140);
        private Rect _viewport;
        private int _columnCount = 1;

        /// <summary>
        /// Gets or sets the uniform cell width. <see cref="double.NaN"/> measures from the first item.
        /// </summary>
        public double ItemWidth
        {
            get => GetValue(ItemWidthProperty);
            set => SetValue(ItemWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the uniform cell height. <see cref="double.NaN"/> measures from the first item.
        /// </summary>
        public double ItemHeight
        {
            get => GetValue(ItemHeightProperty);
            set => SetValue(ItemHeightProperty, value);
        }

        /// <summary>
        /// Initializes a new wrapping virtualizing panel.
        /// </summary>
        public VirtualizingWrapPanel()
        {
            ClipToBounds = false;
            EffectiveViewportChanged += _OnEffectiveViewportChanged;
        }

        /// <inheritdoc />
        protected override Size MeasureOverride(Size availableSize)
        {
            var itemCount = Items.Count;
            if (itemCount == 0)
            {
                _ClearRealizedAndPooled();
                return default;
            }

            var itemSize = _GetItemSize();
            var viewportWidth = availableSize.Width;
            if (double.IsInfinity(viewportWidth) || viewportWidth <= 0)
                viewportWidth = Math.Max(_viewport.Width, itemSize.Width);

            _columnCount = Math.Max(1, (int)(viewportWidth / itemSize.Width));
            var rowCount = (itemCount + _columnCount - 1) / _columnCount;
            _RealizeViewport(itemSize, viewportWidth);

            var width = Math.Min(viewportWidth, _columnCount * itemSize.Width);
            return new Size(width, rowCount * itemSize.Height);
        }

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size finalSize)
        {
            var itemSize = _GetItemSize();
            foreach (var (index, container) in _indexToContainer)
                container.Arrange(_GetItemRect(index, _columnCount, itemSize));

            return finalSize;
        }

        /// <inheritdoc />
        protected override Control? ScrollIntoView(int index)
        {
            if (index < 0 || index >= Items.Count)
                return null;

            var itemSize = _GetItemSize();
            var container = _Realize(index);
            if (container is null)
                return null;

            container.Arrange(_GetItemRect(index, _columnCount, itemSize));
            container.BringIntoView();
            return container;
        }

        /// <inheritdoc />
        protected override Control? ContainerFromIndex(int index)
        {
            return _indexToContainer.TryGetValue(index, out var container) ? container : null;
        }

        /// <inheritdoc />
        protected override int IndexFromContainer(Control container)
        {
            return _containerToIndex.TryGetValue(container, out var index) ? index : -1;
        }

        /// <inheritdoc />
        protected override IEnumerable<Control>? GetRealizedContainers()
        {
            return _indexToContainer.Values;
        }

        /// <inheritdoc />
        protected override IInputElement? GetControl(
            NavigationDirection direction,
            IInputElement? from,
            bool wrap)
        {
            var itemCount = Items.Count;
            if (itemCount == 0)
                return null;

            var fromIndex = from is Control fromControl ? IndexFromContainer(fromControl) : -1;
            var nextIndex = _GetNextIndex(direction, fromIndex, itemCount, wrap);
            if (nextIndex < 0)
                return null;

            return ScrollIntoView(nextIndex);
        }

        /// <inheritdoc />
        protected override void OnItemsChanged(IReadOnlyList<object?> items, NotifyCollectionChangedEventArgs e)
        {
            _ClearRealizedAndPooled();
            InvalidateMeasure();
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == ItemWidthProperty || change.Property == ItemHeightProperty)
                InvalidateMeasure();
        }

        private void _OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
        {
            _viewport = e.EffectiveViewport;
            InvalidateMeasure();
        }

        private void _RealizeViewport(Size itemSize, double viewportWidth)
        {
            var itemCount = Items.Count;
            var columns = Math.Max(1, (int)(viewportWidth / itemSize.Width));
            _columnCount = columns;
            var viewport = _viewport;
            if (viewport.Width <= 0 && viewport.Height <= 0)
                viewport = new Rect(0, 0, viewportWidth, itemSize.Height * 2);

            var firstRow = Math.Max(0, (int)Math.Floor(viewport.Y / itemSize.Height) - _ViewportRowBuffer);
            var lastRow = Math.Max(
                firstRow,
                (int)Math.Floor((viewport.Bottom - 1) / itemSize.Height) + _ViewportRowBuffer);
            var firstIndex = Math.Min(itemCount - 1, firstRow * columns);
            var lastIndex = Math.Min(itemCount - 1, ((lastRow + 1) * columns) - 1);
            if (firstIndex < 0)
                return;

            var keep = new HashSet<int>();
            for (var i = firstIndex; i <= lastIndex; i++)
                keep.Add(i);

            var toUnrealize = new List<int>();
            foreach (var index in _indexToContainer.Keys)
            {
                if (!keep.Contains(index))
                    toUnrealize.Add(index);
            }

            foreach (var index in toUnrealize)
                _Unrealize(index);

            for (var i = firstIndex; i <= lastIndex; i++)
                _Realize(i);
        }

        private Control? _Realize(int index)
        {
            if (_indexToContainer.TryGetValue(index, out var existing))
                return existing;

            var generator = ItemContainerGenerator;
            if (generator is null)
                return null;

            var item = Items[index];
            var needsContainer = generator.NeedsContainer(item, index, out var recycleKey);
            Control container;
            if (!needsContainer)
            {
                container = (Control)item!;
                _ownContainers.Add(container);
            }
            else if (recycleKey is not null
                && _recycleKeyToPool.TryGetValue(recycleKey, out var pool)
                && pool.Count > 0)
            {
                container = pool.Pop();
                container.IsVisible = true;
            }
            else
            {
                container = generator.CreateContainer(item, index, recycleKey);
                AddInternalChild(container);
            }

            generator.PrepareItemContainer(container, item, index);
            generator.ItemContainerPrepared(container, item, index);
            _indexToContainer[index] = container;
            _containerToIndex[container] = index;

            var itemSize = _GetItemSize();
            container.Measure(new Size(itemSize.Width, itemSize.Height));
            container.Arrange(_GetItemRect(index, _columnCount, itemSize));
            return container;
        }

        private void _Unrealize(int index)
        {
            if (!_indexToContainer.Remove(index, out var container))
                return;

            _containerToIndex.Remove(container);
            var generator = ItemContainerGenerator;
            if (generator is null)
                return;

            var item = Items.Count > index ? Items[index] : null;
            if (_ownContainers.Contains(container))
                return;

            generator.ClearItemContainer(container);
            if (item is null || !generator.NeedsContainer(item, index, out var recycleKey) || recycleKey is null)
            {
                RemoveInternalChild(container);
                return;
            }

            container.IsVisible = false;
            if (!_recycleKeyToPool.TryGetValue(recycleKey, out var pool))
            {
                pool = new Stack<Control>();
                _recycleKeyToPool[recycleKey] = pool;
            }

            pool.Push(container);
        }

        private void _ClearRealizedAndPooled()
        {
            var generator = ItemContainerGenerator;
            foreach (var container in _indexToContainer.Values)
            {
                generator?.ClearItemContainer(container);
                RemoveInternalChild(container);
            }

            _indexToContainer.Clear();
            _containerToIndex.Clear();
            _ownContainers.Clear();
            foreach (var pool in _recycleKeyToPool.Values)
            {
                while (pool.Count > 0)
                    RemoveInternalChild(pool.Pop());
            }

            _recycleKeyToPool.Clear();
        }

        private Size _GetItemSize()
        {
            var width = ItemWidth;
            var height = ItemHeight;
            if (double.IsNaN(width) || width <= 0)
                width = _measuredItemSize.Width;
            if (double.IsNaN(height) || height <= 0)
                height = _measuredItemSize.Height;
            if (width <= 0)
                width = 104;
            if (height <= 0)
                height = 132;

            return new Size(width, height);
        }

        private static Rect _GetItemRect(int index, int columns, Size itemSize)
        {
            var column = index % columns;
            var row = index / columns;
            return new Rect(column * itemSize.Width, row * itemSize.Height, itemSize.Width, itemSize.Height);
        }

        private int _GetNextIndex(NavigationDirection direction, int fromIndex, int itemCount, bool wrap)
        {
            if (fromIndex < 0)
                return 0;

            var next = direction switch
            {
                NavigationDirection.Next or NavigationDirection.Right => fromIndex + 1,
                NavigationDirection.Previous or NavigationDirection.Left => fromIndex - 1,
                NavigationDirection.Down or NavigationDirection.PageDown => fromIndex + _columnCount,
                NavigationDirection.Up or NavigationDirection.PageUp => fromIndex - _columnCount,
                NavigationDirection.First => 0,
                NavigationDirection.Last => itemCount - 1,
                _ => fromIndex,
            };

            if (wrap)
            {
                if (next < 0)
                    next = itemCount - 1;
                else if (next >= itemCount)
                    next = 0;
            }

            if (next < 0 || next >= itemCount)
                return -1;

            return next;
        }
    }
}
