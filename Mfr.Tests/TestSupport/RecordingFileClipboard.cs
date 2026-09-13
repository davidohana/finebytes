using Mfr.App.Ui.Services.FileList;
using Mfr.Utils;

namespace Mfr.Tests.TestSupport
{
    /// <summary>
    /// Records <see cref="IFileClipboard"/> calls and tracks cut marks for UI tests.
    /// </summary>
    public sealed class RecordingFileClipboard : IFileClipboard
    {
        private readonly HashSet<string> _cutPaths = new(PathComparers.Os);
        private FileClipboardPaste? _paste;

        /// <summary>
        /// Gets SetCopy calls in order.
        /// </summary>
        public List<IReadOnlyList<string>> Copies { get; } = [];

        /// <summary>
        /// Gets SetCut calls in order.
        /// </summary>
        public List<IReadOnlyList<string>> Cuts { get; } = [];

        /// <summary>
        /// Gets or sets an exception thrown from SetCopy / SetCut when not null.
        /// </summary>
        public Exception? ExceptionToThrow { get; set; }

        /// <inheritdoc />
        public IReadOnlySet<string> CutPaths => _cutPaths;

        /// <inheritdoc />
        public event EventHandler? Changed;

        /// <inheritdoc />
        public bool HasPasteableFiles => _paste is { Paths.Count: > 0 };

        /// <inheritdoc />
        public void SetCopy(IReadOnlyList<string> paths)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            Copies.Add([.. paths]);
            _paste = new FileClipboardPaste([.. paths], PreferMove: false);
            var cleared = _cutPaths.Count > 0;
            _cutPaths.Clear();
            if (cleared || paths.Count > 0)
            {
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <inheritdoc />
        public void SetCut(IReadOnlyList<string> paths)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            Cuts.Add([.. paths]);
            _paste = new FileClipboardPaste([.. paths], PreferMove: true);
            _cutPaths.Clear();
            foreach (var path in paths)
            {
                if (!string.IsNullOrWhiteSpace(path))
                {
                    _cutPaths.Add(path);
                }
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc />
        public bool TryGetPaste(out FileClipboardPaste paste)
        {
            if (_paste is { Paths.Count: > 0 })
            {
                paste = _paste;
                return true;
            }

            paste = new FileClipboardPaste([], PreferMove: false);
            return false;
        }

        /// <inheritdoc />
        public void CompleteMovePaste()
        {
            var hadMarks = _cutPaths.Count > 0;
            _cutPaths.Clear();
            var clearedCutPaste = false;
            if (_paste is { PreferMove: true })
            {
                _paste = null;
                clearedCutPaste = true;
            }

            if (!hadMarks && !clearedCutPaste)
            {
                return;
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Seeds a pasteable payload without writing SetCopy / SetCut (Explorer-style inbound paste).
        /// </summary>
        /// <param name="paths">Filesystem paths to expose via TryGetPaste.</param>
        /// <param name="preferMove">Whether Preferred DropEffect is Move.</param>
        public void SeedPaste(IReadOnlyList<string> paths, bool preferMove)
        {
            _paste = paths.Count > 0 ? new FileClipboardPaste([.. paths], preferMove) : null;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
