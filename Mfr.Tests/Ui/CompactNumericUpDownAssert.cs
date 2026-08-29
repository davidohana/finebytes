using Avalonia.Controls;
using Avalonia.VisualTree;
using Mfr.App.Ui.Views.Controls;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Layout checks for <see cref="CompactNumericUpDown"/>.
    /// </summary>
    internal static class CompactNumericUpDownAssert
    {
        /// <summary>
        /// Asserts the value is visible and the spinner arrows are a compact stacked pair.
        /// </summary>
        /// <param name="spinner">Control under test.</param>
        /// <param name="expectedText">Formatted value shown in the inner text box.</param>
        public static void ShowsStackedValue(CompactNumericUpDown spinner, string expectedText)
        {
            ArgumentNullException.ThrowIfNull(spinner);

            Assert.True(
                spinner.Bounds.Width >= 72,
                $"Spinner width {spinner.Bounds.Width} is too narrow for the value and buttons."
            );

            var textBox = spinner.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();
            Assert.NotNull(textBox);
            Assert.Equal(expectedText, textBox.Text);
            Assert.True(
                textBox.Bounds.Width >= 24,
                $"Spinner text area width {textBox.Bounds.Width} cannot show the value."
            );

            var buttons = spinner
                .GetVisualDescendants()
                .OfType<RepeatButton>()
                .OrderBy(button => button.Bounds.Y)
                .ThenBy(button => button.Bounds.X)
                .ToList();
            Assert.Equal(2, buttons.Count);
            foreach (var button in buttons)
            {
                Assert.True(
                    button.Bounds.Width is >= 10 and <= 20,
                    $"Spinner button width {button.Bounds.Width} should be a compact column."
                );
                Assert.True(
                    button.Bounds.Height is >= 8 and <= 16,
                    $"Spinner button height {button.Bounds.Height} should be a stacked half of the field."
                );
            }

            Assert.True(
                Math.Abs(buttons[0].Bounds.X - buttons[1].Bounds.X) <= 1,
                $"Spinner buttons should share a column: {buttons[0].Bounds.X} vs {buttons[1].Bounds.X}."
            );
            Assert.True(
                buttons[0].Bounds.Bottom <= buttons[1].Bounds.Y + 1,
                $"Spinner buttons should stack vertically: up bottom {buttons[0].Bounds.Bottom}, down top {buttons[1].Bounds.Y}."
            );
        }
    }
}
