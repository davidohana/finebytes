using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Mfr.App.Ui.Views;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Headless layout tests for collapsing leading address-bar folders.
    /// </summary>
    public sealed class BreadcrumbTrailPanelTests
    {
        /// <summary>
        /// Verifies a wide trail keeps every folder when there is enough room.
        /// </summary>
        [AvaloniaFact]
        public void Wide_Panel_Shows_All_Segments()
        {
            var panel = _CreatePanel(width: 400, childWidths: [50, 50, 40]);
            _Show(panel);

            Assert.False(panel.HasOverflow);
            Assert.Equal(0, panel.VisibleStartIndex);
        }

        /// <summary>
        /// Verifies a narrow trail hides ancestors and keeps the current folder.
        /// </summary>
        [AvaloniaFact]
        public void Narrow_Panel_Keeps_Current_Folder()
        {
            var panel = _CreatePanel(width: 70, childWidths: [50, 50, 40]);
            _Show(panel);

            Assert.True(panel.HasOverflow);
            Assert.Equal(2, panel.VisibleStartIndex);
        }

        private static BreadcrumbTrailPanel _CreatePanel(double width, double[] childWidths)
        {
            var panel = new BreadcrumbTrailPanel
            {
                OverflowButtonWidth = 22,
                Width = width,
                Height = 24,
            };
            foreach (var childWidth in childWidths)
            {
                panel.Children.Add(new Border { Width = childWidth, Height = 16 });
            }

            return panel;
        }

        private static void _Show(BreadcrumbTrailPanel panel)
        {
            var window = new Window
            {
                Width = panel.Width,
                Height = 40,
                Content = panel,
            };
            window.Show();
            panel.UpdateLayout();
        }
    }
}
