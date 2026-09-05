using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.Views.Controls;

namespace Mfr.Tests.Ui.Controls
{
    /// <summary>
    /// Headless layout tests for <see cref="CompactRadioButton"/> and <see cref="CompactCheckBox"/>.
    /// </summary>
    public sealed class CompactToggleTests
    {
        /// <summary>
        /// Verifies compact radio label sits close to the glyph (not Fluent's 20px+8px gap).
        /// </summary>
        [AvaloniaFact]
        public void Radio_label_is_close_to_glyph()
        {
            var radio = new CompactRadioButton { Content = "Space" };
            var window = new Window
            {
                Width = 200,
                Height = 80,
                Content = radio,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            try
            {
                _AssertLabelCloseToGlyph(radio);
            }
            finally
            {
                window.Close();
            }
        }

        /// <summary>
        /// Verifies compact check box label sits close to the glyph.
        /// </summary>
        [AvaloniaFact]
        public void Check_label_is_close_to_glyph()
        {
            var checkBox = new CompactCheckBox { Content = "Spaces" };
            var window = new Window
            {
                Width = 200,
                Height = 80,
                Content = checkBox,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            try
            {
                _AssertLabelCloseToGlyph(checkBox);
            }
            finally
            {
                window.Close();
            }
        }

        private static void _AssertLabelCloseToGlyph(Control toggle)
        {
            var content = toggle
                .GetVisualDescendants()
                .OfType<ContentPresenter>()
                .First(presenter => presenter.Name == "PART_ContentPresenter");
            var glyphLeft = toggle
                .GetVisualDescendants()
                .OfType<Control>()
                .Where(control => control is Ellipse or Border { Name: "NormalRectangle" })
                .Select(control => control.Bounds.Right)
                .DefaultIfEmpty(0)
                .Max();

            Assert.True(glyphLeft > 0, "Expected a glyph bounds to measure.");
            var gap = content.Bounds.Left - glyphLeft;
            Assert.InRange(gap, 1, 6);
        }
    }
}
