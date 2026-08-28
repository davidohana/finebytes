using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Tests for Rename List field-load error user-facing copy.
    /// </summary>
    public sealed class RenameListFieldErrorDisplayTests
    {
        /// <summary>
        /// Verifies copy text includes summary, field, explanation, and technical details.
        /// </summary>
        [Fact]
        public void FormatCopyText_includes_summary_and_details()
        {
            var technicalDetails = @"D:\Music\PLAYLIST.M3U (taglib/m3u)";
            var content = new RenameListFieldErrorDialogContent(
                "Album Artists",
                "This file could not be read as audio or media metadata.",
                technicalDetails
            );

            var copyText = RenameListFieldErrorDisplay.FormatCopyText(content);

            Assert.Contains(RenameListFieldErrorDisplay.Summary, copyText, StringComparison.Ordinal);
            Assert.Contains("Field: Album Artists", copyText, StringComparison.Ordinal);
            Assert.Contains(content.UserExplanation, copyText, StringComparison.Ordinal);
            Assert.Contains("Technical details:", copyText, StringComparison.Ordinal);
            Assert.Contains(technicalDetails, copyText, StringComparison.Ordinal);
        }
    }
}
