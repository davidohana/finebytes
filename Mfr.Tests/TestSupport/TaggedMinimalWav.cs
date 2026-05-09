namespace Mfr.Tests.TestSupport
{
    /// <summary>
    /// Builds a tiny PCM WAV fixture that TagLib can open for metadata round-trip assertions.
    /// </summary>
    internal static class TaggedMinimalWav
    {
        /// <summary>
        /// Canonical 48-byte WAV (mono 16-bit, 44100 Hz, 4 bytes PCM silence).
        /// </summary>
        private static readonly byte[] MinimalSilentWav =
        [
            0x52, 0x49, 0x46, 0x46, 0x28, 0x00, 0x00, 0x00,
            0x57, 0x41, 0x56, 0x45, 0x66, 0x6D, 0x74, 0x20,
            0x10, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00,
            0x44, 0xAC, 0x00, 0x00, 0x88, 0x58, 0x01, 0x00,
            0x02, 0x00, 0x10, 0x00, 0x64, 0x61, 0x74, 0x61,
            0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        ];

        /// <summary>
        /// Writes a minimal WAV to <paramref name="absolutePath"/> and applies string tag fields TagLib persists.
        /// </summary>
        /// <param name="absolutePath">Destination path (directories must exist).</param>
        /// <param name="title">Title tag.</param>
        /// <param name="album">Optional album tag; omit assignment when null or empty.</param>
        internal static void WriteTagged(string absolutePath, string title, string? album = null)
        {
            File.WriteAllBytes(absolutePath, MinimalSilentWav);

            using var file = TagLib.File.Create(absolutePath);
            file.Tag.Title = title;

            if (!string.IsNullOrEmpty(album))
                file.Tag.Album = album;

            file.Save();
        }
    }
}
