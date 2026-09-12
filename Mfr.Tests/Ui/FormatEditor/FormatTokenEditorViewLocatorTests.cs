using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
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
        /// Verifies Digits / Upper Letters / Lower Letters sample buttons update Low/High (MFR7).
        /// </summary>
        [AvaloniaFact]
        public void Dialog_RandomChar_SampleButtons_UpdateLowHigh()
        {
            var editor = new RandomCharFormatTokenEditorViewModel(args: null);
            var dialog = new FormatTokenEditorDialog(editor);
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var body = Assert.IsType<RandomCharFormatTokenEditorView>(
                dialog.FindDescendantOfType<RandomCharFormatTokenEditorView>()
            );
            var lowBox = Assert.IsType<TextBox>(body.FindControl<TextBox>("LowBox"));
            var highBox = Assert.IsType<TextBox>(body.FindControl<TextBox>("HighBox"));
            var digits = Assert.IsType<Button>(body.FindControl<Button>("DigitsSampleButton"));
            var upper = Assert.IsType<Button>(body.FindControl<Button>("UpperLettersSampleButton"));
            var lower = Assert.IsType<Button>(body.FindControl<Button>("LowerLettersSampleButton"));
            var symbols = Assert.IsType<Button>(body.FindControl<Button>("SymbolsSampleButton"));

            _Click(dialog, digits);
            Assert.Equal("0", editor.Low);
            Assert.Equal("9", editor.High);
            Assert.Equal("0", lowBox.Text);
            Assert.Equal("9", highBox.Text);

            _Click(dialog, upper);
            Assert.Equal("A", editor.Low);
            Assert.Equal("Z", editor.High);

            _Click(dialog, lower);
            Assert.Equal("a", editor.Low);
            Assert.Equal("z", editor.High);
            Assert.Equal("a", lowBox.Text);
            Assert.Equal("z", highBox.Text);

            _Click(dialog, symbols);
            Assert.Equal("!", editor.Low);
            Assert.Equal("/", editor.High);
            Assert.Equal("!", lowBox.Text);
            Assert.Equal("/", highBox.Text);

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
        /// Verifies the Now Format field, docs hint, and resulting preview, plus SharedSizeGroup
        /// alignment between the Format control and the Preview sample column.
        /// </summary>
        [AvaloniaFact]
        public void Dialog_Now_FormatField_Aligns_With_Preview_Column()
        {
            var editor = new NowFormatTokenEditorViewModel(null);
            var dialog = new FormatTokenEditorDialog(editor) { Width = 520 };
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var formatRow = Assert.Single(
                dialog.GetVisualDescendants().OfType<FilterEditorLabeledRow>(),
                row => row.Label == "Format:"
            );
            var formatCombo = Assert.Single(formatRow.GetVisualDescendants().OfType<ComboBox>());
            Assert.True(formatCombo.IsEditable);
            Assert.Same(DateFormatExamples.All, formatCombo.ItemsSource);
            Assert.Equal(editor.Format, formatCombo.Text);

            var formatDocsHint = Assert.Single(
                dialog.GetVisualDescendants().OfType<FilterEditorHint>(),
                hint => hint.Name == "FormatDocsHint"
            );
            Assert.Equal(DateFormatExamples.DocsLinkText, formatDocsHint.LinkText);
            Assert.Equal(DateFormatExamples.DocsUri, formatDocsHint.NavigateUri);

            var resultingPreview = dialog.FindControl<App.Ui.Views.FormatEditor.FormatEditor>(
                "ResultingFormatStringBox"
            );
            Assert.NotNull(resultingPreview);
            Assert.True(resultingPreview.IsReadOnly);
            Assert.Equal(editor.ResultingFormatString, resultingPreview.Text);

            var previewPanel = dialog.FindControl<StackPanel>("PreviewPanel");
            Assert.NotNull(previewPanel);

            var formatOrigin = formatCombo.TranslatePoint(default, dialog);
            var previewOrigin = previewPanel.TranslatePoint(default, dialog);
            Assert.NotNull(formatOrigin);
            Assert.NotNull(previewOrigin);
            Assert.Equal(formatOrigin.Value.X, previewOrigin.Value.X, precision: 1);
            Assert.Equal(formatCombo.Bounds.Width, previewPanel.Bounds.Width, precision: 1);

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

            var ok = ModalOkCancelFooterAccess.RequireAcceptButton(dialog);
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
        /// Verifies height relocks when the nested source FormatEditor auto-grows (wrap), so the
        /// footer is not clipped under a stale MaxHeight lock.
        /// </summary>
        [AvaloniaFact]
        public void Dialog_RelocksHeight_WhenSourceFormatEditorGrows()
        {
            Assert.True(FormatTokenEditorRegistry.TryCreate("substr", string.Empty, out var editor));
            Assert.NotNull(editor);
            var dialog = new FormatTokenEditorDialog(editor) { Width = 440 };
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var initialHeight = dialog.Bounds.Height;
            Assert.True(initialHeight > 0);

            var sourceEditor = Assert.Single(
                dialog.GetVisualDescendants().OfType<App.Ui.Views.FormatEditor.FormatEditor>(),
                fe => fe.Name == "SourceEditor"
            );
            sourceEditor.Text = string.Concat(Enumerable.Repeat("<file-name>", 24));
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            sourceEditor.UpdateAutoGrowHeightForTests();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(dialog.MinHeight, dialog.MaxHeight);
            Assert.True(
                dialog.Bounds.Height > initialHeight,
                $"Expected grown height {dialog.Bounds.Height} > initial {initialHeight}."
            );

            var ok = ModalOkCancelFooterAccess.RequireAcceptButton(dialog);
            var topLeft = ok.TranslatePoint(default, dialog);
            Assert.NotNull(topLeft);
            Assert.True(
                topLeft.Value.Y + ok.Bounds.Height <= dialog.Bounds.Height + 0.5,
                "OK button should stay fully visible after source grow."
            );

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

        private static void _Click(Window host, Control target)
        {
            var point = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), host);
            Assert.NotNull(point);
            host.MouseDown(point.Value, MouseButton.Left);
            host.MouseUp(point.Value, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
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
