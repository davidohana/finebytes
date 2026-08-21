namespace Mfr.App.Ui.Services.FileList
{
    /// <summary>
    /// Extracts an embedded JPEG thumbnail from a JPEG EXIF IFD1, if present.
    /// </summary>
    internal static class JpegExifThumbnailReader
    {
        private const int _MaxApp1Bytes = 1024 * 1024;
        private const byte _MarkerSoi = 0xD8;
        private const byte _MarkerEoi = 0xD9;
        private const byte _MarkerSos = 0xDA;
        private const byte _MarkerApp1 = 0xE1;
        private const ushort _TiffMagic = 42;
        private const ushort _TypeShort = 3;
        private const ushort _TypeLong = 4;
        private const ushort _TagCompression = 0x0103;
        private const ushort _TagJpegOffset = 0x0201;
        private const ushort _TagJpegLength = 0x0202;
        private const ushort _JpegCompression = 6;

        /// <summary>
        /// Tries to read the EXIF IFD1 JPEG thumbnail from the start of <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">A JPEG file stream positioned at the start, or seekable.</param>
        /// <returns>The embedded thumbnail JPEG bytes, or <see langword="null"/> when none is found.</returns>
        public static byte[]? TryExtract(Stream stream)
        {
            try
            {
                if (stream.CanSeek)
                    stream.Position = 0;

                return _TryExtract(stream);
            }
            catch (Exception ex)
                when (ex is IOException or EndOfStreamException or ArgumentException or NotSupportedException)
            {
                return null;
            }
        }

        private static byte[]? _TryExtract(Stream stream)
        {
            if (stream.ReadByte() != 0xFF)
                return null;
            if (stream.ReadByte() != _MarkerSoi)
                return null;

            while (true)
            {
                var marker = _ReadMarker(stream);
                if (marker is null)
                    return null;
                if (marker is _MarkerEoi or _MarkerSos)
                    return null;
                if (_IsStandalone(marker.Value))
                    continue;

                var segmentLength = _ReadUInt16BigEndian(stream);
                if (segmentLength < 2)
                    return null;

                var payloadLength = segmentLength - 2;
                if (marker != _MarkerApp1)
                {
                    _Skip(stream, payloadLength);
                    continue;
                }

                if (payloadLength > _MaxApp1Bytes)
                {
                    _Skip(stream, payloadLength);
                    continue;
                }

                var payload = _ReadExact(stream, payloadLength);
                var thumbnail = _TryReadExifThumbnail(payload);
                if (thumbnail is not null)
                    return thumbnail;
            }
        }

        private static byte[]? _TryReadExifThumbnail(ReadOnlySpan<byte> payload)
        {
            if (payload.Length < 14)
                return null;
            if (
                payload[0] != (byte)'E'
                || payload[1] != (byte)'x'
                || payload[2] != (byte)'i'
                || payload[3] != (byte)'f'
                || payload[4] != 0
                || payload[5] != 0
            )
                return null;

            var tiff = payload[6..];
            var isLittleEndian = tiff[0] == (byte)'I' && tiff[1] == (byte)'I';
            var isBigEndian = tiff[0] == (byte)'M' && tiff[1] == (byte)'M';
            if (!isLittleEndian && !isBigEndian)
                return null;
            if (_ReadUInt16(tiff, 2, isLittleEndian) != _TiffMagic)
                return null;

            var ifd0Offset = _ReadUInt32(tiff, 4, isLittleEndian);
            if (!_TryReadNextIfdOffset(tiff, ifd0Offset, isLittleEndian, out var ifd1Offset))
                return null;
            if (ifd1Offset == 0)
                return null;

            return _TryReadJpegFromIfd(tiff, ifd1Offset, isLittleEndian);
        }

        private static bool _TryReadNextIfdOffset(
            ReadOnlySpan<byte> tiff,
            uint ifdOffset,
            bool isLittleEndian,
            out uint nextIfdOffset
        )
        {
            nextIfdOffset = 0;
            if (!_TryToIndex(ifdOffset, tiff.Length, minRemaining: 2, out var index))
                return false;

            var entryCount = _ReadUInt16(tiff, index, isLittleEndian);
            var nextOffsetIndex = index + 2 + (entryCount * 12);
            if (nextOffsetIndex < 0 || nextOffsetIndex + 4 > tiff.Length)
                return false;

            nextIfdOffset = _ReadUInt32(tiff, nextOffsetIndex, isLittleEndian);
            return true;
        }

        private static byte[]? _TryReadJpegFromIfd(ReadOnlySpan<byte> tiff, uint ifdOffset, bool isLittleEndian)
        {
            if (!_TryToIndex(ifdOffset, tiff.Length, minRemaining: 2, out var index))
                return null;

            var entryCount = _ReadUInt16(tiff, index, isLittleEndian);
            uint? compression = null;
            uint? jpegOffset = null;
            uint? jpegLength = null;
            var entryIndex = index + 2;
            for (var i = 0; i < entryCount; i++)
            {
                if (entryIndex + 12 > tiff.Length)
                    return null;

                var tag = _ReadUInt16(tiff, entryIndex, isLittleEndian);
                if (
                    tag == _TagCompression
                    && _TryReadIfdUInt(tiff, entryIndex, isLittleEndian, out var compressionValue)
                )
                    compression = compressionValue;
                else if (
                    tag == _TagJpegOffset
                    && _TryReadIfdUInt(tiff, entryIndex, isLittleEndian, out var offsetValue)
                )
                    jpegOffset = offsetValue;
                else if (
                    tag == _TagJpegLength
                    && _TryReadIfdUInt(tiff, entryIndex, isLittleEndian, out var lengthValue)
                )
                    jpegLength = lengthValue;

                entryIndex += 12;
            }

            if (compression != _JpegCompression || jpegOffset is null || jpegLength is null or 0)
                return null;
            if (!_TryToIndex(jpegOffset.Value, tiff.Length, minRemaining: 0, out var jpegIndex))
                return null;
            if (jpegLength.Value > (uint)(tiff.Length - jpegIndex))
                return null;

            return tiff.Slice(jpegIndex, (int)jpegLength.Value).ToArray();
        }

        private static bool _TryReadIfdUInt(
            ReadOnlySpan<byte> tiff,
            int entryOffset,
            bool isLittleEndian,
            out uint value
        )
        {
            value = 0;
            var type = _ReadUInt16(tiff, entryOffset + 2, isLittleEndian);
            var count = _ReadUInt32(tiff, entryOffset + 4, isLittleEndian);
            if (count != 1)
                return false;

            var valueOffset = entryOffset + 8;
            if (type == _TypeShort)
            {
                value = _ReadUInt16(tiff, valueOffset, isLittleEndian);
                return true;
            }

            if (type == _TypeLong)
            {
                value = _ReadUInt32(tiff, valueOffset, isLittleEndian);
                return true;
            }

            return false;
        }

        private static byte? _ReadMarker(Stream stream)
        {
            int current;
            do
            {
                current = stream.ReadByte();
                if (current < 0)
                    return null;
            } while (current != 0xFF);

            do
            {
                current = stream.ReadByte();
                if (current < 0)
                    return null;
            } while (current == 0xFF);

            if (current == 0)
                return _ReadMarker(stream);

            return (byte)current;
        }

        private static bool _IsStandalone(byte marker)
        {
            return marker is (>= 0xD0 and <= 0xD9) or 0x01;
        }

        private static ushort _ReadUInt16BigEndian(Stream stream)
        {
            var high = stream.ReadByte();
            var low = stream.ReadByte();
            if (high < 0 || low < 0)
                throw new EndOfStreamException();

            return (ushort)((high << 8) | low);
        }

        private static byte[] _ReadExact(Stream stream, int count)
        {
            var buffer = new byte[count];
            var offset = 0;
            while (offset < count)
            {
                var read = stream.Read(buffer, offset, count - offset);
                if (read == 0)
                    throw new EndOfStreamException();

                offset += read;
            }

            return buffer;
        }

        private static void _Skip(Stream stream, int count)
        {
            if (stream.CanSeek)
            {
                stream.Seek(count, SeekOrigin.Current);
                return;
            }

            var remaining = count;
            var buffer = new byte[Math.Min(count, 4096)];
            while (remaining > 0)
            {
                var read = stream.Read(buffer, 0, Math.Min(buffer.Length, remaining));
                if (read == 0)
                    throw new EndOfStreamException();

                remaining -= read;
            }
        }

        private static bool _TryToIndex(uint offset, int length, int minRemaining, out int index)
        {
            index = 0;
            if (offset > int.MaxValue)
                return false;

            index = (int)offset;
            return index >= 0 && index <= length - minRemaining;
        }

        private static ushort _ReadUInt16(ReadOnlySpan<byte> data, int offset, bool isLittleEndian)
        {
            var value = (ushort)(data[offset] | (data[offset + 1] << 8));
            if (isLittleEndian)
                return value;

            return (ushort)((data[offset] << 8) | data[offset + 1]);
        }

        private static uint _ReadUInt32(ReadOnlySpan<byte> data, int offset, bool isLittleEndian)
        {
            if (isLittleEndian)
            {
                return data[offset]
                    | ((uint)data[offset + 1] << 8)
                    | ((uint)data[offset + 2] << 16)
                    | ((uint)data[offset + 3] << 24);
            }

            return ((uint)data[offset] << 24)
                | ((uint)data[offset + 1] << 16)
                | ((uint)data[offset + 2] << 8)
                | data[offset + 3];
        }
    }
}
