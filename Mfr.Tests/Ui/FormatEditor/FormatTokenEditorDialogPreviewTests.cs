using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.FormatEditor;
using Mfr.App.Ui.ViewModels.FormatEditor.TokenEditors;
using Mfr.App.Ui.Views.Controls;
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
        /// Verifies Preview chrome loads and sample/result share a content column.
        /// </summary>
        [AvaloniaFact]
        public void Dialog_Preview_ShowsChromeAndAlignedSampleResult()
        {
            var editor = new SubstrFormatTokenEditorViewModel(args: null);
            var dialog = new FormatTokenEditorDialog(editor);
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.NotNull(dialog.FindControl<TextBox>("PreviewSampleBox"));
            Assert.NotNull(dialog.FindControl<TextBox>("PreviewResultBox"));
            Assert.NotNull(dialog.FindControl<Button>("PreviewPreviousButton"));
            Assert.NotNull(dialog.FindControl<Button>("PreviewNextButton"));
            Assert.NotNull(dialog.FindControl<Grid>("PreviewRow"));
            Assert.Equal(
                ["Options", "Preview", "Resulting format string"],
                dialog
                    .GetVisualDescendants()
                    .OfType<FieldsetGroup>()
                    .Select(group => group.Header?.ToString() ?? string.Empty)
                    .ToArray()
            );
            Assert.Equal(
                FormatTokenPreviewViewModel.EmptyListSampleText,
                dialog.FindControl<TextBox>("PreviewSampleBox")!.Text
            );

            // Sample and result share the content column so their left edges align.
            var sample = dialog.FindControl<TextBox>("PreviewSampleBox")!;
            var result = dialog.FindControl<TextBox>("PreviewResultBox")!;
            var sampleLeft = sample.TranslatePoint(default, dialog)!.Value.X;
            var resultLeft = result.TranslatePoint(default, dialog)!.Value.X;
            Assert.Equal(sampleLeft, resultLeft, precision: 1);

            dialog.Close();
        }

        /// <summary>
        /// Verifies Preview binds rename items and updates on ▲/▼ click and option edits.
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

            var nextPoint = next.TranslatePoint(new Point(next.Bounds.Width / 2, next.Bounds.Height / 2), dialog);
            Assert.NotNull(nextPoint);
            dialog.MouseDown(nextPoint.Value, MouseButton.Left);
            dialog.MouseUp(nextPoint.Value, MouseButton.Left);
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
        /// Verifies nested token dialogs reuse the parent Preview snapshot.
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

            Assert.Same(items, FormatTokenEditorDialog.ResolvePreviewRenameItems(parent));

            var nested = new FormatTokenEditorDialog(
                new FileDateFormatTokenEditorViewModel(null),
                FormatTokenEditorDialog.ResolvePreviewRenameItems(parent)
            );
            nested.Show();
            nested.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("alpha.mp3", nested.FindControl<TextBox>("PreviewSampleBox")!.Text);
            Assert.Equal("1", nested.FindControl<TextBlock>("PreviewItemIndexLabel")!.Text);
            Assert.NotEqual(
                FormatTokenPreviewViewModel.PreviewUnavailableText,
                nested.FindControl<TextBox>("PreviewResultBox")!.Text
            );

            nested.Close();
            parent.Close();
        }

        /// <summary>
        /// Verifies Owner-chain walk uses a token dialog snapshot even when empty (no skip-to-MainWindow).
        /// </summary>
        [AvaloniaFact]
        public void ResolvePreviewRenameItems_OwnerChain_UsesDialogSnapshotIncludingEmpty()
        {
            var items = new[] { FilterTestHelpers.CreateRenameItem(prefix: "alpha", extension: ".mp3") };
            var parentWithItems = new FormatTokenEditorDialog(new SubstrFormatTokenEditorViewModel(null), items);
            parentWithItems.Show();
            Dispatcher.UIThread.RunJobs();

            var hosted = new Window { Width = 100, Height = 80 };
            _ = hosted.ShowDialog(parentWithItems);
            Dispatcher.UIThread.RunJobs();

            Assert.Same(parentWithItems.PreviewRenameItems, FormatTokenEditorDialog.ResolvePreviewRenameItems(hosted));
            hosted.Close();
            parentWithItems.Close();

            var emptyParent = new FormatTokenEditorDialog(new SubstrFormatTokenEditorViewModel(null), []);
            emptyParent.Show();
            Dispatcher.UIThread.RunJobs();

            var hostedOnEmpty = new Window { Width = 100, Height = 80 };
            _ = hostedOnEmpty.ShowDialog(emptyParent);
            Dispatcher.UIThread.RunJobs();

            Assert.Empty(FormatTokenEditorDialog.ResolvePreviewRenameItems(hostedOnEmpty));
            hostedOnEmpty.Close();
            emptyParent.Close();
        }
    }
}
