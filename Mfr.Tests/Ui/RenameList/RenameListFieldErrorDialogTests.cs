using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.App.Ui.Views.RenameList;
using Mfr.Models.RenameList;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Headless tests for the Rename List Show Field Error dialog.
    /// </summary>
    public sealed class RenameListFieldErrorDialogTests
    {
        /// <summary>
        /// Verifies the dialog shows a single copyable box with folded friendly and technical text.
        /// </summary>
        [AvaloniaFact]
        public void Dialog_shows_all_row_errors_and_one_details_box()
        {
            var tagLibMessage = @"D:\Music\PLAYLIST.M3U (taglib/m3u)";
            var imageMessage = "Cannot read image properties";
            var content = new RenameListFieldErrorDialogContent(
                @"D:\Music\PLAYLIST.M3U",
                [
                    new RenameListLoadError(
                        "This file could not be read as audio or media metadata.",
                        tagLibMessage
                    ),
                    new RenameListLoadError(
                        "This file could not be read as image or EXIF metadata.",
                        imageMessage
                    ),
                ]
            );
            var dialog = new RenameListFieldErrorDialog(content);
            dialog.Show();
            dialog.UpdateLayout();

            var filePathText = dialog.FindControl<TextBlock>("FilePathText");
            var detailsText = dialog.FindControl<TextBox>("DetailsText");
            Assert.NotNull(filePathText);
            Assert.NotNull(detailsText);

            Assert.Equal("Error", dialog.Title);
            Assert.Equal(@"D:\Music\PLAYLIST.M3U", filePathText.Text);
            Assert.Contains(tagLibMessage, detailsText.Text, StringComparison.Ordinal);
            Assert.Contains(imageMessage, detailsText.Text, StringComparison.Ordinal);
            Assert.Contains("audio or media metadata", detailsText.Text, StringComparison.Ordinal);
            Assert.Contains("image or EXIF metadata", detailsText.Text, StringComparison.Ordinal);
            Assert.True(detailsText.IsReadOnly);
            Assert.NotNull(dialog.FindControl<Button>("CopyButton"));
        }

        /// <summary>
        /// Verifies error foreground is MFR7 gray and is not applied to normal cells via transparent brush.
        /// </summary>
        [Fact]
        public void ErrorBrush_is_gray_and_not_used_for_healthy_cells()
        {
            var brush = Assert.IsType<Avalonia.Media.SolidColorBrush>(RenameListFieldForegroundConverter.ErrorBrush);
            Assert.Equal(Avalonia.Media.Color.Parse("#808080"), brush.Color);
            Assert.Equal(
                Avalonia.Media.Brushes.Transparent,
                RenameListFieldForegroundConverter.Instance.Convert(null, typeof(Avalonia.Media.IBrush), null, null!)
            );
        }

        /// <summary>
        /// Verifies recycled cells drop gray when they no longer show Error.
        /// </summary>
        [AvaloniaFact]
        public void ApplyFromCellText_clears_gray_when_text_is_not_Error()
        {
            var textBlock = new TextBlock { Text = RenameListFieldCatalog.FieldLoadErrorText };
            RenameListFieldForegroundConverter.ApplyFromCellText(textBlock);
            Assert.Same(RenameListFieldForegroundConverter.ErrorBrush, textBlock.Foreground);

            textBlock.Text = "Zero 7";
            RenameListFieldForegroundConverter.ApplyFromCellText(textBlock);
            Assert.NotSame(RenameListFieldForegroundConverter.ErrorBrush, textBlock.Foreground);
        }
    }
}
