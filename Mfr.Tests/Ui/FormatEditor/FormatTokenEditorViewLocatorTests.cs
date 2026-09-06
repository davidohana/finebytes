using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.FormatEditor;
using Mfr.App.Ui.ViewModels.FormatEditor.TokenEditors;
using Mfr.App.Ui.Views.Controls;
using Mfr.App.Ui.Views.FormatEditor;
using Mfr.App.Ui.Views.FormatEditor.TokenEditors;
using Mfr.App.Ui.Views.GridColumnSizing;

namespace Mfr.Tests.Ui.FormatEditor
{
    /// <summary>
    /// Headless / unit tests for <see cref="FormatTokenEditorViewLocator"/> and dialog body hosting.
    /// </summary>
    public sealed class FormatTokenEditorViewLocatorTests
    {
        private static readonly string[] EditableCanonicalNames =
        [
            "counter",
            "parent-folder",
            "now",
            "exif-date",
            "random-char",
            "substr",
            "token",
            "file-date",
            "file-size",
            "id3v2",
            "exif",
        ];

        /// <summary>
        /// Verifies every registered editable token builds a convention-matched body view.
        /// </summary>
        [AvaloniaFact]
        public void Build_ResolvesView_ForEveryRegisteredToken()
        {
            var locator = new FormatTokenEditorViewLocator();
            foreach (var name in EditableCanonicalNames)
            {
                Assert.True(FormatTokenEditorRegistry.TryCreate(name, string.Empty, out var editor));
                Assert.NotNull(editor);
                Assert.True(locator.Match(editor));

                var body = locator.Build(editor);
                Assert.NotNull(body);
                Assert.Same(editor, body.DataContext);

                var expectedViewName = editor.GetType().Name[..^"ViewModel".Length] + "View";
                Assert.Equal(expectedViewName, body.GetType().Name);
                Assert.Equal("Mfr.App.Ui.Views.FormatEditor.TokenEditors", body.GetType().Namespace);
            }
        }

        /// <summary>
        /// Verifies the dialog hosts the counter body via the view locator template.
        /// </summary>
        [AvaloniaFact]
        public void Dialog_HostsCounterBody_ViaLocator()
        {
            var editor = new CounterFormatTokenEditorViewModel(args: null);
            var dialog = new FormatTokenEditorDialog(editor);
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.NotNull(dialog.FindDescendantOfType<CounterFormatTokenEditorView>());
            dialog.Close();
        }

        /// <summary>
        /// Verifies the resulting format string is read-only and uses grayed field chrome.
        /// </summary>
        [AvaloniaFact]
        public void Dialog_ResultingFormatString_IsGrayedWhenReadOnly()
        {
            var editor = new CounterFormatTokenEditorViewModel(args: null);
            var dialog = new FormatTokenEditorDialog(editor);
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var box = dialog.FindControl<TextBox>("ResultingFormatStringBox");
            Assert.NotNull(box);
            Assert.True(box.IsReadOnly);
            Assert.Equal(editor.ResultingFormatString, box.Text);
            Assert.Same(GridFonts.RenameListFixedWidthFamily, box.FontFamily);

            var app = Application.Current;
            Assert.NotNull(app);
            Assert.True(app.TryGetResource("FileListAltRowBrush", app.ActualThemeVariant, out var altRow));
            var expected = Assert.IsAssignableFrom<ISolidColorBrush>(altRow);
            var actual = Assert.IsAssignableFrom<ISolidColorBrush>(box.Background);
            Assert.Equal(expected.Color, actual.Color);

            dialog.Close();
        }

        /// <summary>
        /// Verifies token-editor fields and the resulting string share one label/control column.
        /// </summary>
        [AvaloniaFact]
        public void Dialog_LabeledFields_ShareColumnAlignment()
        {
            var editor = new NowFormatTokenEditorViewModel(null);
            var dialog = new FormatTokenEditorDialog(editor) { Width = 480 };
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var rows = dialog.GetVisualDescendants().OfType<FilterEditorLabeledRow>().ToList();
            var formatRow = Assert.Single(rows, row => row.Label == "Format:");
            var resultingRow = Assert.Single(rows, row => row.Label == "Resulting format string:");
            var formatCombo = Assert.Single(formatRow.GetVisualDescendants().OfType<ComboBox>());
            var resultingBox = Assert.Single(resultingRow.GetVisualDescendants().OfType<TextBox>());

            Assert.True(formatCombo.IsEditable);
            Assert.Same(DateFormatExamples.All, formatCombo.ItemsSource);
            Assert.Equal(editor.Format, formatCombo.Text);

            var formatDocsHint = Assert.Single(
                dialog.GetVisualDescendants().OfType<FilterEditorHint>(),
                hint => hint.Name == "FormatDocsHint"
            );
            Assert.Equal(DateFormatExamples.DocsLinkText, formatDocsHint.LinkText);
            Assert.Equal(DateFormatExamples.DocsUri, formatDocsHint.NavigateUri);

            var formatOrigin = formatCombo.TranslatePoint(default, dialog);
            var resultingOrigin = resultingBox.TranslatePoint(default, dialog);
            Assert.NotNull(formatOrigin);
            Assert.NotNull(resultingOrigin);
            Assert.Equal(formatOrigin.Value.X, resultingOrigin.Value.X, precision: 1);
            Assert.Equal(formatCombo.Bounds.Width, resultingBox.Bounds.Width, precision: 1);

            dialog.Close();
        }

        /// <summary>
        /// Verifies Build fails loudly when the view-model is outside the TokenEditors namespace.
        /// </summary>
        [Fact]
        public void Build_Throws_WhenViewModelNamespaceDoesNotMatch()
        {
            var locator = new FormatTokenEditorViewLocator();
            var orphan = new OrphanFormatTokenEditorViewModel();
            Assert.True(locator.Match(orphan));
            var ex = Assert.Throws<InvalidOperationException>(() => locator.Build(orphan));
            Assert.Contains("TokenEditors", ex.Message, StringComparison.Ordinal);
        }

        private sealed class OrphanFormatTokenEditorViewModel : IFormatTokenEditorViewModel
        {
            public string Title => "orphan";

            public string CanonicalName => "orphan";

            public string ResultingFormatString => "<orphan>";

            public string BuildInnerText()
            {
                return "orphan";
            }
        }
    }
}
