namespace Mfr.Tests.TestSupport

{

    /// <summary>

    /// Applies tags to the committed PCM WAV scaffold from <see cref="MinimalWavFixture" />.

    /// </summary>

    internal static class TaggedMinimalWav

    {

        /// <summary>

        /// Copies <c>minimal-silent.wav</c> into <paramref name="absolutePath"/> and applies TagLib fields.

        /// </summary>

        /// <param name="absolutePath">Destination path (parent directories must exist).</param>

        /// <param name="title">Title tag.</param>

        /// <param name="album">Optional album tag; omit assignment when null or empty.</param>

        internal static void WriteTagged(string absolutePath, string title, string? album = null)

        {

            MinimalWavFixture.CopyScratchTo(absolutePath);



            using var file = TagLib.File.Create(absolutePath);

            file.Tag.Title = title;



            if (!string.IsNullOrEmpty(album))

                file.Tag.Album = album;



            file.Save();

        }

    }

}

