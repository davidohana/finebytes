using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.FilterEditors.Formatting;
using Mfr.App.Ui.Views.FilterEditors.Formatting;
using Mfr.Filters.Formatting;
using Mfr.Tests.Ui.AppliedFilters;
using FormatEditorControl = Mfr.App.Ui.Views.FormatEditor.FormatEditor;

namespace Mfr.Tests.Ui.FilterEditors.Formatting
{
    /// <summary>
    /// Headless tests for <see cref="FormatterFilterEditorView"/>.
    /// </summary>
    public sealed class FormatterFilterEditorViewTests
    {
        /// <summary>
        /// Verifies Formatter template edits persist on the applied step via FormatEditor.
        /// </summary>
        [AvaloniaFact]
        public void Formatter_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("Formatter"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<FormatterFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<FormatterFilterEditorView>().Single();
            var templateEditor = editor.FindControl<FormatEditorControl>("TemplateEditor");
            Assert.NotNull(templateEditor);
            Assert.Equal(string.Empty, templateEditor.Text);

            templateEditor.Text = "<file-name>_<counter:initial=1,step=1>";
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (FormatterFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal("<file-name>_<counter:initial=1,step=1>", filter.Options.Template);

            window.Close();
        }

        /// <summary>
        /// Verifies FormatEditor insert updates the Formatter template binding.
        /// </summary>
        [AvaloniaFact]
        public void Formatter_FormatEditor_insert_updates_template()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("Formatter"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var editor = editorView.GetVisualDescendants().OfType<FormatterFilterEditorView>().Single();
            var templateEditor = editor.FindControl<FormatEditorControl>("TemplateEditor");
            Assert.NotNull(templateEditor);

            templateEditor.InsertTextAtCaret("<file-name>");
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (FormatterFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal("<file-name>", filter.Options.Template);
            Assert.Equal("<file-name>", templateEditor.Text);

            window.Close();
        }
    }
}
