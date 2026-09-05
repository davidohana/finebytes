namespace Mfr.Utils
{
    /// <summary>
    /// Windows file-name character rules used by rename filters regardless of host OS.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Magic File Renamer targets Windows filesystems. Filter cleanup must use Windows illegal
    /// characters even when unit tests run on Linux CI, so <see cref="Path.GetInvalidFileNameChars"/>
    /// (host OS) is not used here.
    /// </para>
    /// </remarks>
    public static class WindowsFileNameChars
    {
        private static readonly char[] s_invalid = _BuildInvalid();

        /// <summary>
        /// Whether <paramref name="value"/> contains a character illegal in Windows file names.
        /// </summary>
        /// <param name="value">Candidate file or folder name segment.</param>
        /// <returns><see langword="true"/> when any Windows-illegal character is present.</returns>
        public static bool ContainsInvalid(string value)
        {
            return value.AsSpan().IndexOfAny(s_invalid) >= 0;
        }

        /// <summary>
        /// Adds Windows-illegal file-name characters to <paramref name="chars"/>.
        /// </summary>
        /// <param name="chars">Set to populate.</param>
        public static void AddInvalidTo(ISet<char> chars)
        {
            foreach (var c in s_invalid)
            {
                chars.Add(c);
            }
        }

        private static char[] _BuildInvalid()
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
    }
}
