using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.VisualTree;
using Path = Avalonia.Controls.Shapes.Path;

namespace Mfr.App.Ui.Views.RenameList
{
    /// <summary>
    /// Drop marker shown while reordering Rename List columns.
    /// </summary>
    internal sealed class RenameListColumnDropIndicator : Panel
    {
        private static readonly IBrush IndicatorBrush = new SolidColorBrush(Color.Parse("#FA8072"));

        private const double IndicatorWidth = 8;
        private const double LineWidth = 3;
        private const double ArrowWidth = 5;
        private const double ArrowHeight = 5;
        private const double ArrowTopMargin = 1;

        private static readonly Geometry LeftArrowGeometry = Geometry.Parse("M5,0 L0,2.5 L5,5 Z");
        private static readonly Geometry RightArrowGeometry = Geometry.Parse("M0,0 L5,2.5 L0,5 Z");

        private readonly Rectangle _line;
        private readonly Path _arrow;
        private readonly double _headerHeight;
        private DataGridColumnHeadersPresenter? _presenter;
        private bool? _isAppendAtEnd;

        /// <summary>
        /// Initializes the drop marker visuals.
        /// </summary>
        /// <param name="headerHeight">Column header height in pixels.</param>
        public RenameListColumnDropIndicator(double headerHeight)
        {
            _headerHeight = headerHeight > 0 ? headerHeight : 22;

            Width = IndicatorWidth;
            Height = _headerHeight;
            MinHeight = _headerHeight;
            IsHitTestVisible = false;
            ClipToBounds = false;

            _line = new Rectangle { Width = LineWidth, Fill = IndicatorBrush };

            _arrow = new Path
            {
                Width = ArrowWidth,
                Height = ArrowHeight,
                Fill = IndicatorBrush,
            };

            Children.Add(_line);
            Children.Add(_arrow);
            _ApplyArrowDirection(isAppendAtEnd: false);
        }

        /// <inheritdoc />
        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(IndicatorWidth, _headerHeight);
        }

        /// <inheritdoc />
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            _presenter = this.FindAncestorOfType<DataGridColumnHeadersPresenter>();
            if (_presenter is not null)
            {
                _presenter.LayoutUpdated += _OnPresenterLayoutUpdated;
                _OnPresenterLayoutUpdated(_presenter, EventArgs.Empty);
            }
        }

        /// <inheritdoc />
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _presenter?.LayoutUpdated -= _OnPresenterLayoutUpdated;
            _presenter = null;
            base.OnDetachedFromVisualTree(e);
        }

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size finalSize)
        {
            var height = finalSize.Height > 0 ? finalSize.Height : _headerHeight;
            _line.Arrange(new Rect(0, 0, LineWidth, height));
            _arrow.Arrange(_GetArrowBounds(height));
            _UpdateArrowDirection();
            return new Size(IndicatorWidth, height);
        }

        private void _OnPresenterLayoutUpdated(object? sender, EventArgs e)
        {
            _UpdateArrowDirection();
        }

        private void _UpdateArrowDirection()
        {
            if (_presenter is null)
            {
                return;
            }

            var dropOffset = this.TranslatePoint(new Point(), _presenter)?.X ?? Bounds.Left;
            var isAppendAtEnd = RenameListColumnDropPosition.IsAppendAtEnd(_presenter, dropOffset);
            if (_isAppendAtEnd == isAppendAtEnd)
            {
                return;
            }

            _isAppendAtEnd = isAppendAtEnd;
            _ApplyArrowDirection(isAppendAtEnd);
            InvalidateArrange();
        }

        private void _ApplyArrowDirection(bool isAppendAtEnd)
        {
            _arrow.Data = isAppendAtEnd ? RightArrowGeometry : LeftArrowGeometry;
        }

        private Rect _GetArrowBounds(double height)
        {
            var isAppendAtEnd = _isAppendAtEnd == true;
            var x = isAppendAtEnd ? LineWidth : 0;
            return new Rect(x, ArrowTopMargin, ArrowWidth, Math.Min(ArrowHeight, height));
        }
    }
}
