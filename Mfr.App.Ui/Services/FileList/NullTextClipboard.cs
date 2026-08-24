namespace Mfr.App.Ui.Services.FileList
{
    /// <summary>
    /// Clipboard that discards text. Used in unit tests.
    /// </summary>
    public sealed class NullTextClipboard : ITextClipboard
    {
        /// <summary>
        /// Gets the shared instance.
        /// </summary>
        public static NullTextClipboard Instance { get; } = new();

        /// <inheritdoc />
        public Task SetTextAsync(string text)
        {
            return Task.CompletedTask;
        }
    }
}
