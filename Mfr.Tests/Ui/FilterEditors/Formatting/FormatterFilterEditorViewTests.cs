using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.FilterEditors.Formatting;
using Mfr.App.Ui.Views.FilterEditors.Formatting;
using Mfr.Filters.Formatting;
using Mfr.Tests.Ui.AppliedFilters;

namespace Mfr.Tests.Ui.FilterEditors.Formatting
{
    /// <summary>
    /// Headless tests for <see cref="FormatterFilterEditorView"/>.
    /// </summary>
    public sealed class FormatterFilterEditorViewTests
    {
        /// <summary>
        /// Verifies Formatter template edits persist on the applied step.
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
            var template = editor.FindControl<TextBox>("TemplateBox");
            Assert.NotNull(template);
            Assert.Equal(string.Empty, template.Text);

            template.Text = "<file-name>_<counter:initial=1,step=1>";
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (FormatterFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal("<file-name>_<counter:initial=1,step=1>", filter.Options.Template);

            window.Close();
        }
    }
}
