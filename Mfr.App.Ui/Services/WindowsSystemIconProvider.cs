using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Mfr.App.Ui.Services
{
    /// <summary>
    /// Windows shell icons via <c>SHGetFileInfo</c>, cached by kind and file extension.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal sealed class WindowsSystemIconProvider : ISystemIconProvider
    {
        private const uint _ShgfiIcon = 0x100;
        private const uint _ShgfiSmallIcon = 0x1;
        private const uint _ShgfiUseFileAttributes = 0x10;
        private const uint _FileAttributeNormal = 0x80;
        private const uint _FileAttributeDirectory = 0x10;
        private const uint _DibRgbColors = 0;
        private const uint _BiRgb = 0;

        private readonly Dictionary<string, IImage?> _keyToIcon = new(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc />
        public IImage? GetSmallIcon(string path, bool isDirectory)
        {
            try
            {
                if (isDirectory)
                {
                    var isDrive = path.Length <= 3;
                    if (isDrive)
                        return _GetCached("drive:" + path, () => _ExtractIcon(path, _FileAttributeDirectory, useFileAttributes: false));

                    return _GetCached("dir", () => _ExtractIcon("folder", _FileAttributeDirectory, useFileAttributes: true));
                }

                var extension = Path.GetExtension(path);
                return _GetCached("file:" + extension, () => _ExtractIcon("file" + extension, _FileAttributeNormal, useFileAttributes: true));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or COMException)
            {
                return null;
            }
        }

        private IImage? _GetCached(string key, Func<IImage?> factory)
        {
            if (_keyToIcon.TryGetValue(key, out var icon))
                return icon;

            icon = factory();
            _keyToIcon[key] = icon;
            return icon;
        }

        private static WriteableBitmap? _ExtractIcon(string path, uint fileAttributes, bool useFileAttributes)
        {
            var flags = _ShgfiIcon | _ShgfiSmallIcon;
            if (useFileAttributes)
                flags |= _ShgfiUseFileAttributes;

            var info = new ShFileInfo();
            _ = NativeMethods.SHGetFileInfo(path, fileAttributes, ref info, (uint)Marshal.SizeOf<ShFileInfo>(), flags);
            if (info.hIcon == IntPtr.Zero)
                return null;

            try
            {
                return _HIconToBitmap(info.hIcon);
            }
            finally
            {
                _ = NativeMethods.DestroyIcon(info.hIcon);
            }
        }

        private static WriteableBitmap? _HIconToBitmap(IntPtr hIcon)
        {
            if (!NativeMethods.GetIconInfo(hIcon, out var iconInfo))
                return null;

            try
            {
                return _BitmapFromGdiHandle(iconInfo.hbmColor);
            }
            finally
            {
                if (iconInfo.hbmMask != IntPtr.Zero)
                    _ = NativeMethods.DeleteObject(iconInfo.hbmMask);
                if (iconInfo.hbmColor != IntPtr.Zero)
                    _ = NativeMethods.DeleteObject(iconInfo.hbmColor);
            }
        }

        private static WriteableBitmap? _BitmapFromGdiHandle(IntPtr hBitmap)
        {
            if (hBitmap == IntPtr.Zero)
                return null;

            var hdc = NativeMethods.GetDC(IntPtr.Zero);
            if (hdc == IntPtr.Zero)
                return null;

            try
            {
                var bmi = new BitmapInfo
                {
                    bmiHeader = new BitmapInfoHeader
                    {
                        biSize = (uint)Marshal.SizeOf<BitmapInfoHeader>(),
                    },
                };

                if (NativeMethods.GetDIBits(hdc, hBitmap, 0, 0, IntPtr.Zero, ref bmi, _DibRgbColors) == 0)
                    return null;

                var width = bmi.bmiHeader.biWidth;
                var height = Math.Abs(bmi.bmiHeader.biHeight);
                if (width <= 0 || height <= 0)
                    return null;

                bmi.bmiHeader.biBitCount = 32;
                bmi.bmiHeader.biCompression = _BiRgb;
                bmi.bmiHeader.biHeight = -height;
                bmi.bmiHeader.biPlanes = 1;
                bmi.bmiHeader.biSizeImage = (uint)(width * height * 4);

                var bitmap = new WriteableBitmap(
                    new PixelSize(width, height),
                    new Vector(96, 96),
                    PixelFormat.Bgra8888,
                    AlphaFormat.Unpremul);

                using (var framebuffer = bitmap.Lock())
                {
                    if (NativeMethods.GetDIBits(
                            hdc,
                            hBitmap,
                            0,
                            (uint)height,
                            framebuffer.Address,
                            ref bmi,
                            _DibRgbColors) == 0)
                    {
                        return null;
                    }
                }

                return bitmap;
            }
            finally
            {
                _ = NativeMethods.ReleaseDC(IntPtr.Zero, hdc);
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct ShFileInfo
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IconInfo
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BitmapInfoHeader
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BitmapInfo
        {
            public BitmapInfoHeader bmiHeader;
            public uint bmiColors;
        }

        private static class NativeMethods
        {
            [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
            public static extern IntPtr SHGetFileInfo(
                string pszPath,
                uint dwFileAttributes,
                ref ShFileInfo psfi,
                uint cbFileInfo,
                uint uFlags);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool DestroyIcon(IntPtr hIcon);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool GetIconInfo(IntPtr hIcon, out IconInfo piconinfo);

            [DllImport("gdi32.dll")]
            public static extern bool DeleteObject(IntPtr hObject);

            [DllImport("user32.dll")]
            public static extern IntPtr GetDC(IntPtr hWnd);

            [DllImport("user32.dll")]
            public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);

            [DllImport("gdi32.dll")]
            public static extern int GetDIBits(
                IntPtr hdc,
                IntPtr hbm,
                uint start,
                uint cLines,
                IntPtr lpvBits,
                ref BitmapInfo lpbmi,
                uint usage);
        }
    }
}
