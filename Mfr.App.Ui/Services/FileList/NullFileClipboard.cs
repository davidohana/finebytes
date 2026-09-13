using Mfr.Utils;

namespace Mfr.App.Ui.Services.FileList
{
    /// <summary>
    /// In-memory file clipboard for tests and platforms without Win32 file clipboard.
    /// <para>
    /// Tracks cut marks for ghosting; does not expose a pasteable OS payload.
    /// </para>
    /// </summary>
    public sealed class NullFileClipboard : IFileClipboard
    {
        private readonly HashSet<string> _cutPaths = new(PathComparers.Os);

        /// <summary>
        /// Gets the shared instance.
        /// </summary>
        public static NullFileClipboard Instance { get; } = new();

        /// <inheritdoc />
        public IReadOnlySet<string> CutPaths => _cutPaths;

        /// <inheritdoc />
        public event EventHandler? Changed;

        /// <inheritdoc />
        public bool HasPasteableFiles => false;

        /// <inheritdoc />
        public void SetCopy(IReadOnlyList<string> paths)
        {
            _ = paths;
            if (_cutPaths.Count == 0)
            {
                return;
            }

            _cutPaths.Clear();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc />
        public void SetCut(IReadOnlyList<string> paths)
        {
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
            paste = new FileClipboardPaste([], PreferMove: false);
            return false;
        }
    }
}
