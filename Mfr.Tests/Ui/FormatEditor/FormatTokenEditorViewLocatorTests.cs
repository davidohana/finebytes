using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
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
        /// Verifies substr/token source fields host FormatEditor with Insert/Edit chrome (MFR7).
        /// </summary>
        /// <param name="canonicalName">Editable token with a nested source format string.</param>
        [AvaloniaTheory]
        [InlineData("substr")]
        [InlineData("token")]
        public void Dialog_SourceFormatString_UsesFormatEditorWithToolButtons(string canonicalName)
        {
            Assert.True(FormatTokenEditorRegistry.TryCreate(canonicalName, string.Empty, out var editor));
            Assert.NotNull(editor);
            var dialog = new FormatTokenEditorDialog(editor);
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var sourceRow = Assert.Single(
                dialog.GetVisualDescendants().OfType<FilterEditorLabeledRow>(),
                row => row.Label == "Source format string:"
            );
            var sourceEditor = Assert.Single(
                sourceRow.GetVisualDescendants().OfType<App.Ui.Views.FormatEditor.FormatEditor>()
            );
            Assert.True(sourceEditor.ShowsToolButtons);
            Assert.True(sourceEditor.ShowInsertButton);
            Assert.True(sourceEditor.ShowEditButton);
            Assert.False(sourceEditor.ShowRightClickHint);
            Assert.Equal(
                canonicalName == "substr"
                    ? ((SubstrFormatTokenEditorViewModel)editor).Source
                    : ((TokenFormatTokenEditorViewModel)editor).Source,
                sourceEditor.Text
            );

            // Bound default Source must highlight on open (attach re-resolves theme brushes).
            var templateBox = sourceEditor.FindControl<TextEditor>("TemplateBox");
            Assert.NotNull(templateBox);
            var colorizer = Assert.Single(
                templateBox.TextArea.TextView.LineTransformers.OfType<FormatTokenColorizingTransformer>()
            );
            Assert.NotNull(colorizer.TokenNameForeground);
            Assert.NotEmpty(colorizer.Tokens);
            Assert.Equal("file-name", colorizer.Tokens[0].CanonicalName);
            var background = Assert.Single(
                templateBox.TextArea.TextView.BackgroundRenderers.OfType<FormatTokenBackgroundRenderer>()
            );
            Assert.NotNull(background.TokenBackground);
            Assert.NotEmpty(background.Tokens);

            dialog.Close();
        }

        /// <summary>
        /// Verifies the resulting format string uses a read-only FormatEditor with highlight chrome.
        /// </summary>
        [AvaloniaFact]
        public void Dialog_ResultingFormatString_IsGrayedWhenReadOnly()
        {
            var editor = new CounterFormatTokenEditorViewModel(args: null);
            var dialog = new FormatTokenEditorDialog(editor);
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var preview = dialog.FindControl<App.Ui.Views.FormatEditor.FormatEditor>("ResultingFormatStringBox");
            Assert.NotNull(preview);
            Assert.True(preview.IsReadOnly);
            Assert.False(preview.ShowsToolButtons);
            Assert.False(preview.ShowRightClickHint);
            Assert.False(preview.AcceptsReturn);
            Assert.Equal(editor.ResultingFormatString, preview.Text);

            var templateBox = preview.FindControl<TextEditor>("TemplateBox");
            Assert.NotNull(templateBox);
            Assert.True(templateBox.IsReadOnly);
            Assert.Same(AppChromeFonts.AppChromeFixedWidthFamily, templateBox.FontFamily);

            var app = Application.Current;
            Assert.NotNull(app);
            Assert.True(app.TryGetResource("AppChromeAltSurfaceBrush", app.ActualThemeVariant, out var altRow));
            var expected = Assert.IsAssignableFrom<ISolidColorBrush>(altRow);
            var actual = Assert.IsAssignableFrom<ISolidColorBrush>(templateBox.Background);
            Assert.Equal(expected.Color, actual.Color);

            var colorizer = Assert.Single(
                templateBox.TextArea.TextView.LineTransformers.OfType<FormatTokenColorizingTransformer>()
            );
            Assert.NotEmpty(colorizer.Tokens);
            Assert.Equal("counter", colorizer.Tokens[0].CanonicalName);
            Assert.NotNull(colorizer.TokenNameForeground);

            var background = Assert.Single(
                templateBox.TextArea.TextView.BackgroundRenderers.OfType<FormatTokenBackgroundRenderer>()
            );
            Assert.Null(background.TokenBackground);
            Assert.Null(background.TokenAltBackground);

            dialog.Close();
        }

        /// <summary>
        /// Verifies token-editor fields and the resulting string share one label/control column.
        /// </summary>
        [AvaloniaFact]
        public void Dialog_LabeledFields_ShareColumnAlignment()
        {
            var editor = new NowFormatTokenEditorViewModel(null);
            var dialog = new FormatTokenEditorDialog(editor) { Width = 520 };
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var rows = dialog.GetVisualDescendants().OfType<FilterEditorLabeledRow>().ToList();
            var formatRow = Assert.Single(rows, row => row.Label == "Format:");
            var resultingRow = Assert.Single(rows, row => row.Label == "Resulting format string:");
            var formatCombo = Assert.Single(formatRow.GetVisualDescendants().OfType<ComboBox>());
            var resultingPreview = Assert.Single(
                resultingRow.GetVisualDescendants().OfType<App.Ui.Views.FormatEditor.FormatEditor>()
            );

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
            var resultingOrigin = resultingPreview.TranslatePoint(default, dialog);
            Assert.NotNull(formatOrigin);
            Assert.NotNull(resultingOrigin);
            Assert.Equal(formatOrigin.Value.X, resultingOrigin.Value.X, precision: 1);
            Assert.Equal(formatCombo.Bounds.Width, resultingPreview.Bounds.Width, precision: 1);

            dialog.Close();
        }

        /// <summary>
        /// Verifies Escape dismisses the token editor dialog (Cancel / <c>IsCancel</c>).
        /// </summary>
        [AvaloniaFact]
        public void Dialog_Escape_ClosesWithoutAccepting()
        {
            var editor = new NowFormatTokenEditorViewModel(null);
            var dialog = new FormatTokenEditorDialog(editor);
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.True(dialog.Focusable);
            Assert.True(dialog.IsVisible);

            dialog.RaiseEvent(
                new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyDownEvent,
                    Key = Key.Escape,
                    Source = dialog,
                }
            );
            Dispatcher.UIThread.RunJobs();

            Assert.False(dialog.IsVisible);
        }

        /// <summary>
        /// Verifies the docked footer stays fully inside the client area for a tall body (substr),
        /// and height is locked to content (horizontal-only resize).
        /// </summary>
        [AvaloniaFact]
        public void Dialog_FooterButtons_FullyVisible_ForSubstr()
        {
            Assert.True(FormatTokenEditorRegistry.TryCreate("substr", string.Empty, out var editor));
            Assert.NotNull(editor);
            var dialog = new FormatTokenEditorDialog(editor);
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(dialog.MinHeight, dialog.MaxHeight);
            Assert.True(dialog.MinHeight > 0);

            var ok = dialog.FindControl<Button>("OkButton");
            Assert.NotNull(ok);
            var topLeft = ok.TranslatePoint(default, dialog);
            Assert.NotNull(topLeft);
            var bottom = topLeft.Value.Y + ok.Bounds.Height;
            Assert.True(
                bottom <= dialog.Bounds.Height + 0.5,
                $"OK button bottom {bottom} exceeds dialog height {dialog.Bounds.Height}."
            );
            Assert.True(ok.IsVisible);
            Assert.True(ok.Bounds.Height > 1);

            dialog.Close();
        }

        /// <summary>
        /// Verifies a short token body opens shorter than substr (content-sized default height).
        /// </summary>
        [AvaloniaFact]
        public void Dialog_DefaultHeight_IsContentSized()
        {
            var now = new FormatTokenEditorDialog(new NowFormatTokenEditorViewModel(null));
            now.Show();
            now.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            now.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            var nowHeight = now.Bounds.Height;
            now.Close();

            Assert.True(FormatTokenEditorRegistry.TryCreate("substr", string.Empty, out var substrEditor));
            Assert.NotNull(substrEditor);
            var substr = new FormatTokenEditorDialog(substrEditor);
            substr.Show();
            substr.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            substr.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            var substrHeight = substr.Bounds.Height;
            substr.Close();

            Assert.True(nowHeight > 0);
            Assert.True(substrHeight > nowHeight, $"Expected substr {substrHeight} > now {nowHeight}.");
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
