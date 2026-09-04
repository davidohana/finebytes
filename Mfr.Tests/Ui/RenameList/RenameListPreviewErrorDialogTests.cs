using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.App.Ui.Views.RenameList;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Headless tests for the Rename List Show Preview Error dialog.
    /// </summary>
    public sealed class RenameListPreviewErrorDialogTests
    {
        /// <summary>
        /// Verifies the dialog shows summary, path, and folded message/technical text.
        /// </summary>
        [AvaloniaFact]
        public void Dialog_shows_message_and_technical_details()
        {
            var content = new RenameListPreviewErrorDialogContent(
                @"D:\Music\album",
                "Cannot apply audio tags to a directory.",
                "System.InvalidOperationException: directory"
            );
            var dialog = new RenameListPreviewErrorDialog(content);
            dialog.Show();
            dialog.UpdateLayout();

            var summaryText = dialog.FindControl<TextBlock>("SummaryText");
            var filePathText = dialog.FindControl<TextBlock>("FilePathText");
            var detailsText = dialog.FindControl<TextBox>("DetailsText");
            Assert.NotNull(summaryText);
            Assert.NotNull(filePathText);
            Assert.NotNull(detailsText);

            Assert.Equal("Preview Error", dialog.Title);
            Assert.Equal(RenameListPreviewErrorDisplay.Summary, summaryText.Text);
            Assert.Equal(@"D:\Music\album", filePathText.Text);
            Assert.Contains("Cannot apply audio tags", detailsText.Text, StringComparison.Ordinal);
            Assert.Contains("InvalidOperationException", detailsText.Text, StringComparison.Ordinal);
            Assert.True(detailsText.IsReadOnly);
            Assert.NotNull(dialog.FindControl<Button>("CopyButton"));
        }

        /// <summary>
        /// Verifies the details box still works when technical text is absent.
        /// </summary>
        [AvaloniaFact]
        public void Dialog_shows_message_only_without_technical_details()
        {
            var content = new RenameListPreviewErrorDialogContent(
                @"D:\Music\note.txt",
                "Destination path already in use.",
                TechnicalDetails: null
            );
            var dialog = new RenameListPreviewErrorDialog(content);
            dialog.Show();
            dialog.UpdateLayout();

            var detailsText = dialog.FindControl<TextBox>("DetailsText");
            Assert.NotNull(detailsText);
            Assert.Equal("Destination path already in use.", detailsText.Text);
        }
    }
}
