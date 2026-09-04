using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.Views.AppliedFilters;
using Mfr.App.Ui.Views.Controls;
using Mfr.Filters.Formatting;
using Mfr.Filters.Space;
using Mfr.Tests.Ui.Controls;

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
                var fieldset = dialog
                    .GetVisualDescendants()
                    .OfType<FieldsetGroup>()
                    .Single(group => group.IsVisible && Equals(group.Header, "Substring"));
                Assert.NotNull(fieldset);

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

                var fieldset = dialog
                    .GetVisualDescendants()
                    .OfType<FieldsetGroup>()
                    .Single(group => group.IsVisible && Equals(group.Header, "Token"));
                Assert.NotNull(fieldset);

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

                var separatorLabelX = separatorLabel.TranslatePoint(new Point(), dialog)!.Value.X;
                var tokenNumberLabelX = tokenNumberLabel.TranslatePoint(new Point(), dialog)!.Value.X;
                Assert.True(Math.Abs(separatorLabelX - tokenNumberLabelX) <= 1);

                var separatorBoxX = separatorBox.TranslatePoint(new Point(), dialog)!.Value.X;
                var spinnerX = spinner.TranslatePoint(new Point(), dialog)!.Value.X;
                Assert.True(Math.Abs(separatorBoxX - spinnerX) <= 1);
            }
            finally
            {
                dialog.Close();
            }
        }

        /// <summary>
        /// Verifies ancestor level is visible and ID3v2 multi-instance fields hide for singleton frames.
        /// </summary>
        [AvaloniaFact]
        public void Target_parameter_rows_follow_selected_apply_to()
        {
            var step = new AppliedFilterStepViewModel("Fix Leading 0's", new ShrinkSpacesFilter());
            var viewModel = new FilterOptionsDialogViewModel(step);
            var pathGroup = FilterTargetCatalog.Groups.First(group => group.Label == "Path");
            var ancestorOption = pathGroup.Targets.First(option => option.Prototype is AncestorFolderTarget);
            viewModel.SelectedTargetGroup = pathGroup;
            viewModel.SelectedTargetOption = ancestorOption;

            var dialog = new FilterOptionsDialog(viewModel);
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            try
            {
                var levelGrid = dialog.FindControl<Grid>("AncestorFolderLevelGrid");
                var id3v2Grid = dialog.FindControl<Grid>("Id3v2MultiInstanceFieldsGrid");
                var levelSpinner = dialog.FindControl<CompactNumericUpDown>("AncestorFolderLevelSpinner");
                Assert.NotNull(levelGrid);
                Assert.NotNull(id3v2Grid);
                Assert.NotNull(levelSpinner);
                Assert.True(levelGrid.IsVisible);
                Assert.True(levelSpinner.IsVisible);
                Assert.False(id3v2Grid.IsVisible);

                var id3v2Group = FilterTargetCatalog.Groups.First(group => group.Label == "ID3v2");
                var titleOption = id3v2Group.Targets.First(option =>
                    option.Prototype is Id3v2FrameTarget frame && frame.FrameId == "TIT2"
                );
                viewModel.SelectedTargetGroup = id3v2Group;
                viewModel.SelectedTargetOption = titleOption;
                dialog.UpdateLayout();
                Dispatcher.UIThread.RunJobs();

                Assert.False(levelGrid.IsVisible);
                Assert.False(id3v2Grid.IsVisible);

                var commentOption = id3v2Group.Targets.First(option =>
                    option.Prototype is Id3v2FrameTarget frame && frame.FrameId == "COMM"
                );
                viewModel.SelectedTargetOption = commentOption;
                dialog.UpdateLayout();
                Dispatcher.UIThread.RunJobs();

                Assert.False(levelGrid.IsVisible);
                Assert.True(id3v2Grid.IsVisible);
            }
            finally
            {
                dialog.Close();
            }
        }

        /// <summary>
        /// Verifies loading an ID3v2 multi-instance target shows language and description fields.
        /// </summary>
        [AvaloniaFact]
        public void Id3v2_multi_instance_fields_load_from_filter()
        {
            var filter = new FormatterFilter(new Id3v2FrameTarget("COMM", "eng", "Primary"), new FormatterOptions("x"));
            var step = new AppliedFilterStepViewModel("Formatter", filter);
            var viewModel = new FilterOptionsDialogViewModel(step);
            var dialog = new FilterOptionsDialog(viewModel);
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            try
            {
                var id3v2Grid = dialog.FindControl<Grid>("Id3v2MultiInstanceFieldsGrid");
                Assert.NotNull(id3v2Grid);
                Assert.True(id3v2Grid.IsVisible);
                Assert.Equal("eng", viewModel.Id3v2Language);
                Assert.Equal("Primary", viewModel.Id3v2Description);
            }
            finally
            {
                dialog.Close();
            }
        }

        /// <summary>
        /// Verifies Apply-on radios keep space between options (CompactRadioButton StyleKey is RadioButton).
        /// </summary>
        [AvaloniaFact]
        public void Apply_on_radios_keep_space_between_options()
        {
            var dialog = _Show(FilterApplyScopeMode.Whole);

            try
            {
                var radios = dialog
                    .GetVisualDescendants()
                    .OfType<CompactRadioButton>()
                    .Where(radio => radio.Classes.Contains("filter-options-radio"))
                    .ToList();
                Assert.Equal(3, radios.Count);

                for (var i = 0; i < radios.Count - 1; i++)
                {
                    var gap = radios[i + 1].Bounds.Left - radios[i].Bounds.Right;
                    Assert.True(gap >= 12, $"Expected gap between Apply-on radios, got {gap}.");
                }
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
