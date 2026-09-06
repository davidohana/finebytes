using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.FilterEditors.Audio;
using Mfr.App.Ui.Views.Controls;
using Mfr.App.Ui.Views.FilterEditors.Audio;
using Mfr.Filters.Audio;
using Mfr.Tests.Ui.AppliedFilters;
using FormatEditorControl = Mfr.App.Ui.Views.FormatEditor.FormatEditor;

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
            var titleEditor = _FindVisibleFieldFormatEditor(editor, AudioTagSetterFieldKind.Title);
            Assert.False(titleCheck.IsChecked);
            Assert.Equal(string.Empty, titleEditor.Text);

            titleEditor.Text = "<file-name>";
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.True(titleCheck.IsChecked);
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
            var trackEditor = _FindVisibleFieldFormatEditor(editor, AudioTagSetterFieldKind.Track);
            var autoInc = editor
                .GetVisualDescendants()
                .OfType<CompactCheckBox>()
                .Single(box =>
                    box.IsVisible && (box.Tag as string) == AudioTagSetterFieldRowViewModel.AutoIncrementTag
                );
            Assert.True(autoInc.IsChecked);

            trackEditor.Text = "1";
            autoInc.IsChecked = false;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.True(trackCheck.IsChecked);
            filter = (AudioTagSetterFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.NotNull(filter.Options.Track);
            Assert.Equal("1", filter.Options.Track.Text);
            Assert.False(filter.Options.TrackAutoIncrement);

            var genreCheck = _FindFieldCheckBox(editor, AudioTagSetterFieldKind.Genre);
            var genreCombo = _FindVisibleFieldComboBox(editor, AudioTagSetterFieldKind.Genre);
            genreCombo.Text = "Jazz";
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.True(genreCheck.IsChecked);
            filter = (AudioTagSetterFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.NotNull(filter.Options.Genre);
            Assert.Equal("Jazz", filter.Options.Genre.Text);

            window.Close();
        }

        /// <summary>
        /// Verifies value controls share a left edge across short and long field labels.
        /// </summary>
        [AvaloniaFact]
        public void Audio_tag_setter_value_fields_align_vertically()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("AudioTagSetter"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var editor = editorView.GetVisualDescendants().OfType<AudioTagSetterFilterEditorView>().Single();
            var performersEditor = _FindVisibleFieldFormatEditor(editor, AudioTagSetterFieldKind.Performers);
            var titleEditor = _FindVisibleFieldFormatEditor(editor, AudioTagSetterFieldKind.Title);
            var albumArtistsEditor = _FindVisibleFieldFormatEditor(editor, AudioTagSetterFieldKind.AlbumArtists);
            var genreCombo = _FindVisibleFieldComboBox(editor, AudioTagSetterFieldKind.Genre);
            var lyricsCheck = _FindFieldCheckBox(editor, AudioTagSetterFieldKind.Lyrics);
            var lyricsEditor = _FindVisibleFieldFormatEditor(editor, AudioTagSetterFieldKind.Lyrics);

            Assert.True(performersEditor.Bounds.Width > 1 && titleEditor.Bounds.Width > 1);
            Assert.Equal(_LeftInEditor(editor, performersEditor), _LeftInEditor(editor, titleEditor), precision: 0);
            Assert.Equal(_LeftInEditor(editor, albumArtistsEditor), _LeftInEditor(editor, genreCombo), precision: 0);
            Assert.True(_LeftInEditor(editor, albumArtistsEditor) > _LeftInEditor(editor, performersEditor));
            Assert.Equal(VerticalAlignment.Center, lyricsCheck.VerticalAlignment);
            Assert.True(lyricsEditor.Bounds.Width > performersEditor.Bounds.Width);

            window.Close();
        }

        /// <summary>
        /// Verifies clicking a field label toggles that row’s three-state checkbox.
        /// </summary>
        [AvaloniaFact]
        public void Audio_tag_setter_label_click_toggles_field_checkbox()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("AudioTagSetter"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var editor = editorView.GetVisualDescendants().OfType<AudioTagSetterFilterEditorView>().Single();
            var titleCheck = _FindFieldCheckBox(editor, AudioTagSetterFieldKind.Title);
            Assert.False(titleCheck.IsChecked);

            var label = titleCheck
                .GetVisualDescendants()
                .OfType<TextBlock>()
                .Single(block => block.Text == "Set title:");
            var local = new Point(Math.Max(2, label.Bounds.Width / 2), Math.Max(2, label.Bounds.Height / 2));
            var windowPoint = label.TranslatePoint(local, window);
            Assert.True(windowPoint.HasValue);

            window.MouseMove(windowPoint.Value);
            window.MouseDown(windowPoint.Value, MouseButton.Left);
            window.MouseUp(windowPoint.Value, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();

            Assert.True(titleCheck.IsChecked);
            window.Close();
        }

        private static double _LeftInEditor(AudioTagSetterFilterEditorView editor, Control control)
        {
            var point = control.TranslatePoint(new Point(0, 0), editor);
            Assert.NotNull(point);
            return point.Value.X;
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

        private static FormatEditorControl _FindVisibleFieldFormatEditor(
            AudioTagSetterFilterEditorView editor,
            AudioTagSetterFieldKind kind
        )
        {
            return editor
                .GetVisualDescendants()
                .OfType<FormatEditorControl>()
                .Single(box => box.IsVisible && box.Tag is AudioTagSetterFieldKind tagged && tagged == kind);
        }

        private static ComboBox _FindVisibleFieldComboBox(
            AudioTagSetterFilterEditorView editor,
            AudioTagSetterFieldKind kind
        )
        {
            return editor
                .GetVisualDescendants()
                .OfType<ComboBox>()
                .Single(box => box.IsVisible && box.Tag is AudioTagSetterFieldKind tagged && tagged == kind);
        }
    }
}
