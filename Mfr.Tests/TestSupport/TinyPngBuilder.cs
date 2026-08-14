using System.IO.Compression;

namespace Mfr.Tests.TestSupport
{
    /// <summary>
    /// Builds tiny RGB PNG files (IHDR + optional pHYs) for image-property tests.
    /// </summary>
    internal static class TinyPngBuilder
    {
        /// <summary>
        /// Builds an 8-bit RGB PNG filled with red, optionally including a pHYs chunk.
        /// </summary>
        /// <param name="width">Pixel width.</param>
        /// <param name="height">Pixel height.</param>
        /// <param name="pixelsPerMetreX">pHYs X density; omitted when both densities are <c>0</c>.</param>
        /// <param name="pixelsPerMetreY">pHYs Y density; omitted when both densities are <c>0</c>.</param>
        /// <param name="physUnitIsMetre">pHYs unit specifier; <see langword="true"/> is metre.</param>
        /// <returns>Complete PNG file bytes.</returns>
        public static byte[] BuildRgb(
            int width,
            int height,
            int pixelsPerMetreX = 0,
            int pixelsPerMetreY = 0,
            bool physUnitIsMetre = true)
        {
            using var output = new MemoryStream();
            output.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

            var ihdr = new byte[13];
            _WriteUInt32Be(ihdr, 0, (uint)width);
            _WriteUInt32Be(ihdr, 4, (uint)height);
            ihdr[8] = 8;
            ihdr[9] = 2;
            _WriteChunk(output, "IHDR"u8, ihdr);

            var includePhys = pixelsPerMetreX != 0 || pixelsPerMetreY != 0;
            if (includePhys)
            {
                var phys = new byte[9];
                _WriteUInt32Be(phys, 0, (uint)pixelsPerMetreX);
                _WriteUInt32Be(phys, 4, (uint)pixelsPerMetreY);
                phys[8] = physUnitIsMetre ? (byte)1 : (byte)0;
                _WriteChunk(output, "pHYs"u8, phys);
            }

            _WriteChunk(output, "IDAT"u8, _DeflateScanlines(width, height));
            _WriteChunk(output, "IEND"u8, []);
            return output.ToArray();
        }

        private static byte[] _DeflateScanlines(int width, int height)
        {
            var raw = new byte[height * (1 + (width * 3))];
            var offset = 0;
            for (var y = 0; y < height; y++)
            {
                raw[offset++] = 0;
                for (var x = 0; x < width; x++)
                {
                    raw[offset++] = 255;
                    raw[offset++] = 0;
                    raw[offset++] = 0;
                }
            }

            using var compressed = new MemoryStream();
            using (var zlib = new ZLibStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
                zlib.Write(raw);

            return compressed.ToArray();
        }

        private static void _WriteChunk(Stream output, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
        {
            var length = new byte[4];
            _WriteUInt32Be(length, 0, (uint)data.Length);
            output.Write(length);
            output.Write(type);
            output.Write(data);

            var crcInput = new byte[type.Length + data.Length];
            type.CopyTo(crcInput);
            data.CopyTo(crcInput.AsSpan(type.Length));
            var crc = new byte[4];
            _WriteUInt32Be(crc, 0, _Crc32(crcInput));
            output.Write(crc);
        }

        private static void _WriteUInt32Be(byte[] buffer, int offset, uint value)
        {
            buffer[offset] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }

        private static uint _Crc32(ReadOnlySpan<byte> data)
        {
            var crc = 0xFFFFFFFF;
            foreach (var b in data)
            {
                crc ^= b;
                for (var i = 0; i < 8; i++)
                {
                    var mask = (crc & 1) == 0 ? 0u : 0xEDB88320;
                    crc = (crc >> 1) ^ mask;
                }
            }

            return crc ^ 0xFFFFFFFF;
        }
    }
}
