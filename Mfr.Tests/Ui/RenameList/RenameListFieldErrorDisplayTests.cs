using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Models.RenameList;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Tests for Rename List field-load error user-facing copy.
    /// </summary>
    public sealed class RenameListFieldErrorDisplayTests
    {
        /// <summary>
        /// Verifies copy text includes summary, path, and folded friendly plus technical lines.
        /// </summary>
        [Fact]
        public void FormatCopyText_includes_summary_and_folded_errors()
        {
            var technicalDetails = @"D:\Music\PLAYLIST.M3U (taglib/m3u)";
            var error = new RenameListLoadError(
                "This file could not be read as audio or media metadata.",
                technicalDetails
            );
            var content = new RenameListFieldErrorDialogContent(@"D:\Music\PLAYLIST.M3U", [error]);

            var copyText = RenameListFieldErrorDisplay.FormatCopyText(content);

            Assert.Contains(RenameListFieldErrorDisplay.Summary, copyText, StringComparison.Ordinal);
            Assert.Contains(@"D:\Music\PLAYLIST.M3U", copyText, StringComparison.Ordinal);
            Assert.Contains(error.UserExplanation, copyText, StringComparison.Ordinal);
            Assert.Contains(technicalDetails, copyText, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies the details box folds each friendly explanation with its technical line.
        /// </summary>
        [Fact]
        public void FormatDetailsText_folds_friendly_and_technical()
        {
            var content = new RenameListFieldErrorDialogContent(
                @"D:\Music\info.htm",
                [
                    new RenameListLoadError("Could not read audio metadata.", "taglib/htm"),
                    new RenameListLoadError("Could not read image metadata.", "format unknown"),
                ]
            );

            var details = RenameListFieldErrorDisplay.FormatDetailsText(content);
            var expected = string.Join(
                $"{Environment.NewLine}{Environment.NewLine}",
                $"Could not read audio metadata.{Environment.NewLine}taglib/htm",
                $"Could not read image metadata.{Environment.NewLine}format unknown"
            );

            Assert.Equal(expected, details);
        }
    }
}
