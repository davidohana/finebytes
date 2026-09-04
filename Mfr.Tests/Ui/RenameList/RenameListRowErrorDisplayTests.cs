using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Tests for the shared Rename List row-error dialog copy formatting.
    /// </summary>
    public sealed class RenameListRowErrorDisplayTests
    {
        /// <summary>
        /// Verifies the primary details box joins path and user message with a blank line.
        /// </summary>
        [Fact]
        public void FormatPrimaryDetails_joins_path_and_user_message()
        {
            var text = RenameListRowErrorDisplay.FormatPrimaryDetails(@"D:\a.txt", "failed");
            Assert.Equal($"D:\\a.txt{Environment.NewLine}{Environment.NewLine}failed", text);
        }

        /// <summary>
        /// Verifies copy text includes summary, path, user message, and technical details.
        /// </summary>
        [Fact]
        public void FormatCopyText_includes_summary_path_user_message_and_technical()
        {
            var content = RenameListPreviewErrorDisplay.Create(@"D:\a.txt", "failed", "System.Exception: boom");
            var copy = RenameListRowErrorDisplay.FormatCopyText(content);
            Assert.Contains(content.Summary, copy, StringComparison.Ordinal);
            Assert.Contains(@"D:\a.txt", copy, StringComparison.Ordinal);
            Assert.Contains("failed", copy, StringComparison.Ordinal);
            Assert.Contains("boom", copy, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies exception formatting includes type, message, and stack without dumping ToString().
        /// </summary>
        [Fact]
        public void FormatExceptionDetails_includes_type_message_and_inner()
        {
            var inner = new InvalidOperationException("inner");
            var outer = new NotSupportedException("outer", inner);
            var details = RenameListRowErrorDisplay.FormatExceptionDetails(outer);

            Assert.NotNull(details);
            Assert.Contains("Type: System.NotSupportedException", details, StringComparison.Ordinal);
            Assert.Contains("Message: outer", details, StringComparison.Ordinal);
            Assert.Contains("Stack Trace:", details, StringComparison.Ordinal);
            Assert.Contains("Type: System.InvalidOperationException", details, StringComparison.Ordinal);
            Assert.Contains("Message: inner", details, StringComparison.Ordinal);
            Assert.Null(RenameListRowErrorDisplay.FormatExceptionDetails(null));
        }
    }
}
