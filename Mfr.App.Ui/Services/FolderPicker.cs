using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Mfr.App.Ui.Services
{
    /// <summary>
    /// Opens a single-folder system picker via Avalonia <see cref="IStorageProvider"/>.
    /// </summary>
    public static class FolderPicker
    {
        /// <summary>
        /// Opens a folder picker owned by <paramref name="visual"/>'s top-level window.
        /// </summary>
        /// <param name="visual">Control used to resolve <see cref="TopLevel.StorageProvider"/>.</param>
        /// <param name="suggestedStartPath">Optional local path for the dialog start location.</param>
        /// <param name="title">Dialog title.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Picked local path, or <see langword="null"/> when cancelled / unavailable.</returns>
        public static Task<string?> PickFolderAsync(
            Visual visual,
            string? suggestedStartPath = null,
            string title = "Select folder",
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(visual);
            var storage = TopLevel.GetTopLevel(visual)?.StorageProvider;
            if (storage is null)
            {
                return Task.FromResult<string?>(null);
            }

            return PickFolderAsync(storage, suggestedStartPath, title, cancellationToken);
        }

        /// <summary>
        /// Opens a folder picker on <paramref name="storage"/>.
        /// </summary>
        /// <param name="storage">Avalonia storage provider.</param>
        /// <param name="suggestedStartPath">Optional local path for the dialog start location.</param>
        /// <param name="title">Dialog title.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Picked local path, or <see langword="null"/> when cancelled / unavailable.</returns>
        public static async Task<string?> PickFolderAsync(
            IStorageProvider storage,
            string? suggestedStartPath = null,
            string title = "Select folder",
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(storage);
            ArgumentException.ThrowIfNullOrWhiteSpace(title);

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            IStorageFolder? startLocation = null;
            if (!string.IsNullOrWhiteSpace(suggestedStartPath))
            {
                startLocation = await storage.TryGetFolderFromPathAsync(suggestedStartPath).ConfigureAwait(true);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            var folders = await storage
                .OpenFolderPickerAsync(
                    new FolderPickerOpenOptions
                    {
                        Title = title,
                        AllowMultiple = false,
                        SuggestedStartLocation = startLocation,
                    }
                )
                .ConfigureAwait(true);

            if (folders.Count == 0)
            {
                return null;
            }

            return folders[0].TryGetLocalPath();
        }
    }
}
