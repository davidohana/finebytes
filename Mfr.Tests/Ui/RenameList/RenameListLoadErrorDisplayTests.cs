using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Tests for Rename List metadata load-error user-facing copy.
    /// </summary>
    public sealed class RenameListLoadErrorDisplayTests
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
            var content = new RenameListLoadErrorsDialogContent(@"D:\Music\PLAYLIST.M3U", [error]);

            var copyText = RenameListLoadErrorDisplay.FormatCopyText(content);

            Assert.Contains(RenameListLoadErrorDisplay.MetadataSummary, copyText, StringComparison.Ordinal);
            Assert.Contains(@"D:\Music\PLAYLIST.M3U", copyText, StringComparison.Ordinal);
            Assert.Contains(error.UserExplanation, copyText, StringComparison.Ordinal);
            Assert.Contains(technicalDetails, copyText, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies missing rows use a missing-path headline and omit the redundant path in details.
        /// </summary>
        [Fact]
        public void FormatSummary_and_details_for_missing_file()
        {
            const string path = @"D:\Music\1\Working on the Highway.mp3";
            var content = new RenameListLoadErrorsDialogContent(path, [RenameListDiskPaths.MissingLoadError(path)]);

            Assert.Equal(RenameListLoadErrorDisplay.MissingSummary, RenameListLoadErrorDisplay.FormatSummary(content));
            Assert.Equal(
                RenameListDiskPaths.MissingUserExplanation,
                RenameListLoadErrorDisplay.FormatDetailsText(content)
            );
        }

        /// <summary>
        /// Verifies missing-path copy is keyed off the structured flag, not explanation text.
        /// </summary>
        [Fact]
        public void FormatSummary_does_not_treat_matching_explanation_text_as_missing()
        {
            const string path = @"D:\Music\track.mp3";
            var content = new RenameListLoadErrorsDialogContent(
                path,
                [new RenameListLoadError(RenameListDiskPaths.MissingUserExplanation, path)]
            );

            Assert.Equal(RenameListLoadErrorDisplay.MetadataSummary, RenameListLoadErrorDisplay.FormatSummary(content));
        }

        /// <summary>
        /// Verifies the details box folds each friendly explanation with its technical line.
        /// </summary>
        [Fact]
        public void FormatDetailsText_folds_friendly_and_technical()
        {
            var content = new RenameListLoadErrorsDialogContent(
                @"D:\Music\info.htm",
                [
                    new RenameListLoadError("Could not read audio metadata.", "taglib/htm"),
                    new RenameListLoadError("Could not read image metadata.", "format unknown"),
                ]
            );

            var details = RenameListLoadErrorDisplay.FormatDetailsText(content);
            var expected = string.Join(
                $"{Environment.NewLine}{Environment.NewLine}",
                $"Could not read audio metadata.{Environment.NewLine}taglib/htm",
                $"Could not read image metadata.{Environment.NewLine}format unknown"
            );

            Assert.Equal(expected, details);
        }
    }
}
