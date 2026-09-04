using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Tests for the shared Rename List row-error dialog copy formatting.
    /// </summary>
    public sealed class RenameListRowErrorDisplayTests
    {
        /// <summary>
        /// Verifies copy text includes summary, path, and details.
        /// </summary>
        [Fact]
        public void FormatCopyText_includes_summary_path_and_details()
        {
            var content = RenameListPreviewErrorDisplay.Create(@"D:\a.txt", "failed", "System.Exception: boom");
            var copy = RenameListRowErrorDisplay.FormatCopyText(content);
            Assert.Contains(content.Summary, copy, StringComparison.Ordinal);
            Assert.Contains(@"D:\a.txt", copy, StringComparison.Ordinal);
            Assert.Contains("failed", copy, StringComparison.Ordinal);
            Assert.Contains("boom", copy, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies a details block omits the technical line when it is empty.
        /// </summary>
        [Fact]
        public void FormatDetailsBlock_omits_blank_technical_line()
        {
            Assert.Equal("failed", RenameListRowErrorDisplay.FormatDetailsBlock("failed", technicalDetails: null));
            Assert.Equal("failed", RenameListRowErrorDisplay.FormatDetailsBlock("failed", "  "));
        }
    }
}
