using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.FormatEditor;
using Mfr.App.Ui.ViewModels.FormatEditor.TokenEditors;
using Mfr.App.Ui.Views.FormatEditor;
using Mfr.App.Ui.Views.FormatEditor.TokenEditors;

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
