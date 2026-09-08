using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// Opens a dialog height-to-content, then locks height so only width remains resizable.
    /// </summary>
    internal static class ModalDialogHorizontalResize
    {
        /// <summary>
        /// Attaches a one-shot open handler that measures content and locks height.
        /// </summary>
        /// <param name="window">Modal dialog window.</param>
        public static void Attach(Window window)
        {
            ArgumentNullException.ThrowIfNull(window);

            window.Opened += _OnOpened;

            void _OnOpened(object? sender, EventArgs e)
            {
                window.Opened -= _OnOpened;
                // Child controls (e.g. FormatEditor auto-grow) need a laid-out width first.
                Dispatcher.UIThread.Post(
                    () =>
                    {
                        window.UpdateLayout();
                        Dispatcher.UIThread.Post(
                            () => LockHeightToContent(window),
                            DispatcherPriority.Render
                        );
                    },
                    DispatcherPriority.Loaded
                );
            }
        }

        /// <summary>
        /// Remeasures <paramref name="window"/> content and locks min/max height to that value
        /// (horizontal resize only).
        /// </summary>
        /// <param name="window">Dialog to resize-lock.</param>
        public static void LockHeightToContent(Window window)
        {
            ArgumentNullException.ThrowIfNull(window);
            if (window.Content is not Control root)
            {
                return;
            }

            var width = window.ClientSize.Width;
            if (width <= 0)
            {
                width = window.Bounds.Width;
            }

            if (width <= 0)
            {
                return;
            }

            root.InvalidateMeasure();
            root.Measure(new Size(width, double.PositiveInfinity));
            var contentHeight = root.DesiredSize.Height;
            if (contentHeight <= 0)
            {
                return;
            }

            var frameChrome = Math.Max(0, window.Bounds.Height - window.ClientSize.Height);
            var height = contentHeight + frameChrome;

            window.SizeToContent = SizeToContent.Manual;
            window.Height = height;
            window.MinHeight = height;
            window.MaxHeight = height;
        }
    }
}
