using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.Views.AppliedFilters;
using Mfr.Filters.Space;

namespace Mfr.Tests.Ui.AppliedFilters
{
    /// <summary>
    /// Headless layout tests for the Filter Options dialog.
    /// </summary>
    public sealed class FilterOptionsDialogViewTests
    {
        /// <summary>
        /// Verifies substring rows show values, spinner buttons, and MFR7 "from the" copy.
        /// </summary>
        [AvaloniaFact]
        public void Substring_rows_show_values_and_from_the_copy()
        {
            var dialog = _Show(FilterApplyScopeMode.Substring);

            try
            {
                var start = dialog.FindControl<NumericUpDown>("SubstringStartSpinner");
                var end = dialog.FindControl<NumericUpDown>("SubstringEndSpinner");
                Assert.NotNull(start);
                Assert.NotNull(end);
                Assert.True(start.IsVisible);
                Assert.True(end.IsVisible);

                _AssertSpinnerShowsValue(start, expectedText: "1");
                _AssertSpinnerShowsValue(end, expectedText: "5");

                var fromTheBlocks = dialog
                    .GetVisualDescendants()
                    .OfType<TextBlock>()
                    .Where(block => block.IsVisible && block.Text == "from the")
                    .ToList();
                Assert.Equal(2, fromTheBlocks.Count);

                var suffixBlocks = dialog
                    .GetVisualDescendants()
                    .OfType<TextBlock>()
                    .Where(block => block.IsVisible && block.Text == "side (incl.)")
                    .ToList();
                Assert.Equal(2, suffixBlocks.Count);
                foreach (var suffix in suffixBlocks)
                {
                    var neededWidth = Math.Max(0, suffix.DesiredSize.Width - suffix.Margin.Left - suffix.Margin.Right);
                    Assert.True(
                        suffix.Bounds.Width + 0.5 >= neededWidth,
                        $"Suffix clipped: bounds={suffix.Bounds.Width}, needed={neededWidth}."
                    );
                }
            }
            finally
            {
                dialog.Close();
            }
        }

        /// <summary>
        /// Verifies the token-number spinner shows its value and stacked buttons.
        /// </summary>
        [AvaloniaFact]
        public void Token_number_spinner_shows_value()
        {
            var dialog = _Show(FilterApplyScopeMode.Token);

            try
            {
                var spinner = dialog.FindControl<NumericUpDown>("TokenNumberSpinner");
                Assert.NotNull(spinner);
                Assert.True(spinner.IsVisible);
                _AssertSpinnerShowsValue(spinner, expectedText: "1");
            }
            finally
            {
                dialog.Close();
            }
        }

        private static FilterOptionsDialog _Show(FilterApplyScopeMode scopeMode)
        {
            var step = new AppliedFilterStepViewModel("Fix Leading 0's", new ShrinkSpacesFilter());
            var viewModel = new FilterOptionsDialogViewModel(step) { ScopeMode = scopeMode };
            var dialog = new FilterOptionsDialog(viewModel);
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            return dialog;
        }

        private static void _AssertSpinnerShowsValue(NumericUpDown spinner, string expectedText)
        {
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
                    button.Bounds.Width >= 10 && button.Bounds.Width <= 20,
                    $"Spinner button width {button.Bounds.Width} should be a compact column."
                );
                Assert.True(
                    button.Bounds.Height >= 8 && button.Bounds.Height <= 16,
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
