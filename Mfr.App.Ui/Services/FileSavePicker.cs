using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Mfr.App.Ui.Services
{
    /// <summary>
    /// Opens a save-file system picker via Avalonia <see cref="IStorageProvider"/>.
    /// </summary>
    public static class FileSavePicker
    {
        /// <summary>
        /// Opens a save-file picker owned by <paramref name="visual"/>'s top-level window.
        /// </summary>
        /// <param name="visual">Control used to resolve <see cref="TopLevel.StorageProvider"/>.</param>
        /// <param name="title">Dialog title.</param>
        /// <param name="defaultExtension">Extension without dot (e.g. <c>txt</c>).</param>
        /// <param name="suggestedFileName">Optional suggested file name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Picked local path, or <see langword="null"/> when cancelled / unavailable.</returns>
        public static Task<string?> PickSaveFileAsync(
            Visual visual,
            string title = "Save as",
            string defaultExtension = "txt",
            string? suggestedFileName = null,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(visual);
            var storage = TopLevel.GetTopLevel(visual)?.StorageProvider;
            if (storage is null)
            {
                return Task.FromResult<string?>(null);
            }

            return PickSaveFileAsync(storage, title, defaultExtension, suggestedFileName, cancellationToken);
        }

        /// <summary>
        /// Opens a save-file picker on <paramref name="storage"/>.
        /// </summary>
        /// <param name="storage">Avalonia storage provider.</param>
        /// <param name="title">Dialog title.</param>
        /// <param name="defaultExtension">Extension without dot (e.g. <c>txt</c>).</param>
        /// <param name="suggestedFileName">Optional suggested file name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Picked local path, or <see langword="null"/> when cancelled / unavailable.</returns>
        public static async Task<string?> PickSaveFileAsync(
            IStorageProvider storage,
            string title = "Save as",
            string defaultExtension = "txt",
            string? suggestedFileName = null,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(storage);
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            ArgumentException.ThrowIfNullOrWhiteSpace(defaultExtension);

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            var file = await storage
                .SaveFilePickerAsync(
                    new FilePickerSaveOptions
                    {
                        Title = title,
                        DefaultExtension = defaultExtension,
                        SuggestedFileName = suggestedFileName,
                        FileTypeChoices =
                        [
                            new FilePickerFileType("Text files") { Patterns = [$"*.{defaultExtension}"] },
                            new FilePickerFileType("All files") { Patterns = ["*.*"] },
                        ],
                    }
                )
                .ConfigureAwait(true);

            return file?.TryGetLocalPath();
        }
    }
}
