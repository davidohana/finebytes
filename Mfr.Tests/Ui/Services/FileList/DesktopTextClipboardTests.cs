using Avalonia.Headless.XUnit;
using Mfr.App.Ui.Services.FileList;

namespace Mfr.Tests.Ui.Services.FileList
{
    /// <summary>
    /// Tests <see cref="DesktopTextClipboard"/> when the main window clipboard is missing.
    /// </summary>
    public sealed class DesktopTextClipboardTests
    {
        /// <summary>
        /// Verifies SetText throws when no desktop main-window clipboard is available.
        /// </summary>
        [AvaloniaFact]
        public async Task SetTextAsync_Throws_When_Main_Window_Clipboard_Missing()
        {
            var clipboard = new DesktopTextClipboard();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => clipboard.SetTextAsync("path"));

            Assert.Equal("Clipboard is unavailable.", ex.Message);
        }
    }
}
