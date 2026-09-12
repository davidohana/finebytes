using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;

namespace Mfr.App.Ui.Views.DragAndDrop
{
    /// <summary>
    /// 3px salmon horizontal insert line shown on an AdornerLayer while dragging.
    /// </summary>
    internal sealed class DropInsertLineAdorner
    {
        /// <summary>
        /// Insert-line thickness in device-independent pixels.
        /// </summary>
        public const double Thickness = 3;

        private Canvas? _host;
        private Rectangle? _line;

        /// <summary>
        /// Shows or moves the insert line on <paramref name="target"/> at vertical position <paramref name="y"/>.
        /// </summary>
        /// <param name="target">Control that hosts the adorner (list or grid).</param>
        /// <param name="y">Line center Y relative to <paramref name="target"/>.</param>
        public void Show(Control target, double y)
        {
            _host ??= new Canvas { IsHitTestVisible = false };
            _line ??= new Rectangle { Height = Thickness, IsHitTestVisible = false };

            if (_line.Parent is null)
            {
                _host.Children.Add(_line);
            }

            _line.Fill = DropMarkBrushes.Resolve(target);
            _line.Width = Math.Max(0, target.Bounds.Width - 4);
            Canvas.SetLeft(_line, 2);
            Canvas.SetTop(_line, Math.Clamp(y - (Thickness / 2), 0, Math.Max(0, target.Bounds.Height - Thickness)));
            AdornerLayer.SetAdorner(target, _host);
        }

        /// <summary>
        /// Removes the insert line from <paramref name="target"/>.
        /// </summary>
        /// <param name="target">Control that previously hosted the adorner.</param>
        public void Clear(Control? target)
        {
            if (target is not null)
            {
                AdornerLayer.SetAdorner(target, null);
            }
        }
    }
}
