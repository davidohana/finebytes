using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Tests for Show Preview Error dialog copy formatting.
    /// </summary>
    public sealed class RenameListPreviewErrorDisplayTests
    {
        /// <summary>
        /// Verifies copy text includes summary, path, and details.
        /// </summary>
        [Fact]
        public void FormatCopyText_includes_summary_path_and_details()
        {
            var content = new RenameListPreviewErrorDialogContent(@"D:\a.txt", "failed", "System.Exception: boom");
            var copy = RenameListPreviewErrorDisplay.FormatCopyText(content);
            Assert.Contains(RenameListPreviewErrorDisplay.Summary, copy, StringComparison.Ordinal);
            Assert.Contains(@"D:\a.txt", copy, StringComparison.Ordinal);
            Assert.Contains("failed", copy, StringComparison.Ordinal);
            Assert.Contains("boom", copy, StringComparison.Ordinal);
        }
    }
}
