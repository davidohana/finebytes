using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.FilterEditors.Formatting;
using Mfr.App.Ui.Views.Controls;
using Mfr.App.Ui.Views.FilterEditors.Formatting;
using Mfr.Filters.Formatting;
using Mfr.Tests.Ui.AppliedFilters;

namespace Mfr.Tests.Ui.FilterEditors.Formatting
{
    /// <summary>
    /// Headless tests for <see cref="InserterFilterEditorView"/>.
    /// </summary>
    public sealed class InserterFilterEditorViewTests
    {
        /// <summary>
        /// Verifies Inserter option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Inserter_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("Inserter"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<InserterFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<InserterFilterEditorView>().Single();
            var insertText = editor.FindControl<TextBox>("InsertTextBox");
            var position = editor.FindControl<CompactNumericUpDown>("PositionSpinner");
            var endRadio = editor.FindControl<RadioButton>("EndRadio");
            var overwrite = editor.FindControl<CompactCheckBox>("OverwriteCheckBox");
            Assert.NotNull(insertText);
            Assert.NotNull(position);
            Assert.NotNull(endRadio);
            Assert.NotNull(overwrite);
            Assert.Equal(string.Empty, insertText.Text);
            Assert.Equal(1, position.Value);
            Assert.False(overwrite.IsChecked);

            insertText.Text = "_-";
            position.Value = 3;
            endRadio.IsChecked = true;
            overwrite.IsChecked = true;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (InserterFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal("_-", filter.Options.Text);
            Assert.Equal(3, filter.Options.Position);
            Assert.Equal(InserterOrigin.End, filter.Options.StartFrom);
            Assert.True(filter.Options.Overwrite);

            window.Close();
        }
    }
}
