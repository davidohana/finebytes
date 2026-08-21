using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Mfr.App.Ui.Services.FileList
{
    /// <summary>
    /// Windows shell icons via <c>SHGetFileInfo</c> and <c>SHGetImageList</c>, cached by kind, size, and extension.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal sealed class WindowsSystemIconProvider : ISystemIconProvider
    {
        private const uint _ShgfiIcon = 0x100;
        private const uint _ShgfiSmallIcon = 0x1;
        private const uint _ShgfiLargeIcon = 0x0;
        private const uint _ShgfiSysIconIndex = 0x4000;
        private const uint _ShgfiUseFileAttributes = 0x10;
        private const uint _FileAttributeNormal = 0x80;
        private const uint _FileAttributeDirectory = 0x10;
        private const uint _DibRgbColors = 0;
        private const uint _BiRgb = 0;
        private const int _ShilExtraLarge = 2;
        private const int _ShilJumbo = 4;
        private const int _IldTransparent = 1;
        private const int _JumboPixels = 256;
        private const int _ExtraLargePixels = 48;
        private const int _SOk = 0;

        private static readonly Guid _IidIImageList = new("46EB5926-582E-4017-9FDF-E8998DAA0950");
        private static readonly Dictionary<int, IImageList?> _shilToImageList = [];
        private static readonly Lock _imageListGate = new();

        private readonly Dictionary<string, IImage?> _keyToIcon = new(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc />
        public IImage? GetIcon(string path, bool isDirectory, ShellIconSize size)
        {
            var sizeKey = size switch
            {
                ShellIconSize.Small => "small",
                ShellIconSize.Jumbo => "jumbo",
                ShellIconSize.Large => throw new NotImplementedException(),
                _ => "large",
            };
            try
            {
                if (isDirectory)
                {
                    var isDrive = path.Length <= 3;
                    if (isDrive)
                    {
                        return _GetCached(
                            "drive:" + path + ":" + sizeKey,
                            () => _ExtractIcon(path, _FileAttributeDirectory, useFileAttributes: false, size)
                        );
                    }

                    return _GetCached(
                        "dir:" + sizeKey,
                        () => _ExtractIcon("folder", _FileAttributeDirectory, useFileAttributes: true, size)
                    );
                }

                var extension = Path.GetExtension(path);
                return _GetCached(
                    "file:" + extension + ":" + sizeKey,
                    () => _ExtractIcon("file" + extension, _FileAttributeNormal, useFileAttributes: true, size)
                );
            }
            catch (Exception ex)
                when (ex is IOException or UnauthorizedAccessException or ArgumentException or COMException)
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

        private static WriteableBitmap? _ExtractIcon(
            string path,
            uint fileAttributes,
            bool useFileAttributes,
            ShellIconSize size
        )
        {
            if (size == ShellIconSize.Jumbo)
            {
                var jumbo = _ExtractFromImageList(path, fileAttributes, useFileAttributes, _ShilJumbo);
                // Jumbo is 256px; missing glyphs are a small icon on an empty canvas.
                if (jumbo is not null && _HasJumboContent(jumbo))
                    return jumbo;

                jumbo?.Dispose();
                var extraLarge = _ExtractFromImageList(path, fileAttributes, useFileAttributes, _ShilExtraLarge);
                if (extraLarge is not null)
                    return extraLarge;
            }

            var flags = _ShgfiIcon | (size == ShellIconSize.Small ? _ShgfiSmallIcon : _ShgfiLargeIcon);
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

        private static WriteableBitmap? _ExtractFromImageList(
            string path,
            uint fileAttributes,
            bool useFileAttributes,
            int shil
        )
        {
            var iconIndex = _GetSysIconIndex(path, fileAttributes, useFileAttributes);
            if (iconIndex < 0)
                return null;

            var imageList = _GetImageList(shil);
            if (imageList is null)
                return null;

            var hr = imageList.GetIcon(iconIndex, _IldTransparent, out var hIcon);
            if (hr != _SOk || hIcon == IntPtr.Zero)
                return null;

            try
            {
                return _HIconToBitmap(hIcon);
            }
            finally
            {
                _ = NativeMethods.DestroyIcon(hIcon);
            }
        }

        private static int _GetSysIconIndex(string path, uint fileAttributes, bool useFileAttributes)
        {
            var flags = _ShgfiSysIconIndex;
            if (useFileAttributes)
                flags |= _ShgfiUseFileAttributes;

            var info = new ShFileInfo();
            var result = NativeMethods.SHGetFileInfo(
                path,
                fileAttributes,
                ref info,
                (uint)Marshal.SizeOf<ShFileInfo>(),
                flags
            );
            if (result == IntPtr.Zero)
                return -1;

            return info.iIcon;
        }

        private static IImageList? _GetImageList(int shil)
        {
            lock (_imageListGate)
            {
                if (_shilToImageList.TryGetValue(shil, out var cached))
                    return cached;

                var iid = _IidIImageList;
                var hr = NativeMethods.SHGetImageList(shil, ref iid, out var imageList);
                if (hr != _SOk)
                    imageList = null;

                _shilToImageList[shil] = imageList;
                return imageList;
            }
        }

        private static bool _HasJumboContent(WriteableBitmap bitmap)
        {
            var width = bitmap.PixelSize.Width;
            var height = bitmap.PixelSize.Height;
            var isSmallerThanJumbo = width < _JumboPixels || height < _JumboPixels;
            if (isSmallerThanJumbo)
                return width > _ExtraLargePixels || height > _ExtraLargePixels;

            var marginX = (width - _ExtraLargePixels) / 2;
            var marginY = (height - _ExtraLargePixels) / 2;
            using var framebuffer = bitmap.Lock();
            var rowBytes = framebuffer.RowBytes;
            var buffer = new byte[rowBytes * height];
            Marshal.Copy(framebuffer.Address, buffer, 0, buffer.Length);

            for (var y = 0; y < height; y++)
            {
                var rowIsInCenter = y >= marginY && y < height - marginY;
                var rowOffset = y * rowBytes;
                for (var x = 0; x < width; x++)
                {
                    var pixelIsInCenter = rowIsInCenter && x >= marginX && x < width - marginX;
                    if (pixelIsInCenter)
                        continue;

                    var alpha = buffer[rowOffset + (x * 4) + 3];
                    if (alpha != 0)
                        return true;
                }
            }

            return false;
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
                    bmiHeader = new BitmapInfoHeader { biSize = (uint)Marshal.SizeOf<BitmapInfoHeader>() },
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
                    AlphaFormat.Unpremul
                );

                using (var framebuffer = bitmap.Lock())
                {
                    if (
                        NativeMethods.GetDIBits(
                            hdc,
                            hBitmap,
                            0,
                            (uint)height,
                            framebuffer.Address,
                            ref bmi,
                            _DibRgbColors
                        ) == 0
                    )
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

        [ComImport]
        [Guid("46EB5926-582E-4017-9FDF-E8998DAA0950")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IImageList
        {
            [PreserveSig]
            int Add(IntPtr hbmImage, IntPtr hbmMask, ref int pi);

            [PreserveSig]
            int ReplaceIcon(int i, IntPtr hicon, ref int pi);

            [PreserveSig]
            int SetOverlayImage(int iImage, int iOverlay);

            [PreserveSig]
            int Replace(int i, IntPtr hbmImage, IntPtr hbmMask);

            [PreserveSig]
            int AddMasked(IntPtr hbmImage, int crMask, ref int pi);

            [PreserveSig]
            int Draw(IntPtr pimldp);

            [PreserveSig]
            int Remove(int i);

            [PreserveSig]
            int GetIcon(int i, int flags, out IntPtr picon);
        }

        private static class NativeMethods
        {
            [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
            public static extern IntPtr SHGetFileInfo(
                string pszPath,
                uint dwFileAttributes,
                ref ShFileInfo psfi,
                uint cbFileInfo,
                uint uFlags
            );

            [DllImport("shell32.dll")]
            public static extern int SHGetImageList(int iImageList, ref Guid riid, out IImageList ppv);

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
                uint usage
            );
        }
    }
}
