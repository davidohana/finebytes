using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Mfr.Utils;

namespace Mfr.App.Ui.Services.FileList
{
    /// <summary>
    /// Writes and reads Explorer file clipboard payloads via Win32 CF_HDROP + Preferred DropEffect.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public sealed class WindowsFileClipboard : IFileClipboard
    {
        private const uint CfHdrop = 15;
        private const uint GmemMoveable = 0x0002;
        private const uint GmemZeroInit = 0x0040;
        private const uint Ghnd = GmemMoveable | GmemZeroInit;
        private const int DropEffectCopy = 1;
        private const int DropEffectMove = 2;

        private readonly HashSet<string> _cutPaths = new(PathComparers.Os);
        private readonly uint _preferredDropEffectFormat;

        /// <summary>
        /// Creates a Win32 file clipboard.
        /// </summary>
        public WindowsFileClipboard()
        {
            _preferredDropEffectFormat = NativeMethods.RegisterClipboardFormatW("Preferred DropEffect");
        }

        /// <inheritdoc />
        public IReadOnlySet<string> CutPaths => _cutPaths;

        /// <inheritdoc />
        public event EventHandler? Changed;

        /// <inheritdoc />
        public bool HasPasteableFiles => TryGetPaste(out _);

        /// <inheritdoc />
        public void SetCopy(IReadOnlyList<string> paths)
        {
            _SetFiles(paths, preferMove: false);
            _cutPaths.Clear();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc />
        public void SetCut(IReadOnlyList<string> paths)
        {
            _SetFiles(paths, preferMove: true);
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
            if (!NativeMethods.OpenClipboard(IntPtr.Zero))
            {
                return false;
            }

            try
            {
                var hDrop = NativeMethods.GetClipboardData(CfHdrop);
                if (hDrop == IntPtr.Zero)
                {
                    return false;
                }

                var paths = _ReadHdropPaths(hDrop);
                if (paths.Count == 0)
                {
                    return false;
                }

                var preferMove = _ReadPreferMove();
                paste = new FileClipboardPaste(paths, preferMove);
                return true;
            }
            finally
            {
                _ = NativeMethods.CloseClipboard();
            }
        }

        /// <inheritdoc />
        public void CompleteMovePaste()
        {
            var hadMarks = _cutPaths.Count > 0;
            _cutPaths.Clear();
            var emptiedClipboard = _TryEmptyClipboard();
            if (!hadMarks && !emptiedClipboard)
            {
                return;
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Empties the Win32 clipboard when it can be opened (post move-paste).
        /// </summary>
        private static bool _TryEmptyClipboard()
        {
            if (!NativeMethods.OpenClipboard(IntPtr.Zero))
            {
                return false;
            }

            try
            {
                return NativeMethods.EmptyClipboard();
            }
            finally
            {
                _ = NativeMethods.CloseClipboard();
            }
        }

        /// <summary>
        /// Writes CF_HDROP + Preferred DropEffect for <paramref name="paths"/>.
        /// <para>
        /// Cut requires Preferred DropEffect (Explorer treats CF_HDROP alone as Copy). If DropEffect
        /// cannot be set after CF_HDROP succeeds, the clipboard is emptied so Cut is not mislabeled.
        /// </para>
        /// </summary>
        private void _SetFiles(IReadOnlyList<string> paths, bool preferMove)
        {
            var writablePaths = paths.Where(static path => !string.IsNullOrWhiteSpace(path)).ToList();
            if (writablePaths.Count == 0)
            {
                throw new ArgumentException("At least one path is required.", nameof(paths));
            }

            if (preferMove && _preferredDropEffectFormat == 0)
            {
                throw new InvalidOperationException("Preferred DropEffect clipboard format is unavailable.");
            }

            var hDrop = _CreateHdrop(writablePaths);
            var hEffect =
                _preferredDropEffectFormat == 0
                    ? IntPtr.Zero
                    : _CreateDropEffect(preferMove ? DropEffectMove : DropEffectCopy);
            if (!NativeMethods.OpenClipboard(IntPtr.Zero))
            {
                _ = NativeMethods.GlobalFree(hDrop);
                if (hEffect != IntPtr.Zero)
                {
                    _ = NativeMethods.GlobalFree(hEffect);
                }

                throw new InvalidOperationException("Clipboard is unavailable.");
            }

            try
            {
                if (!NativeMethods.EmptyClipboard())
                {
                    throw new InvalidOperationException("Clipboard could not be cleared.");
                }

                if (NativeMethods.SetClipboardData(CfHdrop, hDrop) == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Clipboard file list could not be set.");
                }

                hDrop = IntPtr.Zero;
                if (hEffect != IntPtr.Zero)
                {
                    if (NativeMethods.SetClipboardData(_preferredDropEffectFormat, hEffect) == IntPtr.Zero)
                    {
                        _ = NativeMethods.EmptyClipboard();
                        throw new InvalidOperationException("Clipboard drop effect could not be set.");
                    }

                    hEffect = IntPtr.Zero;
                }
            }
            finally
            {
                _ = NativeMethods.CloseClipboard();
                if (hDrop != IntPtr.Zero)
                {
                    _ = NativeMethods.GlobalFree(hDrop);
                }

                if (hEffect != IntPtr.Zero)
                {
                    _ = NativeMethods.GlobalFree(hEffect);
                }
            }
        }

        /// <summary>
        /// Builds a movable HGLOBAL containing DROPFILES + double-null Unicode paths.
        /// </summary>
        private static IntPtr _CreateHdrop(IReadOnlyList<string> paths)
        {
            var pathBytes = 0;
            foreach (var path in paths)
            {
                pathBytes += (path.Length + 1) * sizeof(char);
            }

            pathBytes += sizeof(char);
            var headerSize = Marshal.SizeOf<DropFiles>();
            var totalSize = headerSize + pathBytes;
            var handle = NativeMethods.GlobalAlloc(Ghnd, (nuint)totalSize);
            if (handle == IntPtr.Zero)
            {
                throw new OutOfMemoryException("Could not allocate clipboard file list.");
            }

            var pointer = NativeMethods.GlobalLock(handle);
            if (pointer == IntPtr.Zero)
            {
                _ = NativeMethods.GlobalFree(handle);
                throw new InvalidOperationException("Could not lock clipboard file list.");
            }

            try
            {
                var header = new DropFiles
                {
                    pFiles = (uint)headerSize,
                    ptX = 0,
                    ptY = 0,
                    fNC = 0,
                    fWide = 1,
                };
                Marshal.StructureToPtr(header, pointer, fDeleteOld: false);

                var writeAt = IntPtr.Add(pointer, headerSize);
                foreach (var path in paths)
                {
                    var chars = path.ToCharArray();
                    Marshal.Copy(chars, 0, writeAt, chars.Length);
                    writeAt = IntPtr.Add(writeAt, chars.Length * sizeof(char));
                    Marshal.WriteInt16(writeAt, 0);
                    writeAt = IntPtr.Add(writeAt, sizeof(char));
                }

                Marshal.WriteInt16(writeAt, 0);
            }
            finally
            {
                _ = NativeMethods.GlobalUnlock(handle);
            }

            return handle;
        }

        /// <summary>
        /// Builds a movable HGLOBAL DWORD for Preferred DropEffect.
        /// </summary>
        private static IntPtr _CreateDropEffect(int dropEffect)
        {
            var handle = NativeMethods.GlobalAlloc(Ghnd, sizeof(int));
            if (handle == IntPtr.Zero)
            {
                throw new OutOfMemoryException("Could not allocate clipboard drop effect.");
            }

            var pointer = NativeMethods.GlobalLock(handle);
            if (pointer == IntPtr.Zero)
            {
                _ = NativeMethods.GlobalFree(handle);
                throw new InvalidOperationException("Could not lock clipboard drop effect.");
            }

            try
            {
                Marshal.WriteInt32(pointer, dropEffect);
            }
            finally
            {
                _ = NativeMethods.GlobalUnlock(handle);
            }

            return handle;
        }

        private static List<string> _ReadHdropPaths(IntPtr hDrop)
        {
            var count = NativeMethods.DragQueryFileW(hDrop, 0xFFFFFFFF, null, 0);
            if (count == 0)
            {
                return [];
            }

            var paths = new List<string>(checked((int)count));
            for (uint i = 0; i < count; i++)
            {
                var length = NativeMethods.DragQueryFileW(hDrop, i, null, 0);
                if (length == 0)
                {
                    continue;
                }

                var buffer = new char[length + 1];
                var written = NativeMethods.DragQueryFileW(hDrop, i, buffer, (uint)buffer.Length);
                if (written == 0)
                {
                    continue;
                }

                paths.Add(new string(buffer, 0, (int)written));
            }

            return paths;
        }

        private bool _ReadPreferMove()
        {
            if (_preferredDropEffectFormat == 0)
            {
                return false;
            }

            var handle = NativeMethods.GetClipboardData(_preferredDropEffectFormat);
            if (handle == IntPtr.Zero)
            {
                return false;
            }

            var pointer = NativeMethods.GlobalLock(handle);
            if (pointer == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                return Marshal.ReadInt32(pointer) == DropEffectMove;
            }
            finally
            {
                _ = NativeMethods.GlobalUnlock(handle);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DropFiles
        {
            public uint pFiles;
            public int ptX;
            public int ptY;
            public int fNC;
            public int fWide;
        }

        private static class NativeMethods
        {
            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool OpenClipboard(IntPtr hWndNewOwner);

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CloseClipboard();

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool EmptyClipboard();

            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr GetClipboardData(uint uFormat);

            [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern uint RegisterClipboardFormatW(string lpszFormat);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr GlobalAlloc(uint uFlags, nuint dwBytes);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr GlobalLock(IntPtr hMem);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GlobalUnlock(IntPtr hMem);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr GlobalFree(IntPtr hMem);

            [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
            public static extern uint DragQueryFileW(IntPtr hDrop, uint iFile, char[]? lpszFile, uint cch);
        }
    }
}
