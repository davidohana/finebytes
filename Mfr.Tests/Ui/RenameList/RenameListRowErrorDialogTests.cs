using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.App.Ui.Views.RenameList;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Headless tests for the shared Rename List row-error dialog.
    /// </summary>
    public sealed class RenameListRowErrorDialogTests
    {
        /// <summary>
        /// Verifies load errors show a single copyable box with folded friendly and technical text.
        /// </summary>
        [AvaloniaFact]
        public void Dialog_shows_load_errors_and_one_details_box()
        {
            var tagLibMessage = @"D:\Music\PLAYLIST.M3U (taglib/m3u)";
            var imageMessage = "Cannot read image properties";
            var content = RenameListLoadErrorDisplay.Create(
                @"D:\Music\PLAYLIST.M3U",
                [
                    new RenameListLoadError("This file could not be read as audio or media metadata.", tagLibMessage),
                    new RenameListLoadError("This file could not be read as image or EXIF metadata.", imageMessage),
                ]
            );
            var dialog = new RenameListRowErrorDialog(content);
            dialog.Show();
            dialog.UpdateLayout();

            var summaryText = dialog.FindControl<TextBlock>("SummaryText");
            var filePathText = dialog.FindControl<TextBlock>("FilePathText");
            var detailsText = dialog.FindControl<TextBox>("DetailsText");
            Assert.NotNull(summaryText);
            Assert.NotNull(filePathText);
            Assert.NotNull(detailsText);

            Assert.Equal(RenameListLoadErrorDisplay.DialogTitle, dialog.Title);
            Assert.Equal(RenameListLoadErrorDisplay.MetadataSummary, summaryText.Text);
            Assert.Equal(@"D:\Music\PLAYLIST.M3U", filePathText.Text);
            Assert.Contains(tagLibMessage, detailsText.Text, StringComparison.Ordinal);
            Assert.Contains(imageMessage, detailsText.Text, StringComparison.Ordinal);
            Assert.Contains("audio or media metadata", detailsText.Text, StringComparison.Ordinal);
            Assert.Contains("image or EXIF metadata", detailsText.Text, StringComparison.Ordinal);
            Assert.True(detailsText.IsReadOnly);
            Assert.NotNull(dialog.FindControl<Button>("CopyButton"));
        }

        /// <summary>
        /// Verifies missing rows show a missing-path headline instead of the metadata summary.
        /// </summary>
        [AvaloniaFact]
        public void Dialog_shows_missing_summary_for_absent_path()
        {
            const string path = @"D:\Music\1\Working on the Highway.mp3";
            var content = RenameListLoadErrorDisplay.Create(path, [RenameListDiskPaths.MissingLoadError(path)]);
            var dialog = new RenameListRowErrorDialog(content);
            dialog.Show();
            dialog.UpdateLayout();

            var summaryText = dialog.FindControl<TextBlock>("SummaryText");
            var detailsText = dialog.FindControl<TextBox>("DetailsText");
            Assert.NotNull(summaryText);
            Assert.NotNull(detailsText);

            Assert.Equal(RenameListLoadErrorDisplay.MissingSummary, summaryText.Text);
            Assert.Equal(RenameListDiskPaths.MissingUserExplanation, detailsText.Text);
        }

        /// <summary>
        /// Verifies a preview failure shows summary, path, and folded message/technical text.
        /// </summary>
        [AvaloniaFact]
        public void Dialog_shows_preview_message_and_technical_details()
        {
            var content = RenameListPreviewErrorDisplay.Create(
                @"D:\Music\album",
                "Cannot apply audio tags to a directory.",
                "System.InvalidOperationException: directory"
            );
            var dialog = new RenameListRowErrorDialog(content);
            dialog.Show();
            dialog.UpdateLayout();

            var summaryText = dialog.FindControl<TextBlock>("SummaryText");
            var filePathText = dialog.FindControl<TextBlock>("FilePathText");
            var detailsText = dialog.FindControl<TextBox>("DetailsText");
            Assert.NotNull(summaryText);
            Assert.NotNull(filePathText);
            Assert.NotNull(detailsText);

            Assert.Equal(RenameListPreviewErrorDisplay.DialogTitle, dialog.Title);
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
        public void Dialog_shows_preview_message_only_without_technical_details()
        {
            var content = RenameListPreviewErrorDisplay.Create(
                @"D:\Music\note.txt",
                "Destination path already in use.",
                technicalDetails: null
            );
            var dialog = new RenameListRowErrorDialog(content);
            dialog.Show();
            dialog.UpdateLayout();

            var detailsText = dialog.FindControl<TextBox>("DetailsText");
            Assert.NotNull(detailsText);
            Assert.Equal("Destination path already in use.", detailsText.Text);
        }
    }
}
