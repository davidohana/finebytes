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
    /// Headless tests for <see cref="AudioTagSetterFilterEditorView"/>.
    /// </summary>
    public sealed class AudioTagSetterFilterEditorViewTests
    {
        /// <summary>
        /// Verifies Audio Tag Setter option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Audio_tag_setter_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("AudioTagSetter"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<AudioTagSetterFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);
            var editor = editorView.GetVisualDescendants().OfType<AudioTagSetterFilterEditorView>().Single();

            var titleCheck = _FindFieldCheckBox(editor, AudioTagSetterFieldKind.Title);
            var titleBox = _FindVisibleFieldTextBox(editor, AudioTagSetterFieldKind.Title);
            Assert.False(titleCheck.IsChecked);
            Assert.Equal(string.Empty, titleBox.Text);

            titleBox.Text = "<file-name>";
            titleCheck.IsChecked = true;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (AudioTagSetterFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.NotNull(filter.Options.Title);
            Assert.Equal("<file-name>", filter.Options.Title.Text);
            Assert.False(filter.Options.Title.OnlyIfEmpty);

            titleCheck.IsChecked = null;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            filter = (AudioTagSetterFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.NotNull(filter.Options.Title);
            Assert.True(filter.Options.Title.OnlyIfEmpty);

            var trackCheck = _FindFieldCheckBox(editor, AudioTagSetterFieldKind.Track);
            var trackBox = _FindVisibleFieldTextBox(editor, AudioTagSetterFieldKind.Track);
            var autoInc = editor
                .GetVisualDescendants()
                .OfType<CompactCheckBox>()
                .Single(box =>
                    box.IsVisible && box.Tag as string == AudioTagSetterFieldRowViewModel.AutoIncrementTag
                );
            Assert.True(autoInc.IsChecked);

            trackBox.Text = "1";
            trackCheck.IsChecked = true;
            autoInc.IsChecked = false;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            filter = (AudioTagSetterFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.NotNull(filter.Options.Track);
            Assert.Equal("1", filter.Options.Track.Text);
            Assert.False(filter.Options.TrackAutoIncrement);

            window.Close();
        }

        private static CompactCheckBox _FindFieldCheckBox(
            AudioTagSetterFilterEditorView editor,
            AudioTagSetterFieldKind kind
        )
        {
            return editor
                .GetVisualDescendants()
                .OfType<CompactCheckBox>()
                .Single(box => box.Tag is AudioTagSetterFieldKind tagged && tagged == kind);
        }

        private static TextBox _FindVisibleFieldTextBox(
            AudioTagSetterFilterEditorView editor,
            AudioTagSetterFieldKind kind
        )
        {
            return editor
                .GetVisualDescendants()
                .OfType<TextBox>()
                .Single(box => box.IsVisible && box.Tag is AudioTagSetterFieldKind tagged && tagged == kind);
        }
    }
}
