using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using Mfr.App.Ui.Services.FileList;
using Mfr.Utils;

namespace Mfr.App.Ui.ViewModels.FileList
{
    /// <summary>
    /// Cached image previews for Thumbnails view, loaded off the UI thread.
    /// </summary>
    internal sealed class FileListThumbnailSession : IDisposable
    {
        private const int _LoadParallelismCap = 4;

        private readonly Dictionary<string, IImage?> _pathToThumbnail = new(PathComparers.Os);
        private CancellationTokenSource? _loadCts;

        /// <summary>
        /// Returns a cached preview bitmap for <paramref name="path"/>, if one decoded successfully.
        /// </summary>
        /// <param name="path">Full filesystem path of a listed file.</param>
        /// <returns>The cached image, or <see langword="null"/> when none is ready.</returns>
        public IImage? TryGetCached(string path)
        {
            if (_pathToThumbnail.TryGetValue(path, out var cached) && cached is not null)
            {
                return cached;
            }

            return null;
        }

        /// <summary>
        /// Starts decoding previews for listed image files that are not already cached.
        /// </summary>
        /// <param name="entries">Visible File List rows.</param>
        public void BeginLoad(IEnumerable<FileListEntry> entries)
        {
            var pending = new List<FileListEntry>();
            foreach (var entry in entries)
            {
                if (entry.IsDirectory)
                {
                    continue;
                }

                if (_pathToThumbnail.ContainsKey(entry.FullPath))
                {
                    continue;
                }

                if (!ImageThumbnailLoader.CanLoad(entry.FullPath, entry.Length))
                {
                    continue;
                }

                pending.Add(entry);
            }

            if (pending.Count == 0)
            {
                return;
            }

            var cts = new CancellationTokenSource();
            _loadCts = cts;
            var token = cts.Token;
            var loadTask = _LoadAsync(pending, token);
            _ = loadTask.ContinueWith(
                static completed => completed.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default
            );
        }

        /// <summary>
        /// Stops an in-flight decode pass without discarding cached bitmaps.
        /// </summary>
        public void CancelLoad()
        {
            if (_loadCts is null)
            {
                return;
            }

            _loadCts.Cancel();
            _loadCts.Dispose();
            _loadCts = null;
        }

        /// <summary>
        /// Disposes cached bitmaps so the next Thumbnails pass decodes again.
        /// </summary>
        public void ClearCache()
        {
            foreach (var image in _pathToThumbnail.Values)
            {
                _DisposeImage(image);
            }

            _pathToThumbnail.Clear();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            CancelLoad();
            ClearCache();
        }

        private async Task _LoadAsync(IReadOnlyList<FileListEntry> pending, CancellationToken token)
        {
            var options = new ParallelOptions
            {
                CancellationToken = token,
                MaxDegreeOfParallelism = Math.Min(_LoadParallelismCap, Environment.ProcessorCount),
            };

            try
            {
                await Parallel.ForEachAsync(
                    pending,
                    options,
                    (entry, ct) =>
                    {
                        var thumbnail = ImageThumbnailLoader.TryLoad(entry.FullPath, entry.Length, ThumbnailSizes.Huge);
                        if (ct.IsCancellationRequested)
                        {
                            _DisposeImage(thumbnail);
                            return ValueTask.CompletedTask;
                        }

                        _PostToUi(() => _Apply(entry, thumbnail, ct));
                        return ValueTask.CompletedTask;
                    }
                );
            }
            catch (OperationCanceledException) { }
        }

        private void _Apply(FileListEntry entry, IImage? thumbnail, CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                _DisposeImage(thumbnail);
                return;
            }

            _pathToThumbnail[entry.FullPath] = thumbnail;
            if (thumbnail is not null)
            {
                entry.Icon = thumbnail;
            }
        }

        private static void _DisposeImage(IImage? image)
        {
            if (image is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        private static void _PostToUi(Action action)
        {
            if (Application.Current is null)
            {
                action();
                return;
            }

            Dispatcher.UIThread.Post(action);
        }
    }
}
