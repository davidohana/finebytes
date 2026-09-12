using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Shared Avalonia Headless pointer helpers (window-routed clicks for Avalonia 12).
    /// </summary>
    internal static class HeadlessPointerClicks
    {
        /// <summary>
        /// Moves to <paramref name="target"/> and left-clicks through the host window.
        /// </summary>
        /// <param name="window">Host window for pointer routing.</param>
        /// <param name="target">Visual to hit-test (center unless <paramref name="localPoint"/> is set).</param>
        /// <param name="modifiers">Pointer modifiers for down/up.</param>
        /// <param name="localPoint">Optional point in <paramref name="target"/> coordinates.</param>
        public static void ClickAt(
            Window window,
            Visual target,
            RawInputModifiers modifiers = RawInputModifiers.None,
            Point? localPoint = null
        )
        {
            var local =
                localPoint ?? new Point(Math.Max(2, target.Bounds.Width / 2), Math.Max(2, target.Bounds.Height / 2));
            var windowPoint = target.TranslatePoint(local, window);
            Assert.True(windowPoint.HasValue);

            window.MouseMove(windowPoint.Value, modifiers);
            window.MouseDown(windowPoint.Value, MouseButton.Left, modifiers);
            window.MouseUp(windowPoint.Value, MouseButton.Left, modifiers);
            Dispatcher.UIThread.RunJobs();
        }

        /// <summary>
        /// Scrolls a ListBox row into view and left-clicks it through the host window.
        /// </summary>
        /// <param name="window">Host window for pointer routing.</param>
        /// <param name="list">ListBox hosting the row.</param>
        /// <param name="rowIndex">Zero-based row index.</param>
        /// <param name="preferredTarget">
        /// Optional descendant to click (e.g. label text); defaults to first non-empty TextBlock or the item.
        /// </param>
        public static void ClickListBoxRow(Window window, ListBox list, int rowIndex, Visual? preferredTarget = null)
        {
            list.ScrollIntoView(rowIndex);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var item = list.ContainerFromIndex(rowIndex) as ListBoxItem;
            Assert.NotNull(item);

            Visual target;
            if (preferredTarget is not null)
            {
                target = preferredTarget;
            }
            else
            {
                var labelText = item.GetVisualDescendants()
                    .OfType<TextBlock>()
                    .FirstOrDefault(text => !string.IsNullOrEmpty(text.Text));
                target = (Visual?)labelText ?? item;
            }

            ClickAt(window, target);
        }
    }
}
