using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.FormatEditor.TokenEditors;
using Mfr.App.Ui.Views.FormatEditor;
using Mfr.Tests.Models.Filters;

namespace Mfr.Tests.Ui.FormatEditor
{
    /// <summary>
    /// Headless tests for the Format Token Editor dialog Preview band.
    /// </summary>
    public sealed class FormatTokenEditorDialogPreviewTests
    {
        /// <summary>
        /// Verifies Preview chrome is present and shows empty-list messaging by default.
        /// </summary>
        [AvaloniaFact]
        public void Dialog_Preview_ShowsEmptyListMessages_WhenNoRenameItems()
        {
            var editor = new SubstrFormatTokenEditorViewModel(args: null);
            var dialog = new FormatTokenEditorDialog(editor);
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.NotNull(dialog.FindControl<TextBox>("PreviewSampleBox"));
            Assert.NotNull(dialog.FindControl<TextBox>("PreviewResultBox"));
            Assert.Equal("<Rename list is empty>", dialog.FindControl<TextBox>("PreviewSampleBox")!.Text);
            Assert.Equal("<Preview N/A>", dialog.FindControl<TextBox>("PreviewResultBox")!.Text);
            Assert.False(dialog.FindControl<Button>("PreviewPreviousButton")!.IsEnabled);
            Assert.False(dialog.FindControl<Button>("PreviewNextButton")!.IsEnabled);
            Assert.NotNull(dialog.FindControl<Grid>("PreviewRow"));
            Assert.NotNull(dialog.FindControl<StackPanel>("PreviewPanel"));

            // Sample and result share the content column so their left edges align.
            var sample = dialog.FindControl<TextBox>("PreviewSampleBox")!;
            var result = dialog.FindControl<TextBox>("PreviewResultBox")!;
            var sampleLeft = sample.TranslatePoint(default, dialog)!.Value.X;
            var resultLeft = result.TranslatePoint(default, dialog)!.Value.X;
            Assert.Equal(sampleLeft, resultLeft, precision: 1);

            dialog.Close();
        }

        /// <summary>
        /// Verifies Preview evaluates against rename items and updates when cycling / options change.
        /// </summary>
        [AvaloniaFact]
        public void Dialog_Preview_EvaluatesAndCyclesRenameItems()
        {
            var items = new[]
            {
                FilterTestHelpers.CreateRenameItem(prefix: "alpha", extension: ".mp3"),
                FilterTestHelpers.CreateRenameItem(prefix: "beta", extension: ".wav", renameListIndex: 1),
            };
            var editor = new SubstrFormatTokenEditorViewModel("start=1,end=-1,source=<file-name>");
            var dialog = new FormatTokenEditorDialog(editor, items);
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var sample = dialog.FindControl<TextBox>("PreviewSampleBox")!;
            var result = dialog.FindControl<TextBox>("PreviewResultBox")!;
            var indexLabel = dialog.FindControl<TextBlock>("PreviewItemIndexLabel")!;
            var next = dialog.FindControl<Button>("PreviewNextButton")!;
            var previous = dialog.FindControl<Button>("PreviewPreviousButton")!;

            Assert.Equal("alpha.mp3", sample.Text);
            Assert.Equal("alpha", result.Text);
            Assert.Equal("1", indexLabel.Text);
            Assert.False(previous.IsEnabled);
            Assert.True(next.IsEnabled);

            next.Command!.Execute(null);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("beta.wav", sample.Text);
            Assert.Equal("beta", result.Text);
            Assert.Equal("2", indexLabel.Text);
            Assert.True(previous.IsEnabled);
            Assert.False(next.IsEnabled);

            editor.FromPosition = 2;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("eta", result.Text);

            dialog.Close();
        }

        /// <summary>
        /// Verifies a nested token dialog reuses the parent dialog's Rename List Preview snapshot.
        /// </summary>
        [AvaloniaFact]
        public void NestedDialog_ReusesParentPreviewRenameItems()
        {
            var items = new[]
            {
                FilterTestHelpers.CreateRenameItem(prefix: "alpha", extension: ".mp3"),
                FilterTestHelpers.CreateRenameItem(prefix: "beta", extension: ".wav", renameListIndex: 1),
            };
            var parent = new FormatTokenEditorDialog(new SubstrFormatTokenEditorViewModel(null), items);
            parent.Show();
            Dispatcher.UIThread.RunJobs();

            var resolved = FormatTokenEditorDialog.ResolvePreviewRenameItems(parent);
            Assert.Same(items[0], resolved[0]);
            Assert.Equal(2, resolved.Count);

            var nested = new FormatTokenEditorDialog(new FileDateFormatTokenEditorViewModel(null), resolved);
            nested.Show();
            nested.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("alpha.mp3", nested.FindControl<TextBox>("PreviewSampleBox")!.Text);
            Assert.Equal("1", nested.FindControl<TextBlock>("PreviewItemIndexLabel")!.Text);
            Assert.NotEqual("<Preview N/A>", nested.FindControl<TextBox>("PreviewResultBox")!.Text);
            Assert.DoesNotContain(
                "<Rename list is empty>",
                nested.FindControl<TextBox>("PreviewSampleBox")!.Text,
                StringComparison.Ordinal
            );

            nested.Close();
            parent.Close();
        }
    }
}
