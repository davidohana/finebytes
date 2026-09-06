using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Mfr.Filters.Formatting;

namespace Mfr.Tests.Ui.Controls.FormatEditor
{
    /// <summary>
    /// Headless tests for the shared <see cref="FormatEditor"/> control.
    /// </summary>
    public sealed class FormatEditorViewTests
    {
        /// <summary>
        /// Verifies insert-at-caret updates text and clears selection replacement.
        /// </summary>
        [AvaloniaFact]
        public void InsertTextAtCaret_InsertsCatalogToken()
        {
            var editor = new App.Ui.Views.Controls.FormatEditor.FormatEditor { Text = "pre__post" };
            var window = new Window
            {
                Width = 480,
                Height = 200,
                Content = editor,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var box = editor.FindControl<TextBox>("TemplateBox");
            Assert.NotNull(box);
            box.CaretIndex = 3;
            box.SelectionStart = 3;
            box.SelectionEnd = 5;

            editor.InsertTextAtCaret("<file-name>");
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("pre<file-name>post", editor.Text);
            Assert.False(editor.ViewModel.HasError);

            window.Close();
        }

        /// <summary>
        /// Verifies a bad template shows an error and JumpToError selects the span.
        /// </summary>
        [AvaloniaFact]
        public void JumpToError_SelectsFailingSpan()
        {
            var editor = new App.Ui.Views.Controls.FormatEditor.FormatEditor();
            var window = new Window
            {
                Width = 480,
                Height = 200,
                Content = editor,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            editor.Text = "x <does-not-exist> y";
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.True(editor.ViewModel.HasError);
            Assert.Contains("Unknown formatter token", editor.ViewModel.ErrorMessage);

            editor.JumpToError();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var box = editor.FindControl<TextBox>("TemplateBox");
            Assert.NotNull(box);
            Assert.Equal(editor.Text.IndexOf('<'), box.SelectionStart);
            Assert.Equal(editor.Text.IndexOf('<') + "<does-not-exist>".Length, box.SelectionEnd);

            window.Close();
        }

        /// <summary>
        /// Verifies tapping a catalog row inserts its default text (keyboard highlight alone must not).
        /// </summary>
        [AvaloniaFact]
        public void InsertList_Tapped_InsertsCatalogEntry()
        {
            var editor = new App.Ui.Views.Controls.FormatEditor.FormatEditor();
            var window = new Window
            {
                Width = 480,
                Height = 320,
                Content = editor,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var insertButton = editor.FindControl<Button>("InsertButton");
            Assert.NotNull(insertButton);
            Assert.IsType<Flyout>(insertButton.Flyout);
            ((Flyout)insertButton.Flyout!).ShowAt(insertButton);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var list = editor.FindControl<ListBox>("InsertList");
            Assert.NotNull(list);
            var entry = FormatTokenCatalog.Entries.First(e => e.CanonicalName == "file-name");
            list.ScrollIntoView(entry);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var container = Assert.IsType<ListBoxItem>(list.ContainerFromItem(entry));
            container.RaiseEvent(new TappedEventArgs(InputElement.TappedEvent, null!));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(entry.InsertText, editor.Text);
            Assert.False(editor.ViewModel.HasError);

            window.Close();
        }

        /// <summary>
        /// Verifies double-tap on a validated token selects that span.
        /// </summary>
        [AvaloniaFact]
        public void Template_DoubleTap_SelectsTokenSpan()
        {
            var editor = new App.Ui.Views.Controls.FormatEditor.FormatEditor { Text = "pre<file-name>post" };
            var window = new Window
            {
                Width = 480,
                Height = 200,
                Content = editor,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var box = editor.FindControl<TextBox>("TemplateBox");
            Assert.NotNull(box);
            var tokenStart = editor.Text.IndexOf('<');
            box.CaretIndex = tokenStart + 1;
            box.SelectionStart = tokenStart + 1;
            box.SelectionEnd = tokenStart + 1;

            box.RaiseEvent(new TappedEventArgs(InputElement.DoubleTappedEvent, null!));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(tokenStart, box.SelectionStart);
            Assert.Equal(tokenStart + "<file-name>".Length, box.SelectionEnd);

            window.Close();
        }

        /// <summary>
        /// Verifies constructing the control with catalog-backed picker does not throw.
        /// </summary>
        [AvaloniaFact]
        public void Construct_ShowsWithoutThrowing()
        {
            var editor = new App.Ui.Views.Controls.FormatEditor.FormatEditor
            {
                Text = FormatTokenCatalog.Entries[0].InsertText,
            };
            var window = new Window { Content = editor };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.NotEmpty(editor.ViewModel.VisibleEntries);
            Assert.False(editor.ViewModel.HasError);
            window.Close();
        }
    }
}
