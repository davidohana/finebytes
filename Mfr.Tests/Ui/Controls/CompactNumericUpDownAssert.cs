using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Media;
using Avalonia.VisualTree;
using Mfr.App.Ui.Views.Controls;

namespace Mfr.Tests.Ui.Controls
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

            var buttons = _StackedButtons(spinner);
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

        /// <summary>
        /// Asserts the decrease arrow has no disabled fill when the value is at minimum.
        /// </summary>
        /// <param name="spinner">Control under test, with value at minimum.</param>
        public static void DecreaseButtonHasTransparentFill(CompactNumericUpDown spinner)
        {
            ArgumentNullException.ThrowIfNull(spinner);

            var decrease = _StackedButtons(spinner)[1];
            Assert.False(decrease.IsEnabled);
            Assert.Equal(1d, decrease.Opacity);

            var presenter = decrease
                .GetVisualDescendants()
                .OfType<ContentPresenter>()
                .FirstOrDefault(part => part.Name == "PART_ContentPresenter");
            Assert.NotNull(presenter);

            var isTransparent =
                presenter.Background is null || (presenter.Background is ISolidColorBrush brush && brush.Color.A == 0);
            Assert.True(
                isTransparent,
                $"Decrease button at minimum should not have a gray fill; was {presenter.Background}."
            );
        }

        private static List<RepeatButton> _StackedButtons(CompactNumericUpDown spinner)
        {
            return
            [
                .. spinner
                    .GetVisualDescendants()
                    .OfType<RepeatButton>()
                    .OrderBy(button => button.Bounds.Y)
                    .ThenBy(button => button.Bounds.X),
            ];
        }
    }
}
