using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Tests for Rename List metadata load-error user-facing copy.
    /// </summary>
    public sealed class RenameListLoadErrorDisplayTests
    {
        /// <summary>
        /// Verifies copy text includes summary, path, user message, and technical details.
        /// </summary>
        [Fact]
        public void Create_copy_text_includes_summary_user_message_and_technical()
        {
            var technicalDetails = @"D:\Music\PLAYLIST.M3U (taglib/m3u)";
            var error = new RenameListLoadError(
                "This file could not be read as audio or media metadata.",
                technicalDetails
            );
            var content = RenameListLoadErrorDisplay.Create(@"D:\Music\PLAYLIST.M3U", [error]);
            var copyText = RenameListRowErrorDisplay.FormatCopyText(content);

            Assert.Equal(RenameListLoadErrorDisplay.DialogTitle, content.Title);
            Assert.Contains(RenameListLoadErrorDisplay.MetadataSummary, copyText, StringComparison.Ordinal);
            Assert.Contains(@"D:\Music\PLAYLIST.M3U", copyText, StringComparison.Ordinal);
            Assert.Contains(error.UserExplanation, copyText, StringComparison.Ordinal);
            Assert.Contains(technicalDetails, copyText, StringComparison.Ordinal);
            Assert.Equal(error.UserExplanation, content.UserMessage);
            Assert.Equal(technicalDetails, content.TechnicalDetails);
        }

        /// <summary>
        /// Verifies missing rows use a missing-path headline and omit technical details.
        /// </summary>
        [Fact]
        public void FormatSummary_and_user_message_for_missing_file()
        {
            const string path = @"D:\Music\1\Working on the Highway.mp3";
            IReadOnlyList<RenameListLoadError> errors = [RenameListDiskPaths.MissingLoadError(path)];

            Assert.Equal(RenameListLoadErrorDisplay.MissingSummary, RenameListLoadErrorDisplay.FormatSummary(errors));
            Assert.Equal(
                RenameListDiskPaths.MissingUserExplanation,
                RenameListLoadErrorDisplay.FormatUserMessage(errors)
            );
            Assert.Null(RenameListLoadErrorDisplay.FormatTechnicalDetails(errors));
        }

        /// <summary>
        /// Verifies missing-path copy is keyed off the structured flag, not explanation text.
        /// </summary>
        [Fact]
        public void FormatSummary_does_not_treat_matching_explanation_text_as_missing()
        {
            const string path = @"D:\Music\track.mp3";
            IReadOnlyList<RenameListLoadError> errors =
            [
                new RenameListLoadError(RenameListDiskPaths.MissingUserExplanation, path),
            ];

            Assert.Equal(RenameListLoadErrorDisplay.MetadataSummary, RenameListLoadErrorDisplay.FormatSummary(errors));
        }

        /// <summary>
        /// Verifies user and technical text are kept separate for multiple reader failures.
        /// </summary>
        [Fact]
        public void FormatUserMessage_and_technical_are_separate()
        {
            IReadOnlyList<RenameListLoadError> errors =
            [
                new RenameListLoadError("Could not read audio metadata.", "taglib/htm"),
                new RenameListLoadError("Could not read image metadata.", "format unknown"),
            ];

            var userMessage = RenameListLoadErrorDisplay.FormatUserMessage(errors);
            var technical = RenameListLoadErrorDisplay.FormatTechnicalDetails(errors);
            var expectedUser = string.Join(
                $"{Environment.NewLine}{Environment.NewLine}",
                "Could not read audio metadata.",
                "Could not read image metadata."
            );
            var expectedTechnical = string.Join(
                $"{Environment.NewLine}{Environment.NewLine}",
                "taglib/htm",
                "format unknown"
            );

            Assert.Equal(expectedUser, userMessage);
            Assert.Equal(expectedTechnical, technical);
        }
    }
}
