using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.FilterEditors.Audio;
using Mfr.App.Ui.Views.Controls;
using Mfr.App.Ui.Views.FilterEditors.Audio;
using Mfr.Filters.Audio;
using Mfr.Tests.Ui.AppliedFilters;

namespace Mfr.Tests.Ui.FilterEditors.Audio
{
    /// <summary>
    /// Headless tests for <see cref="Id3v2FieldSetterFilterEditorView"/>.
    /// </summary>
    public sealed class Id3v2FieldSetterFilterEditorViewTests
    {
        /// <summary>
        /// Verifies ID3v2 Field Setter option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Id3v2_field_setter_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("Id3v2FieldSetter"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<Id3v2FieldSetterFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);
            var editorVm = (Id3v2FieldSetterFilterEditorViewModel)mainViewModel.FilterEditorViewModel.OptionsEditor;

            var editor = editorView.GetVisualDescendants().OfType<Id3v2FieldSetterFilterEditorView>().Single();
            var frameCombo = editor.FindControl<ComboBox>("FrameCombo");
            var textBox = editor.FindControl<TextBox>("TextBox");
            var onlyIfEmpty = editor.FindControl<CompactCheckBox>("OnlyIfEmptyCheckBox");
            var languageRow = editor.FindControl<FilterEditorLabeledRow>("LanguageRow");
            var descriptionRow = editor.FindControl<FilterEditorLabeledRow>("DescriptionRow");
            var languageBox = editor.FindControl<TextBox>("LanguageBox");
            var descriptionBox = editor.FindControl<TextBox>("DescriptionBox");
            Assert.NotNull(frameCombo);
            Assert.NotNull(textBox);
            Assert.NotNull(onlyIfEmpty);
            Assert.NotNull(languageRow);
            Assert.NotNull(descriptionRow);
            Assert.NotNull(languageBox);
            Assert.NotNull(descriptionBox);
            Assert.Equal("TIT2", editorVm.SelectedFrame.FrameId);
            Assert.Equal(string.Empty, textBox.Text);
            Assert.False(onlyIfEmpty.IsChecked);
            Assert.False(languageRow.IsVisible);
            Assert.False(descriptionRow.IsVisible);

            frameCombo.SelectedItem = editorVm.Frames.Single(c => c.FrameId == "COMM");
            textBox.Text = "Hi";
            onlyIfEmpty.IsChecked = true;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.True(languageRow.IsVisible);
            Assert.True(descriptionRow.IsVisible);
            languageBox.Text = "eng";
            descriptionBox.Text = "note";
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (Id3v2FieldSetterFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal("COMM", filter.Options.FrameId);
            Assert.Equal("Hi", filter.Options.Text);
            Assert.True(filter.Options.OnlyIfEmpty);
            Assert.Equal("eng", filter.Options.Language);
            Assert.Equal("note", filter.Options.Description);

            frameCombo.SelectedItem = editorVm.Frames.Single(c => c.FrameId == "TALB");
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            filter = (Id3v2FieldSetterFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal("TALB", filter.Options.FrameId);
            Assert.Null(filter.Options.Language);
            Assert.Null(filter.Options.Description);
            Assert.False(languageRow.IsVisible);
            Assert.False(descriptionRow.IsVisible);

            window.Close();
        }
    }
}
