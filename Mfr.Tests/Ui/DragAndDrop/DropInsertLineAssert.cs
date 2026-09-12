using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.VisualTree;
using Mfr.App.Ui.Views.DragAndDrop;

namespace Mfr.Tests.Ui.DragAndDrop
{
    /// <summary>
    /// Assertions for the shared salmon insert-line adorner.
    /// </summary>
    internal static class DropInsertLineAssert
    {
        /// <summary>
        /// Asserts <paramref name="host"/> shows a 3px insert-line adorner.
        /// </summary>
        /// <param name="host">List or grid that hosts the adorner.</param>
        public static void IsVisible(Control host)
        {
            var adorner = AdornerLayer.GetAdorner(host);
            Assert.NotNull(adorner);

            var line = adorner.GetVisualDescendants().OfType<Rectangle>().FirstOrDefault();
            Assert.NotNull(line);
            Assert.Equal(DropInsertLineAdorner.Thickness, line.Height);
        }

        /// <summary>
        /// Asserts <paramref name="host"/> has no insert-line adorner.
        /// </summary>
        /// <param name="host">List or grid that previously hosted the adorner.</param>
        public static void IsCleared(Control host)
        {
            Assert.Null(AdornerLayer.GetAdorner(host));
        }
    }
}
