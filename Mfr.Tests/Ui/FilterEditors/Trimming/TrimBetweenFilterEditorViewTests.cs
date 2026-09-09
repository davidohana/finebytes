using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors.Trimming;
using Mfr.App.Ui.Views.Controls;
using Mfr.App.Ui.Views.FilterEditors.Trimming;
using Mfr.Filters.Trimming;
using Mfr.Models.Rename;
using Mfr.Tests.Ui.AppliedFilters;

namespace Mfr.Tests.Ui.FilterEditors.Trimming
{
    /// <summary>
    /// Headless tests for <see cref="TrimBetweenFilterEditorView"/>.
    /// </summary>
    public sealed class TrimBetweenFilterEditorViewTests
    {
        /// <summary>
        /// Verifies Trim Between position/anchor edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Trim_between_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("TrimBetween"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<TrimBetweenFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<TrimBetweenFilterEditorView>().Single();
            var startSpinner = editor.FindControl<CompactNumericUpDown>("StartValueSpinner");
            var endSpinner = editor.FindControl<CompactNumericUpDown>("EndValueSpinner");
            var startAnchor = editor.FindControl<ComboBox>("StartAnchorCombo");
            var endAnchor = editor.FindControl<ComboBox>("EndAnchorCombo");
            Assert.NotNull(startSpinner);
            Assert.NotNull(endSpinner);
            Assert.NotNull(startAnchor);
            Assert.NotNull(endAnchor);
            Assert.Equal(2, startSpinner.Value);
            Assert.Equal(4, endSpinner.Value);
            Assert.Equal(Side.Left, startAnchor.SelectedItem);
            Assert.Equal(Side.Left, endAnchor.SelectedItem);

            startSpinner.Value = 13;
            endSpinner.Value = 5;
            endAnchor.SelectedItem = Side.Right;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (TrimBetweenFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(new Position(13, Side.Left), filter.Options.Start);
            Assert.Equal(new Position(5, Side.Right), filter.Options.End);

            window.Close();
        }

        /// <summary>
        /// Verifies Visual Trim Helper selection updates Trim Between options on the chain.
        /// </summary>
        [AvaloniaFact]
        public void Visual_trim_helper_selection_updates_trim_between_range()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("TrimBetween"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var optionsEditor = Assert.IsType<TrimBetweenFilterEditorViewModel>(
                mainViewModel.FilterEditorViewModel.OptionsEditor
            );
            optionsEditor.TrimHelper.SetSampleText("abcdef");
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var helperView = editorView.GetVisualDescendants().OfType<VisualTrimHelperView>().Single();
            var textBox = helperView.FindControl<TextBox>("TrimHelperText");
            Assert.NotNull(textBox);

            textBox.SelectionStart = 1;
            textBox.SelectionEnd = 4;
            _RaisePointerReleased(textBox);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (TrimBetweenFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(new Position(2, Side.Left), filter.Options.Start);
            Assert.Equal(new Position(4, Side.Left), filter.Options.End);
            Assert.Equal(1, textBox.SelectionStart);
            Assert.Equal(4, textBox.SelectionEnd);

            window.Close();
        }

        /// <summary>
        /// Verifies the helper TextBox selection is applied on first show when a sample is present.
        /// </summary>
        [AvaloniaFact]
        public void Visual_trim_helper_applies_selection_on_first_show()
        {
            var item = new RenameItem(
                new FileMeta(
                    renameListIndex: 0,
                    inFolderIndex: 0,
                    directoryPath: TestPaths.Absolute("album"),
                    prefix: "abcdef",
                    extension: ".txt"
                )
            );
            var step = new AppliedFilterStepViewModel("Trim Between", new TrimBetweenFilter());
            var editorVm = new TrimBetweenFilterEditorViewModel(step, sampleRenameItems: [item]);
            var editor = new TrimBetweenFilterEditorView { DataContext = editorVm };
            var window = new Window
            {
                Width = 480,
                Height = 320,
                Content = editor,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var textBox = editor
                .GetVisualDescendants()
                .OfType<VisualTrimHelperView>()
                .Single()
                .FindControl<TextBox>("TrimHelperText");
            Assert.NotNull(textBox);
            Assert.Equal("abcdef", textBox.Text);
            Assert.Equal(1, textBox.SelectionStart);
            Assert.Equal(4, textBox.SelectionEnd);

            window.Close();
        }

        private static void _RaisePointerReleased(Control control)
        {
            var pointer = new Pointer(1, PointerType.Mouse, true);
            var props = new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased);
            control.RaiseEvent(
                new PointerReleasedEventArgs(
                    control,
                    pointer,
                    control,
                    new Point(1, 1),
                    0,
                    props,
                    KeyModifiers.None,
                    MouseButton.Left
                )
                {
                    RoutedEvent = InputElement.PointerReleasedEvent,
                }
            );
        }
    }
}
