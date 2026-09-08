using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using AvaloniaEdit;
using Mfr.App.Ui.Views.FormatEditor;
using Mfr.Filters.Formatting.FormatString;

namespace Mfr.Tests.Ui.FormatEditor
{
    /// <summary>
    /// Headless tests for <see cref="FormatTokenToolsHost"/> last-focus Insert/Edit and collapse.
    /// </summary>
    public sealed class FormatTokenToolsHostViewTests
    {
        /// <summary>
        /// Verifies hosted format fields hide local Insert/Edit and expose the tools pane Edit.
        /// </summary>
        [AvaloniaFact]
        public void Host_HidesPerFieldChrome_AndKeepsPaneEditWhenCollapsed()
        {
            var (host, left, _, window) = _ShowHostWithTwoEditors();

            Assert.False(left.ShowInsertButton);
            Assert.False(left.ShowEditButton);
            Assert.False(left.ShowsToolButtons);
            Assert.True(host.IsExpanded);
            Assert.True(host.FindControl<Button>("EditButton")!.IsVisible);
            Assert.Same(left, host.ActiveEditor);
            Assert.True(left.IsActiveTarget);

            host.IsExpanded = false;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.False(host.IsExpanded);
            Assert.Equal(64, host.ToolsPaneWidth);
            Assert.True(host.FindControl<Button>("EditButton")!.IsVisible);
            Assert.False(host.ShowsInsertPicker);

            var pane = host.FindControl<Border>("ToolsPane");
            Assert.NotNull(pane);
            Assert.True(pane.Bounds.Width >= 64 - 0.5);

            window.Close();
        }

        /// <summary>
        /// Verifies Insert targets the last-focused format field among multiple hosted editors.
        /// </summary>
        [AvaloniaFact]
        public void Insert_TargetsLastFocusedEditor()
        {
            var (host, left, right, window) = _ShowHostWithTwoEditors();

            left.Text = "L";
            right.Text = "R";
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.True(host.HasActiveEditor);
            Assert.Same(left, host.ActiveEditor);
            Assert.True(host.ShowsInsertPicker);

            left.FindControl<TextEditor>("TemplateBox")!.CaretOffset = 1;
            host.ActiveEditor!.InsertTextAtCaret("<file-name>");
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("L<file-name>", left.Text);
            Assert.Equal("R", right.Text);

            _FocusEditor(right);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Same(right, host.ActiveEditor);
            Assert.True(right.IsActiveTarget);
            Assert.False(left.IsActiveTarget);

            right.FindControl<TextEditor>("TemplateBox")!.CaretOffset = 1;
            var entry = FormatTokenCatalog.Entries.First(e => e.CanonicalName == "file-name");
            var picker = host.FindControl<FormatTokenInsertPicker>("InsertPicker");
            Assert.NotNull(picker);
            Assert.IsType<App.Ui.ViewModels.FormatEditor.FormatEditorViewModel>(picker.DataContext);
            ((App.Ui.ViewModels.FormatEditor.FormatEditorViewModel)picker.DataContext).InsertEntryCommand.Execute(
                entry
            );
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("L<file-name>", left.Text);
            Assert.Equal("R<file-name>", right.Text);

            window.Close();
        }

        /// <summary>
        /// Verifies Edit on the tools pane uses the active editor caret token.
        /// </summary>
        [AvaloniaFact]
        public void PaneEdit_UsesActiveEditorToken()
        {
            var (host, left, right, window) = _ShowHostWithTwoEditors();
            left.Text = "<counter:initial=1>";
            right.Text = "plain";
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Same(left, host.ActiveEditor);

            var box = left.FindControl<TextEditor>("TemplateBox");
            Assert.NotNull(box);
            box.CaretOffset = 2;

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
            Assert.Same(left, host.ActiveEditor);

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

        private static void _FocusEditor(App.Ui.Views.FormatEditor.FormatEditor editor)
        {
            var box = editor.FindControl<TextEditor>("TemplateBox");
            Assert.NotNull(box);
            box.Focus();
            box.TextArea.Focus();
        }

        private static (
            FormatTokenToolsHost Host,
            App.Ui.Views.FormatEditor.FormatEditor Left,
            App.Ui.Views.FormatEditor.FormatEditor Right,
            Window Window
        ) _ShowHostWithTwoEditors()
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
            var host = new FormatTokenToolsHost
            {
                Body = new StackPanel { Spacing = 8, Children = { left, right } },
            };
            var window = new Window
            {
                Width = 720,
                Height = 360,
                Content = host,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.False(left.ShowInsertButton);
            Assert.False(right.ShowEditButton);

            return (host, left, right, window);
        }
    }
}
