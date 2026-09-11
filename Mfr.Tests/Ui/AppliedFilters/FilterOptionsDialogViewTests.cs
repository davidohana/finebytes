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
        /// Verifies Name is focused and selected when the dialog opens (MFR7 parity).
        /// </summary>
        [AvaloniaFact]
        public void Name_box_is_focused_and_selected_on_open()
        {
            var dialog = _Show(FilterApplyScopeMode.Whole);

            try
            {
                var nameBox = dialog.FindControl<TextBox>("NameBox");
                Assert.NotNull(nameBox);
                Assert.True(nameBox.IsFocused);
                Assert.Equal(0, nameBox.SelectionStart);
                Assert.Equal(nameBox.Text?.Length ?? 0, nameBox.SelectionEnd);
            }
            finally
            {
                dialog.Close();
            }
        }

        /// <summary>
        /// Verifies outer labeled rows share one label column width via <c>FilterEditorLabel</c>.
        /// </summary>
        [AvaloniaFact]
        public void Outer_labeled_rows_share_label_column_width()
        {
            var dialog = _Show(FilterApplyScopeMode.Whole);

            try
            {
                var rows = dialog.GetVisualDescendants().OfType<FilterEditorLabeledRow>().ToList();
                var nameRow = rows.Single(row => row.Label == "Name:");
                var applyToRow = rows.Single(row => row.Label == "Apply To:");
                var nameLabel = _LabelText(nameRow);
                var applyToLabel = _LabelText(applyToRow);

                Assert.True(nameLabel.Bounds.Width > 1 && applyToLabel.Bounds.Width > 1);
                Assert.Equal(applyToLabel.Bounds.Width, nameLabel.Bounds.Width, precision: 0);
            }
            finally
            {
                dialog.Close();
            }
        }

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
                    .Single(block => block.IsVisible && block.Text == "Token separator string:");
                var tokenNumberLabel = dialog
                    .GetVisualDescendants()
                    .OfType<TextBlock>()
                    .Single(block => block.IsVisible && block.Text == "Token number:");

                Assert.True(separatorLabel.TextAlignment is TextAlignment.Left or TextAlignment.Start);
                Assert.True(tokenNumberLabel.TextAlignment is TextAlignment.Left or TextAlignment.Start);

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
                var levelRow = dialog.FindControl<FilterEditorLabeledRow>("AncestorFolderLevelRow");
                var id3v2Panel = dialog.FindControl<StackPanel>("Id3v2MultiInstanceFieldsPanel");
                var levelSpinner = dialog.FindControl<CompactNumericUpDown>("AncestorFolderLevelSpinner");
                Assert.NotNull(levelRow);
                Assert.NotNull(id3v2Panel);
                Assert.NotNull(levelSpinner);
                Assert.True(levelRow.IsVisible);
                Assert.True(levelSpinner.IsVisible);
                Assert.False(id3v2Panel.IsVisible);

                var id3v2Group = FilterTargetCatalog.Groups.First(group => group.Label == "ID3v2");
                var titleOption = id3v2Group.Targets.First(option =>
                    option.Prototype is Id3v2FrameTarget frame && frame.FrameId == "TIT2"
                );
                viewModel.SelectedTargetGroup = id3v2Group;
                viewModel.SelectedTargetOption = titleOption;
                dialog.UpdateLayout();
                Dispatcher.UIThread.RunJobs();

                Assert.False(levelRow.IsVisible);
                Assert.False(id3v2Panel.IsVisible);

                var commentOption = id3v2Group.Targets.First(option =>
                    option.Prototype is Id3v2FrameTarget frame && frame.FrameId == "COMM"
                );
                viewModel.SelectedTargetOption = commentOption;
                dialog.UpdateLayout();
                Dispatcher.UIThread.RunJobs();

                Assert.False(levelRow.IsVisible);
                Assert.True(id3v2Panel.IsVisible);
                Assert.True(viewModel.HasId3v2Language);

                var customOption = id3v2Group.Targets.First(option =>
                    option.Prototype is Id3v2FrameTarget frame && frame.FrameId == "TXXX"
                );
                viewModel.SelectedTargetOption = customOption;
                dialog.UpdateLayout();
                Dispatcher.UIThread.RunJobs();

                Assert.True(id3v2Panel.IsVisible);
                Assert.False(viewModel.HasId3v2Language);
                var languageRow = dialog.FindControl<FilterEditorLabeledRow>("Id3v2LanguageRow");
                Assert.NotNull(languageRow);
                Assert.False(languageRow.IsVisible);
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
                var id3v2Panel = dialog.FindControl<StackPanel>("Id3v2MultiInstanceFieldsPanel");
                Assert.NotNull(id3v2Panel);
                Assert.True(id3v2Panel.IsVisible);
                Assert.True(viewModel.HasId3v2Language);
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

        /// <summary>
        /// Verifies Apply-on radio selection updates <see cref="FilterOptionsDialogViewModel.ScopeMode"/>.
        /// </summary>
        [AvaloniaFact]
        public void Apply_on_radio_updates_scope_mode()
        {
            var dialog = _Show(FilterApplyScopeMode.Whole);

            try
            {
                var viewModel = Assert.IsType<FilterOptionsDialogViewModel>(dialog.DataContext);
                Assert.Equal(FilterApplyScopeMode.Whole, viewModel.ScopeMode);
                Assert.False(viewModel.ShowSubstringOptions);
                Assert.False(viewModel.ShowTokenOptions);

                var substringRadio = dialog.FindControl<RadioButton>("SubstringScopeRadio");
                Assert.NotNull(substringRadio);
                substringRadio.IsChecked = true;
                dialog.UpdateLayout();
                Dispatcher.UIThread.RunJobs();

                Assert.Equal(FilterApplyScopeMode.Substring, viewModel.ScopeMode);
                Assert.True(viewModel.ShowSubstringOptions);
                Assert.False(viewModel.ShowTokenOptions);
                Assert.True(substringRadio.IsChecked);

                var tokenRadio = dialog.FindControl<RadioButton>("TokenScopeRadio");
                Assert.NotNull(tokenRadio);
                tokenRadio.IsChecked = true;
                dialog.UpdateLayout();
                Dispatcher.UIThread.RunJobs();

                Assert.Equal(FilterApplyScopeMode.Token, viewModel.ScopeMode);
                Assert.False(viewModel.ShowSubstringOptions);
                Assert.True(viewModel.ShowTokenOptions);
                Assert.True(tokenRadio.IsChecked);
                Assert.False(substringRadio.IsChecked);
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
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            return dialog;
        }

        private static TextBlock _LabelText(FilterEditorLabeledRow row)
        {
            var label = row.GetVisualDescendants()
                .OfType<TextBlock>()
                .FirstOrDefault(block => block.Classes.Contains("filter-editor-label"));
            Assert.NotNull(label);
            return label;
        }

        /// <summary>
        /// Verifies height is content-sized and locked (horizontal-only resize), and grows when
        /// Apply-on switches to Substring.
        /// </summary>
        [AvaloniaFact]
        public void Dialog_HorizontalResizeOnly_RelocksWhenScopeGrows()
        {
            var dialog = _Show(FilterApplyScopeMode.Whole);
            try
            {
                Assert.Equal(dialog.MinHeight, dialog.MaxHeight);
                Assert.True(dialog.MinHeight > 0);
                var wholeHeight = dialog.Bounds.Height;

                var ok = dialog.FindControl<Button>("OkButton");
                Assert.NotNull(ok);
                var topLeft = ok.TranslatePoint(default, dialog);
                Assert.NotNull(topLeft);
                Assert.True(
                    topLeft.Value.Y + ok.Bounds.Height <= dialog.Bounds.Height + 0.5,
                    "OK button should stay fully visible."
                );

                var substringRadio = dialog.FindControl<RadioButton>("SubstringScopeRadio");
                Assert.NotNull(substringRadio);
                substringRadio.IsChecked = true;
                dialog.UpdateLayout();
                Dispatcher.UIThread.RunJobs();
                dialog.UpdateLayout();
                Dispatcher.UIThread.RunJobs();

                Assert.Equal(dialog.MinHeight, dialog.MaxHeight);
                Assert.True(
                    dialog.Bounds.Height > wholeHeight,
                    $"Expected substring height {dialog.Bounds.Height} > whole {wholeHeight}."
                );
            }
            finally
            {
                dialog.Close();
            }
        }
    }
}
