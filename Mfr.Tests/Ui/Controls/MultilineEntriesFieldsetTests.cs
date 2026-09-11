using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.Views.Controls;

namespace Mfr.Tests.Ui.Controls
{
    /// <summary>
    /// Headless layout tests for <see cref="MultilineEntriesFieldset"/>.
    /// </summary>
    public sealed class MultilineEntriesFieldsetTests
    {
        /// <summary>
        /// Verifies header, label, hint, tip, and content are reachable after show.
        /// </summary>
        [AvaloniaFact]
        public void Shows_header_label_hint_and_content()
        {
            var editor = new TextBox { Name = "EntriesBox", AcceptsReturn = true };
            var fieldset = new MultilineEntriesFieldset
            {
                Header = "Name List",
                Label = "Entries:",
                Hint = "One name per line.",
                Tip = RichToolTip.Wrap("Tip body"),
                Content = editor,
            };

            var window = new Window
            {
                Width = 420,
                Height = 200,
                Content = fieldset,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            try
            {
                Assert.Same(
                    editor,
                    fieldset.GetVisualDescendants().OfType<TextBox>().Single(t => t.Name == "EntriesBox")
                );
                Assert.Equal("Name List", fieldset.Header);
                Assert.Equal("Entries:", fieldset.Label);
                Assert.Equal("One name per line.", fieldset.Hint);
                Assert.NotNull(fieldset.Tip);

                var hint = fieldset.GetVisualDescendants().OfType<FilterEditorHint>().Single();
                Assert.Equal("One name per line.", hint.Text);
                Assert.True(hint.IsVisible);

                var label = fieldset
                    .GetVisualDescendants()
                    .OfType<TextBlock>()
                    .Single(t => t.Classes.Contains("filter-editor-label"));
                Assert.Equal("Entries:", label.Text);
                Assert.True(label.IsVisible);
            }
            finally
            {
                window.Close();
            }
        }

        /// <summary>
        /// Verifies empty label and hint hide their chrome without removing content.
        /// </summary>
        [AvaloniaFact]
        public void Hides_empty_label_and_hint()
        {
            var editor = new TextBox { Name = "EntriesBox" };
            var fieldset = new MultilineEntriesFieldset { Header = "Replace List", Content = editor };

            var window = new Window
            {
                Width = 420,
                Height = 160,
                Content = fieldset,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            try
            {
                Assert.Same(
                    editor,
                    fieldset.GetVisualDescendants().OfType<TextBox>().Single(t => t.Name == "EntriesBox")
                );
                Assert.DoesNotContain(
                    fieldset.GetVisualDescendants().OfType<TextBlock>(),
                    t => t.Classes.Contains("filter-editor-label") && t.IsVisible
                );
                var hint = fieldset.GetVisualDescendants().OfType<FilterEditorHint>().SingleOrDefault();
                Assert.True(hint is null || !hint.IsVisible);
            }
            finally
            {
                window.Close();
            }
        }
    }
}
