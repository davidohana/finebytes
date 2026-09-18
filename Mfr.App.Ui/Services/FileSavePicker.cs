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
        /// <param name="options">Dialog title, extension, and type filters.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Picked local path, or <see langword="null"/> when cancelled / unavailable.</returns>
        public static Task<string?> PickSaveFileAsync(
            Visual visual,
            SaveFilePickOptions options,
            CancellationToken cancellationToken = default
        )
        {
            var storage = _TryGetStorageProvider(visual);
            if (storage is null)
            {
                return Task.FromResult<string?>(null);
            }

            return PickSaveFileAsync(storage, options, cancellationToken);
        }

        /// <summary>
        /// Opens a save-file picker on <paramref name="storage"/>.
        /// </summary>
        /// <param name="storage">Avalonia storage provider.</param>
        /// <param name="options">Dialog title, extension, and type filters.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Picked local path, or <see langword="null"/> when cancelled / unavailable.</returns>
        public static async Task<string?> PickSaveFileAsync(
            IStorageProvider storage,
            SaveFilePickOptions options,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(storage);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentException.ThrowIfNullOrWhiteSpace(options.Title);
            ArgumentException.ThrowIfNullOrWhiteSpace(options.DefaultExtension);

            var fileTypeChoices = _BuildFileTypeChoices(options);

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            var file = await storage
                .SaveFilePickerAsync(
                    new FilePickerSaveOptions
                    {
                        Title = options.Title,
                        DefaultExtension = options.DefaultExtension,
                        SuggestedFileName = options.SuggestedFileName,
                        FileTypeChoices = fileTypeChoices,
                    }
                )
                .ConfigureAwait(true);

            return file?.TryGetLocalPath();
        }

        /// <summary>
        /// Resolves <see cref="IStorageProvider"/> from <paramref name="visual"/>'s top-level window.
        /// </summary>
        private static IStorageProvider? _TryGetStorageProvider(Visual visual)
        {
            ArgumentNullException.ThrowIfNull(visual);
            return TopLevel.GetTopLevel(visual)?.StorageProvider;
        }

        /// <summary>
        /// Builds Avalonia type filters from <paramref name="options"/>.
        /// </summary>
        private static FilePickerFileType[] _BuildFileTypeChoices(SaveFilePickOptions options)
        {
            if (options.FileTypes is { Count: > 0 } fileTypes)
            {
                var choices = new FilePickerFileType[fileTypes.Count];
                for (var i = 0; i < fileTypes.Count; i++)
                {
                    var type = fileTypes[i];
                    ArgumentException.ThrowIfNullOrWhiteSpace(type.Name);
                    if (type.Patterns is null || type.Patterns.Count == 0)
                    {
                        throw new ArgumentException(
                            $"Save file type '{type.Name}' requires at least one pattern.",
                            nameof(options)
                        );
                    }

                    choices[i] = new FilePickerFileType(type.Name) { Patterns = type.Patterns };
                }

                return choices;
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(options.FileTypeName);
            return
            [
                new FilePickerFileType(options.FileTypeName) { Patterns = [$"*.{options.DefaultExtension}"] },
                new FilePickerFileType("All files") { Patterns = ["*.*"] },
            ];
        }
    }
}
