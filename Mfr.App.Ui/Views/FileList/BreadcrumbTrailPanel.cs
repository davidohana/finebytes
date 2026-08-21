using Avalonia;
using Avalonia.Controls;
using Mfr.App.Ui.ViewModels.FileList;

namespace Mfr.App.Ui.Views.FileList
{
    /// <summary>
    /// Lays out address-bar folders from the right so the current folder stays visible.
    /// <para>
    /// Hidden ancestors are moved off-screen and clipped. The host overlays an overflow
    /// button in the reserved leading gap when <see cref="HasOverflow"/> is true.
    /// </para>
    /// </summary>
    public sealed class BreadcrumbTrailPanel : Panel
    {
        /// <summary>
        /// Width reserved on the left when leading folders are collapsed.
        /// </summary>
        public static readonly StyledProperty<double> OverflowButtonWidthProperty = AvaloniaProperty.Register<
            BreadcrumbTrailPanel,
            double
        >(nameof(OverflowButtonWidth), BreadcrumbOverflow.ButtonWidth);

        /// <summary>
        /// Whether any ancestor folder is hidden behind the overflow button.
        /// </summary>
        public static readonly DirectProperty<BreadcrumbTrailPanel, bool> HasOverflowProperty =
            AvaloniaProperty.RegisterDirect<BreadcrumbTrailPanel, bool>(
                nameof(HasOverflow),
                panel => panel.HasOverflow
            );

        /// <summary>
        /// Index of the first visible folder in root-to-current order.
        /// </summary>
        public static readonly DirectProperty<BreadcrumbTrailPanel, int> VisibleStartIndexProperty =
            AvaloniaProperty.RegisterDirect<BreadcrumbTrailPanel, int>(
                nameof(VisibleStartIndex),
                panel => panel.VisibleStartIndex
            );

        static BreadcrumbTrailPanel()
        {
            AffectsMeasure<BreadcrumbTrailPanel>(OverflowButtonWidthProperty);
        }

        /// <summary>
        /// Initializes the trail panel and clips collapsed folders so they cannot paint.
        /// </summary>
        public BreadcrumbTrailPanel()
        {
            ClipToBounds = true;
        }

        /// <summary>
        /// Gets or sets the width reserved for the overflow button when ancestors are hidden.
        /// </summary>
        public double OverflowButtonWidth
        {
            get => GetValue(OverflowButtonWidthProperty);
            set => SetValue(OverflowButtonWidthProperty, value);
        }

        /// <summary>
        /// Gets whether leading folders are collapsed into the overflow button.
        /// </summary>
        public bool HasOverflow
        {
            get;
            private set => SetAndRaise(HasOverflowProperty, ref field, value);
        }

        /// <summary>
        /// Gets the first visible folder index. <c>0</c> means the full trail is shown.
        /// </summary>
        public int VisibleStartIndex
        {
            get;
            private set => SetAndRaise(VisibleStartIndexProperty, ref field, value);
        }

        /// <inheritdoc />
        protected override Size MeasureOverride(Size availableSize)
        {
            var count = Children.Count;
            if (count == 0)
            {
                HasOverflow = false;
                VisibleStartIndex = 0;
                return default;
            }

            var unconstrained = new Size(double.PositiveInfinity, availableSize.Height);
            var widths = new double[count];
            var height = 0d;
            for (var i = 0; i < count; i++)
            {
                var child = Children[i];
                child.ClearValue(MaxWidthProperty);
                child.Measure(unconstrained);
                widths[i] = child.DesiredSize.Width;
                height = Math.Max(height, child.DesiredSize.Height);
            }

            var visibleStart = BreadcrumbOverflow.PickVisibleStart(widths, availableSize.Width, OverflowButtonWidth);
            VisibleStartIndex = visibleStart;
            HasOverflow = visibleStart > 0;

            _ConstrainLastVisibleChild(widths, availableSize, visibleStart);

            if (double.IsInfinity(availableSize.Width) || double.IsNaN(availableSize.Width))
            {
                return new Size(_Sum(widths), height);
            }

            var usedWidth = HasOverflow ? OverflowButtonWidth : 0;
            for (var i = visibleStart; i < count; i++)
            {
                usedWidth += Children[i].DesiredSize.Width;
            }

            return new Size(Math.Min(usedWidth, availableSize.Width), height);
        }

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size finalSize)
        {
            var count = Children.Count;
            if (count == 0)
            {
                return finalSize;
            }

            var x = HasOverflow ? OverflowButtonWidth : 0;
            for (var i = 0; i < count; i++)
            {
                var child = Children[i];
                var isCollapsed = i < VisibleStartIndex;
                child.Opacity = isCollapsed ? 0 : 1;
                child.IsHitTestVisible = !isCollapsed;
                if (isCollapsed)
                {
                    child.Arrange(new Rect(-10000, 0, child.DesiredSize.Width, child.DesiredSize.Height));
                    continue;
                }

                var width = child.DesiredSize.Width;
                var remaining = Math.Max(0, finalSize.Width - x);
                if (width > remaining)
                {
                    width = remaining;
                }

                var y = Math.Max(0, (finalSize.Height - child.DesiredSize.Height) / 2);
                child.Arrange(new Rect(x, y, width, child.DesiredSize.Height));
                x += width;
            }

            return finalSize;
        }

        private void _ConstrainLastVisibleChild(double[] widths, Size availableSize, int visibleStart)
        {
            if (double.IsInfinity(availableSize.Width) || double.IsNaN(availableSize.Width))
            {
                return;
            }

            var lastIndex = widths.Length - 1;
            var usedBeforeLast = HasOverflow ? OverflowButtonWidth : 0;
            for (var i = visibleStart; i < lastIndex; i++)
            {
                usedBeforeLast += widths[i];
            }

            var remainingForLast = availableSize.Width - usedBeforeLast;
            if (remainingForLast >= widths[lastIndex])
            {
                return;
            }

            var maxWidth = Math.Max(0, remainingForLast);
            var lastChild = Children[lastIndex];
            lastChild.MaxWidth = maxWidth;
            lastChild.Measure(new Size(maxWidth, availableSize.Height));
        }

        private static double _Sum(double[] widths)
        {
            var total = 0d;
            foreach (var width in widths)
            {
                total += width;
            }

            return total;
        }
    }
}
