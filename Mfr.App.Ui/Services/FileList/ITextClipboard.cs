namespace Mfr.App.Ui.Services.FileList
{
    /// <summary>
    /// Sets plain text on the system clipboard.
    /// </summary>
    public interface ITextClipboard
    {
        /// <summary>
        /// Replaces the clipboard contents with <paramref name="text"/>.
        /// </summary>
        /// <param name="text">Text to place on the clipboard.</param>
        /// <returns>A task that completes when the clipboard has been updated.</returns>
        Task SetTextAsync(string text);
    }
}
