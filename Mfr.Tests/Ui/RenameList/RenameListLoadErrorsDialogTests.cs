using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.App.Ui.Views.RenameList;
using Mfr.Models.RenameList;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Headless tests for the Rename List Show Load Errors dialog.
    /// </summary>
    public sealed class RenameListLoadErrorsDialogTests
    {
        /// <summary>
        /// Verifies the dialog shows a single copyable box with folded friendly and technical text.
        /// </summary>
        [AvaloniaFact]
        public void Dialog_shows_all_row_errors_and_one_details_box()
        {
            var tagLibMessage = @"D:\Music\PLAYLIST.M3U (taglib/m3u)";
            var imageMessage = "Cannot read image properties";
            var content = new RenameListLoadErrorsDialogContent(
                @"D:\Music\PLAYLIST.M3U",
                [
                    new RenameListLoadError("This file could not be read as audio or media metadata.", tagLibMessage),
                    new RenameListLoadError("This file could not be read as image or EXIF metadata.", imageMessage),
                ]
            );
            var dialog = new RenameListLoadErrorsDialog(content);
            dialog.Show();
            dialog.UpdateLayout();

            var summaryText = dialog.FindControl<TextBlock>("SummaryText");
            var filePathText = dialog.FindControl<TextBlock>("FilePathText");
            var detailsText = dialog.FindControl<TextBox>("DetailsText");
            Assert.NotNull(summaryText);
            Assert.NotNull(filePathText);
            Assert.NotNull(detailsText);

            Assert.Equal("Error", dialog.Title);
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
            var content = new RenameListLoadErrorsDialogContent(
                path,
                [new RenameListLoadError(RenameListDiskPaths.MissingUserExplanation, path)]
            );
            var dialog = new RenameListLoadErrorsDialog(content);
            dialog.Show();
            dialog.UpdateLayout();

            var summaryText = dialog.FindControl<TextBlock>("SummaryText");
            var detailsText = dialog.FindControl<TextBox>("DetailsText");
            Assert.NotNull(summaryText);
            Assert.NotNull(detailsText);

            Assert.Equal(RenameListLoadErrorDisplay.MissingSummary, summaryText.Text);
            Assert.Equal(RenameListDiskPaths.MissingUserExplanation, detailsText.Text);
        }
    }
}
