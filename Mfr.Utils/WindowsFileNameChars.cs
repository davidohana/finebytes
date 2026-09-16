namespace Mfr.Utils
{
    /// <summary>
    /// Windows file-name and path character rules used by rename logic regardless of host OS.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Magic File Renamer targets Windows filesystems. Validation and cleanup must use Windows illegal
    /// characters even when unit tests run on Linux CI, so host
    /// <see cref="Path.GetInvalidFileNameChars"/> / <see cref="Path.GetInvalidPathChars"/> are not used here.
    /// </para>
    /// </remarks>
    public static class WindowsFileNameChars
    {
        private static readonly char[] s_invalidName = _BuildInvalidName();
        private static readonly char[] s_invalidPath = _BuildInvalidPath();
        private static readonly HashSet<char> s_invalidNameSet = [.. s_invalidName];

        /// <summary>
        /// Whether <paramref name="value"/> contains a character illegal in Windows file names.
        /// </summary>
        /// <param name="value">Candidate file or folder name segment.</param>
        /// <returns><see langword="true"/> when any Windows-illegal character is present.</returns>
        public static bool ContainsInvalid(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return value.AsSpan().IndexOfAny(s_invalidName) >= 0;
        }

        /// <summary>
        /// Distinct Windows-illegal file-name characters in <paramref name="value"/>, in first-seen order.
        /// </summary>
        /// <param name="value">Candidate file or folder name segment.</param>
        /// <returns>Illegal characters found; empty when none.</returns>
        public static IReadOnlyList<char> FindInvalid(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            List<char>? found = null;
            HashSet<char>? seen = null;
            foreach (var c in value)
            {
                if (!s_invalidNameSet.Contains(c))
                {
                    continue;
                }

                seen ??= [];
                if (!seen.Add(c))
                {
                    continue;
                }

                found ??= [];
                found.Add(c);
            }

            return found is null ? [] : found;
        }

        /// <summary>
        /// Formats illegal characters from <see cref="FindInvalid"/> for error messages.
        /// </summary>
        /// <param name="invalidChars">Illegal characters (typically from <see cref="FindInvalid"/>).</param>
        /// <returns>Display text such as <c>':'</c> or <c>':' '*'</c>; empty when <paramref name="invalidChars"/> is empty.</returns>
        public static string FormatInvalidForMessage(IReadOnlyList<char> invalidChars)
        {
            ArgumentNullException.ThrowIfNull(invalidChars);

            if (invalidChars.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(' ', invalidChars.Select(_FormatCharForMessage));
        }

        /// <summary>
        /// Whether <paramref name="path"/> contains a character illegal in Windows paths.
        /// </summary>
        /// <param name="path">Candidate absolute or relative path (may include separators and drive letters).</param>
        /// <returns><see langword="true"/> when any Windows-illegal path character is present.</returns>
        public static bool ContainsInvalidPath(string path)
        {
            ArgumentNullException.ThrowIfNull(path);
            return path.AsSpan().IndexOfAny(s_invalidPath) >= 0;
        }

        /// <summary>
        /// Adds Windows-illegal file-name characters to <paramref name="chars"/>.
        /// </summary>
        /// <param name="chars">Set to populate.</param>
        public static void AddInvalidTo(ISet<char> chars)
        {
            ArgumentNullException.ThrowIfNull(chars);

            foreach (var c in s_invalidName)
            {
                chars.Add(c);
            }
        }

        private static string _FormatCharForMessage(char c)
        {
            if (char.IsControl(c))
            {
                return $"U+{(int)c:X4}";
            }

            return $"'{c}'";
        }

        private static char[] _BuildInvalidName()
        {
            // Matches Windows Path.GetInvalidFileNameChars(): U+0000–U+001F plus "<>:|?*\/.
            var chars = new char[32 + 9];
            for (var i = 0; i < 32; i++)
            {
                chars[i] = (char)i;
            }

            "\"<>:|?*\\/".CopyTo(0, chars, 32, 9);
            return chars;
        }

        private static char[] _BuildInvalidPath()
        {
            // Matches Windows Path.GetInvalidPathChars(): U+0000–U+001F plus "<>|.
            var chars = new char[32 + 4];
            for (var i = 0; i < 32; i++)
            {
                chars[i] = (char)i;
            }

            "\"<>|".CopyTo(0, chars, 32, 4);
            return chars;
        }
    }
}
