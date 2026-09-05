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
        /// <summary>
        /// Characters that are illegal in Windows file names (control chars, reserved punctuation).
        /// </summary>
        public static char[] Invalid { get; } = _BuildInvalid();

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
