using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace Mfr.App.Ui.Services.FileList
{
    /// <summary>
    /// Clipboard backed by the desktop main window.
    /// </summary>
    public sealed class DesktopTextClipboard : ITextClipboard
    {
        /// <inheritdoc />
        public async Task SetTextAsync(string text)
        {
            if (
                Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop
                || desktop.MainWindow?.Clipboard is not { } clipboard
            )
            {
                return;
            }

            await clipboard.SetTextAsync(text).ConfigureAwait(true);
        }
    }
}
