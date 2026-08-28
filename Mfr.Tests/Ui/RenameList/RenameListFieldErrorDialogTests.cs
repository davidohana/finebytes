using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.App.Ui.Views.RenameList;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Headless tests for the Rename List Show Field Error dialog.
    /// </summary>
    public sealed class RenameListFieldErrorDialogTests
    {
        /// <summary>
        /// Verifies the dialog explains the failure and exposes copyable raw details.
        /// </summary>
        [AvaloniaFact]
        public void Dialog_shows_explanation_and_copyable_details()
        {
            var message = @"D:\Music\General\UVWXYZ\U2 - 1991 - Achtung Baby\info.htm (taglib/mht)";
            var userExplanation = "This file could not be read as audio or media metadata.";
            var content = new RenameListFieldErrorDialogContent("Title", userExplanation, message);
            var dialog = new RenameListFieldErrorDialog(content);
            dialog.Show();
            dialog.UpdateLayout();

            var explanationText = dialog.FindControl<TextBlock>("ExplanationText");
            var detailsText = dialog.FindControl<TextBox>("DetailsText");
            Assert.NotNull(explanationText);
            Assert.NotNull(detailsText);

            Assert.Equal("Error", dialog.Title);
            Assert.Equal(userExplanation, explanationText.Text);
            Assert.Equal(message, detailsText.Text);
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
    }
}
