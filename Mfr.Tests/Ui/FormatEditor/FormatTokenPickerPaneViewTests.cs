using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using Mfr.App.Ui.Views.FormatEditor;
using Mfr.Filters.Formatting.FormatString;
using Mfr.Tests.Ui.AppliedFilters;
using Mfr.Tests.Ui.FilterEditors;

namespace Mfr.Tests.Ui.FormatEditor
{
    /// <summary>
    /// Headless tests for <see cref="FormatTokenPickerPane"/> last-focus Insert/Edit and collapse.
    /// </summary>
    public sealed class FormatTokenPickerPaneViewTests
    {
        /// <summary>
        /// Verifies hosted format fields hide local Insert/Edit and expose the pane Edit.
        /// </summary>
        [AvaloniaFact]
        public void Pane_HidesPerFieldChrome_AndKeepsEditWhenCollapsed()
        {
            var (pane, left, _, window) = _ShowPaneWithTwoEditors();

            Assert.False(left.ShowInsertButton);
            Assert.False(left.ShowEditButton);
            Assert.False(left.ShowsToolButtons);
            Assert.True(pane.IsExpanded);
            Assert.True(_NamedDescendant<Button>(pane, "PART_EditButton").IsVisible);
            Assert.Same(left, pane.ActiveEditor);
            Assert.True(left.IsActiveTarget);

            var collapse = _NamedDescendant<Button>(pane, "PART_CollapseButton");
            collapse.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.False(pane.IsExpanded);
            Assert.Equal(26, pane.PaneWidth);
            Assert.Equal(
                FormatTokenPickerPane.PaneMinHeight,
                _NamedDescendant<DockPanel>(pane, "PART_GripRail").Height
            );
            Assert.True(_NamedDescendant<Button>(pane, "PART_EditButton").IsVisible);
            Assert.True(collapse.IsVisible);
            Assert.False(_NamedDescendant<Border>(pane, "PART_PickerBody").IsVisible);

            window.Close();
        }

        /// <summary>
        /// Verifies a sole hosted format field is the Insert target and shows the blue border cue.
        /// </summary>
        [AvaloniaFact]
        public void SingleEditor_IsActiveTargetWithBorderCue()
        {
            var editor = new App.Ui.Views.FormatEditor.FormatEditor
            {
                AcceptsReturn = false,
                ShowRightClickHint = false,
            };
            var pane = new FormatTokenPickerPane { Content = editor };
            var window = new Window
            {
                Width = 480,
                Height = 280,
                Content = pane,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Same(editor, pane.ActiveEditor);
            Assert.True(pane.HasActiveEditor);
            Assert.True(editor.IsActiveTarget);

            window.Close();
        }

        /// <summary>
        /// Verifies Insert targets the last-focused format field among multiple hosted editors.
        /// </summary>
        [AvaloniaFact]
        public void Insert_TargetsLastFocusedEditor()
        {
            var (pane, left, right, window) = _ShowPaneWithTwoEditors();

            left.Text = "L";
            right.Text = "R";
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.True(pane.HasActiveEditor);
            Assert.Same(left, pane.ActiveEditor);
            Assert.True(pane.IsExpanded);

            left.FindControl<TextEditor>("TemplateBox")!.CaretOffset = 1;
            pane.ActiveEditor!.InsertTextAtCaret("<file-name>");
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("L<file-name>", left.Text);
            Assert.Equal("R", right.Text);

            _FocusEditor(right);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Same(right, pane.ActiveEditor);
            Assert.True(right.IsActiveTarget);
            Assert.False(left.IsActiveTarget);

            right.FindControl<TextEditor>("TemplateBox")!.CaretOffset = 1;
            var entry = FormatTokenCatalog.Entries.First(e => e.CanonicalName == "file-name");
            var picker = _NamedDescendant<FormatTokenPicker>(pane, "PART_TokenPicker");
            Assert.IsType<App.Ui.ViewModels.FormatEditor.FormatTokenPickerViewModel>(picker.DataContext);
            ((App.Ui.ViewModels.FormatEditor.FormatTokenPickerViewModel)picker.DataContext).InsertEntryCommand.Execute(
                entry
            );
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("L<file-name>", left.Text);
            Assert.Equal("R<file-name>", right.Text);

            window.Close();
        }

        /// <summary>
        /// Verifies Edit on the picker pane uses the active editor caret token.
        /// </summary>
        [AvaloniaFact]
        public void PaneEdit_UsesActiveEditorToken()
        {
            var (pane, left, right, window) = _ShowPaneWithTwoEditors();
            left.Text = "<counter:initial=1>";
            right.Text = "plain";
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Same(left, pane.ActiveEditor);

            var box = left.FindControl<TextEditor>("TemplateBox");
            Assert.NotNull(box);
            box.CaretOffset = 2;

            var edit = _NamedDescendant<Button>(pane, "PART_EditButton");
            Assert.True(edit.IsEnabled);

            Assert.True(
                left.EditUnderCaretForTests(
                    accept: true,
                    mutate: vm =>
                    {
                        if (vm is App.Ui.ViewModels.FormatEditor.TokenEditors.CounterFormatTokenEditorViewModel counter)
                        {
                            counter.Initial = 9;
                        }
                    }
                )
            );

            Assert.Contains("initial=9", left.Text, StringComparison.Ordinal);
            Assert.Equal("plain", right.Text);
            Assert.Same(left, pane.ActiveEditor);

            _FocusEditor(right);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Same(right, pane.ActiveEditor);
            Assert.True(edit.IsEnabled);

            window.Close();
        }

        /// <summary>
        /// Verifies standalone FormatEditor keeps Insert/Edit chrome (nested/dialog style).
        /// </summary>
        [AvaloniaFact]
        public void StandaloneFormatEditor_KeepsLocalChrome()
        {
            var editor = new App.Ui.Views.FormatEditor.FormatEditor();
            var window = new Window
            {
                Width = 480,
                Height = 200,
                Content = editor,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.True(editor.ShowInsertButton);
            Assert.True(editor.ShowEditButton);
            Assert.True(editor.ShowsToolButtons);
            Assert.NotNull(editor.FindControl<Button>("InsertButton"));
            Assert.NotNull(editor.FindControl<Button>("EditButton"));

            window.Close();
        }

        /// <summary>
        /// Verifies a format-capable filter editor restores collapsed picker from session via FilterEditorViewModel.
        /// </summary>
        [AvaloniaFact]
        public void FilterEditor_RestoresCollapsedTokenPickerFromSession()
        {
            var session = new SessionState
            {
                FilterEditor = new SessionStateFilterEditor { FormatTokenPickerExpanded = false },
            };
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes(session);

            Assert.False(mainViewModel.FilterEditorViewModel.FormatTokenPickerExpanded);

            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("Formatter"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var pane = editorView.GetVisualDescendants().OfType<FormatTokenPickerPane>().Single();
            Assert.False(pane.IsExpanded);
            Assert.False(_NamedDescendant<Border>(pane, "PART_PickerBody").IsVisible);

            pane.IsExpanded = true;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.True(mainViewModel.FilterEditorViewModel.FormatTokenPickerExpanded);
            Assert.True(session.FilterEditor.FormatTokenPickerExpanded);

            window.Close();
        }

        /// <summary>
        /// Verifies collapse preference is shared when switching between format-capable filters.
        /// </summary>
        [AvaloniaFact]
        public void FilterEditor_SharesCollapsedPreferenceAcrossFormatEditors()
        {
            var session = new SessionState();
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes(session);
            var applied = mainViewModel.AppliedFiltersViewModel;

            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("Formatter"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var pane = editorView.GetVisualDescendants().OfType<FormatTokenPickerPane>().Single();
            Assert.True(pane.IsExpanded);

            pane.IsExpanded = false;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.False(session.FilterEditor!.FormatTokenPickerExpanded);

            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("Inserter"));
            applied.SetSelectedSteps([applied.Steps[^1]]);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            pane = editorView.GetVisualDescendants().OfType<FormatTokenPickerPane>().Single();
            Assert.False(pane.IsExpanded);
            Assert.False(_NamedDescendant<Border>(pane, "PART_PickerBody").IsVisible);

            window.Close();
        }

        private static T _NamedDescendant<T>(Control root, string name)
            where T : Control
        {
            return Assert.Single(root.GetVisualDescendants().OfType<T>(), c => c.Name == name);
        }

        private static void _FocusEditor(App.Ui.Views.FormatEditor.FormatEditor editor)
        {
            var box = editor.FindControl<TextEditor>("TemplateBox");
            Assert.NotNull(box);
            box.Focus();
            box.TextArea.Focus();
        }

        private static (
            FormatTokenPickerPane Pane,
            App.Ui.Views.FormatEditor.FormatEditor Left,
            App.Ui.Views.FormatEditor.FormatEditor Right,
            Window Window
        ) _ShowPaneWithTwoEditors()
        {
            var left = new App.Ui.Views.FormatEditor.FormatEditor
            {
                Name = "LeftEditor",
                AcceptsReturn = false,
                ShowRightClickHint = false,
            };
            var right = new App.Ui.Views.FormatEditor.FormatEditor
            {
                Name = "RightEditor",
                AcceptsReturn = false,
                ShowRightClickHint = false,
            };
            var pane = new FormatTokenPickerPane
            {
                Content = new StackPanel { Spacing = 8, Children = { left, right } },
            };
            var window = new Window
            {
                Width = 720,
                Height = 360,
                Content = pane,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.False(left.ShowInsertButton);
            Assert.False(right.ShowEditButton);

            return (pane, left, right, window);
        }
    }
}
