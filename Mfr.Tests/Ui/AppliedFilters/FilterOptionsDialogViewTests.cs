using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.Views.AppliedFilters;
using Mfr.App.Ui.Views.Controls;
using Mfr.Filters.Space;
using Mfr.Tests.Ui;

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
                var start = dialog.FindControl<CompactNumericUpDown>("SubstringStartSpinner");
                var end = dialog.FindControl<CompactNumericUpDown>("SubstringEndSpinner");
                Assert.NotNull(start);
                Assert.NotNull(end);
                Assert.True(start.IsVisible);
                Assert.True(end.IsVisible);

                CompactNumericUpDownAssert.ShowsStackedValue(start, expectedText: "1");
                CompactNumericUpDownAssert.ShowsStackedValue(end, expectedText: "5");

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
        /// Verifies the token-number spinner, left-aligned labels, and aligned fields.
        /// </summary>
        [AvaloniaFact]
        public void Token_number_spinner_shows_value()
        {
            var dialog = _Show(FilterApplyScopeMode.Token);

            try
            {
                var spinner = dialog.FindControl<CompactNumericUpDown>("TokenNumberSpinner");
                var separatorBox = dialog.FindControl<TextBox>("TokenSeparatorBox");
                Assert.NotNull(spinner);
                Assert.NotNull(separatorBox);
                Assert.True(spinner.IsVisible);
                CompactNumericUpDownAssert.ShowsStackedValue(spinner, expectedText: "1");

                var title = dialog
                    .GetVisualDescendants()
                    .OfType<TextBlock>()
                    .Single(block =>
                        block.IsVisible && block.Text == "Token" && block.FontWeight == FontWeight.SemiBold
                    );
                var separatorLabel = dialog
                    .GetVisualDescendants()
                    .OfType<TextBlock>()
                    .Single(block => block.IsVisible && block.Text == "Separator:");
                var tokenNumberLabel = dialog
                    .GetVisualDescendants()
                    .OfType<TextBlock>()
                    .Single(block => block.IsVisible && block.Text == "Token number:");

                Assert.Equal(TextAlignment.Left, separatorLabel.TextAlignment);
                Assert.Equal(TextAlignment.Left, tokenNumberLabel.TextAlignment);

                var titleX = title.TranslatePoint(new Point(), dialog)!.Value.X;
                var separatorLabelX = separatorLabel.TranslatePoint(new Point(), dialog)!.Value.X;
                var tokenNumberLabelX = tokenNumberLabel.TranslatePoint(new Point(), dialog)!.Value.X;
                Assert.True(Math.Abs(titleX - separatorLabelX) <= 1);
                Assert.True(Math.Abs(titleX - tokenNumberLabelX) <= 1);

                var separatorBoxX = separatorBox.TranslatePoint(new Point(), dialog)!.Value.X;
                var spinnerX = spinner.TranslatePoint(new Point(), dialog)!.Value.X;
                Assert.True(Math.Abs(separatorBoxX - spinnerX) <= 1);
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
    }
}
