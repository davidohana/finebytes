namespace Mfr.Models.RenameList
{
    /// <summary>
    /// MFR7 RAHS attribute display string and WriteTarget / log-friendly parse.
    /// </summary>
    internal static class FileAttributesRahs
    {
        /// <summary>
        /// Read-only, Archive, Hidden, and System bits (MFR7 RAHS set).
        /// </summary>
        internal const FileAttributes Mask =
            FileAttributes.ReadOnly | FileAttributes.Archive | FileAttributes.Hidden | FileAttributes.System;

        /// <summary>
        /// Formats the MFR7 RAHS attribute string for grid display.
        /// </summary>
        /// <param name="attributes">Filesystem attributes from scan metadata.</param>
        /// <returns>Four-character <c>R/A/H/S</c> or dash flags.</returns>
        internal static string Format(FileAttributes attributes)
        {
            return string.Concat(
                _FormatFlag(attributes, FileAttributes.ReadOnly, 'R'),
                _FormatFlag(attributes, FileAttributes.Archive, 'A'),
                _FormatFlag(attributes, FileAttributes.Hidden, 'H'),
                _FormatFlag(attributes, FileAttributes.System, 'S')
            );
        }

        /// <summary>
        /// Parses an attributes string into flags, merging RAHS display form onto <paramref name="current"/>.
        /// </summary>
        /// <param name="value">
        /// Four-character <c>R/A/H/S</c> or dash display (e.g. <c>RA--</c>), or a
        /// <see cref="FileAttributes"/> enum name string from rename logs.
        /// </param>
        /// <param name="current">Existing attributes; non-RAHS bits are preserved for display-form writes.</param>
        /// <returns>Updated attributes.</returns>
        /// <exception cref="ArgumentException"><paramref name="value"/> is not a recognized attributes form.</exception>
        internal static FileAttributes Parse(string value, FileAttributes current)
        {
            ArgumentNullException.ThrowIfNull(value);

            var trimmed = value.Trim();
            if (trimmed.Length == 0)
            {
                throw new ArgumentException("Attributes value cannot be empty or whitespace.", nameof(value));
            }

            if (_TryParseRahs(trimmed, out var rahs))
            {
                return (current & ~Mask) | rahs;
            }

            if (
                Enum.TryParse<FileAttributes>(trimmed, ignoreCase: true, out var parsed) && _IsDefinedAttributes(parsed)
            )
            {
                return parsed;
            }

            throw new ArgumentException($"Unrecognized attributes value '{value}'.", nameof(value));
        }

        private static char _FormatFlag(FileAttributes attributes, FileAttributes flag, char letter)
        {
            return attributes.HasFlag(flag) ? letter : '-';
        }

        /// <summary>
        /// Tries the four-character RAHS / dash display form.
        /// </summary>
        private static bool _TryParseRahs(string value, out FileAttributes attributes)
        {
            attributes = default;
            if (value.Length != 4)
            {
                return false;
            }

            if (!_TryParseRahsFlag(value[0], 'R', FileAttributes.ReadOnly, out var readOnly))
            {
                return false;
            }

            if (!_TryParseRahsFlag(value[1], 'A', FileAttributes.Archive, out var archive))
            {
                return false;
            }

            if (!_TryParseRahsFlag(value[2], 'H', FileAttributes.Hidden, out var hidden))
            {
                return false;
            }

            if (!_TryParseRahsFlag(value[3], 'S', FileAttributes.System, out var system))
            {
                return false;
            }

            attributes = readOnly | archive | hidden | system;
            return true;
        }

        private static bool _TryParseRahsFlag(char text, char letter, FileAttributes flag, out FileAttributes value)
        {
            if (text == letter || text == char.ToLowerInvariant(letter))
            {
                value = flag;
                return true;
            }

            if (text == '-')
            {
                value = default;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Rejects numeric leftovers that <see cref="Enum.TryParse{TEnum}(string,bool,out TEnum)"/> accepts for flags enums.
        /// </summary>
        private static bool _IsDefinedAttributes(FileAttributes attributes)
        {
            // Allow any combination of known flag bits; reject garbage like "99999" that TryParse still maps.
            const FileAttributes known =
                FileAttributes.ReadOnly
                | FileAttributes.Hidden
                | FileAttributes.System
                | FileAttributes.Directory
                | FileAttributes.Archive
                | FileAttributes.Device
                | FileAttributes.Normal
                | FileAttributes.Temporary
                | FileAttributes.SparseFile
                | FileAttributes.ReparsePoint
                | FileAttributes.Compressed
                | FileAttributes.Offline
                | FileAttributes.NotContentIndexed
                | FileAttributes.Encrypted
                | FileAttributes.IntegrityStream
                | FileAttributes.NoScrubData;

            return (attributes & ~known) == 0;
        }
    }
}
