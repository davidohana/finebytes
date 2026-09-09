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

        /// <summary>
        /// Whether <paramref name="value"/> contains a character illegal in Windows file names.
        /// </summary>
        /// <param name="value">Candidate file or folder name segment.</param>
        /// <returns><see langword="true"/> when any Windows-illegal character is present.</returns>
        public static bool ContainsInvalid(string value)
        {
            return value.AsSpan().IndexOfAny(s_invalidName) >= 0;
        }

        /// <summary>
        /// Whether <paramref name="path"/> contains a character illegal in Windows paths.
        /// </summary>
        /// <param name="path">Candidate absolute or relative path (may include separators and drive letters).</param>
        /// <returns><see langword="true"/> when any Windows-illegal path character is present.</returns>
        public static bool ContainsInvalidPath(string path)
        {
            return path.AsSpan().IndexOfAny(s_invalidPath) >= 0;
        }

        /// <summary>
        /// Adds Windows-illegal file-name characters to <paramref name="chars"/>.
        /// </summary>
        /// <param name="chars">Set to populate.</param>
        public static void AddInvalidTo(ISet<char> chars)
        {
            foreach (var c in s_invalidName)
            {
                chars.Add(c);
            }
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
