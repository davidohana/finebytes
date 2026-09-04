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
        /// Verifies load errors show path and user message in one box, with technical text collapsed.
        /// </summary>
        [AvaloniaFact]
        public void Dialog_shows_load_errors_with_collapsed_technical_details()
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
            var primaryDetailsText = dialog.FindControl<TextBox>("PrimaryDetailsText");
            var technicalExpander = dialog.FindControl<Expander>("TechnicalDetailsExpander");
            var technicalDetailsText = dialog.FindControl<TextBox>("TechnicalDetailsText");
            Assert.NotNull(summaryText);
            Assert.NotNull(primaryDetailsText);
            Assert.NotNull(technicalExpander);
            Assert.NotNull(technicalDetailsText);

            Assert.Equal(RenameListLoadErrorDisplay.DialogTitle, dialog.Title);
            Assert.Equal(RenameListLoadErrorDisplay.MetadataSummary, summaryText.Text);
            Assert.Contains(@"D:\Music\PLAYLIST.M3U", primaryDetailsText.Text, StringComparison.Ordinal);
            Assert.Contains("audio or media metadata", primaryDetailsText.Text, StringComparison.Ordinal);
            Assert.Contains("image or EXIF metadata", primaryDetailsText.Text, StringComparison.Ordinal);
            Assert.DoesNotContain(tagLibMessage, primaryDetailsText.Text, StringComparison.Ordinal);
            Assert.True(technicalExpander.IsVisible);
            Assert.False(technicalExpander.IsExpanded);
            Assert.Contains(tagLibMessage, technicalDetailsText.Text, StringComparison.Ordinal);
            Assert.Contains(imageMessage, technicalDetailsText.Text, StringComparison.Ordinal);
            Assert.NotNull(dialog.FindControl<Button>("CopyButton"));
        }

        /// <summary>
        /// Verifies missing rows show a missing-path headline and hide the technical expander.
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
            var primaryDetailsText = dialog.FindControl<TextBox>("PrimaryDetailsText");
            var technicalExpander = dialog.FindControl<Expander>("TechnicalDetailsExpander");
            Assert.NotNull(summaryText);
            Assert.NotNull(primaryDetailsText);
            Assert.NotNull(technicalExpander);

            Assert.Equal(RenameListLoadErrorDisplay.MissingSummary, summaryText.Text);
            Assert.Contains(path, primaryDetailsText.Text, StringComparison.Ordinal);
            Assert.Contains(
                RenameListDiskPaths.MissingUserExplanation,
                primaryDetailsText.Text,
                StringComparison.Ordinal
            );
            Assert.False(technicalExpander.IsVisible);
        }

        /// <summary>
        /// Verifies a preview failure shows path and message together and keeps technical text collapsed.
        /// </summary>
        [AvaloniaFact]
        public void Dialog_shows_preview_message_and_collapsed_technical_details()
        {
            var content = RenameListPreviewErrorDisplay.Create(
                @"D:\Music\album",
                "Cannot apply audio tags to a directory.",
                "Type: System.InvalidOperationException\nMessage: directory"
            );
            var dialog = new RenameListRowErrorDialog(content);
            dialog.Show();
            dialog.UpdateLayout();

            var summaryText = dialog.FindControl<TextBlock>("SummaryText");
            var primaryDetailsText = dialog.FindControl<TextBox>("PrimaryDetailsText");
            var technicalExpander = dialog.FindControl<Expander>("TechnicalDetailsExpander");
            var technicalDetailsText = dialog.FindControl<TextBox>("TechnicalDetailsText");
            Assert.NotNull(summaryText);
            Assert.NotNull(primaryDetailsText);
            Assert.NotNull(technicalExpander);
            Assert.NotNull(technicalDetailsText);

            Assert.Equal(RenameListPreviewErrorDisplay.DialogTitle, dialog.Title);
            Assert.Equal(RenameListPreviewErrorDisplay.Summary, summaryText.Text);
            Assert.Contains(@"D:\Music\album", primaryDetailsText.Text, StringComparison.Ordinal);
            Assert.Contains(
                "Cannot apply audio tags to a directory.",
                primaryDetailsText.Text,
                StringComparison.Ordinal
            );
            Assert.DoesNotContain("InvalidOperationException", primaryDetailsText.Text, StringComparison.Ordinal);
            Assert.True(technicalExpander.IsVisible);
            Assert.False(technicalExpander.IsExpanded);
            Assert.Contains("InvalidOperationException", technicalDetailsText.Text, StringComparison.Ordinal);
            Assert.NotNull(dialog.FindControl<Button>("CopyButton"));
        }

        /// <summary>
        /// Verifies the technical expander stays hidden when technical text is absent.
        /// </summary>
        [AvaloniaFact]
        public void Dialog_hides_technical_expander_without_technical_details()
        {
            var content = RenameListPreviewErrorDisplay.Create(
                @"D:\Music\note.txt",
                "Destination path already in use.",
                technicalDetails: null
            );
            var dialog = new RenameListRowErrorDialog(content);
            dialog.Show();
            dialog.UpdateLayout();

            var primaryDetailsText = dialog.FindControl<TextBox>("PrimaryDetailsText");
            var technicalExpander = dialog.FindControl<Expander>("TechnicalDetailsExpander");
            Assert.NotNull(primaryDetailsText);
            Assert.NotNull(technicalExpander);
            Assert.Contains(@"D:\Music\note.txt", primaryDetailsText.Text, StringComparison.Ordinal);
            Assert.Contains("Destination path already in use.", primaryDetailsText.Text, StringComparison.Ordinal);
            Assert.False(technicalExpander.IsVisible);
        }
    }
}
