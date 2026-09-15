using System.Globalization;
using Mfr.Utils;

namespace Mfr.Models.RenameList
{
    /// <summary>
    /// Parses Rename List display / log-friendly strings for Extended WriteTargets.
    /// </summary>
    internal static class RenameListFieldParse
    {
        private const FileAttributes _RahsFlags =
            FileAttributes.ReadOnly | FileAttributes.Archive | FileAttributes.Hidden | FileAttributes.System;

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
        internal static FileAttributes ParseAttributes(string value, FileAttributes current)
        {
            ArgumentNullException.ThrowIfNull(value);

            var trimmed = value.Trim();
            if (trimmed.Length == 0)
            {
                throw new ArgumentException("Attributes value cannot be empty or whitespace.", nameof(value));
            }

            if (_TryParseRahs(trimmed, out var rahs))
            {
                return (current & ~_RahsFlags) | rahs;
            }

            if (
                Enum.TryParse<FileAttributes>(trimmed, ignoreCase: true, out var parsed) && _IsDefinedAttributes(parsed)
            )
            {
                return parsed;
            }

            throw new ArgumentException($"Unrecognized attributes value '{value}'.", nameof(value));
        }

        /// <summary>
        /// Parses a filesystem timestamp from grid display or rename-log round-trip form.
        /// </summary>
        /// <param name="value">
        /// Culture <c>G</c> display text, invariant round-trip <c>O</c>, or empty/whitespace for unset
        /// (<see cref="DateTime"/> default).
        /// </param>
        /// <returns>Parsed local/unspecified timestamp, or default when blank.</returns>
        /// <exception cref="ArgumentException"><paramref name="value"/> is non-blank but not a recognized date/time.</exception>
        internal static DateTime ParseFileDate(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (value.IsBlank())
            {
                return default;
            }

            var trimmed = value.Trim();
            if (
                DateTime.TryParseExact(
                    trimmed,
                    "O",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var roundTrip
                )
            )
            {
                return roundTrip;
            }

            if (DateTime.TryParse(trimmed, CultureInfo.CurrentCulture, DateTimeStyles.None, out var display))
            {
                return display;
            }

            if (
                DateTime.TryParse(
                    trimmed,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind,
                    out var invariant
                )
            )
            {
                return invariant;
            }

            throw new ArgumentException($"Unrecognized date/time value '{value}'.", nameof(value));
        }

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
