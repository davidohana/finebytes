using TagLib;
using TagLib.Riff;

namespace Mfr.Tests.TestSupport
{
    /// <summary>
    /// Applies tags to the committed PCM WAV scaffold from <see cref="MinimalWavFixture" />.
    /// </summary>
    internal static class TaggedMinimalWav
    {
        /// <summary>
        /// Copies <c>minimal-silent.wav</c> into <paramref name="absolutePath"/> and writes RIFF INFO chunks.
        /// </summary>
        /// <param name="absolutePath">Destination path (parent directories must exist).</param>
        /// <param name="title">Title tag, written as <c>INAM</c>.</param>
        /// <param name="album">Optional album tag, written as <c>IPRD</c>; omitted when null or empty.</param>
        internal static void WriteTagged(string absolutePath, string title, string? album = null)
        {
            MinimalWavFixture.CopyScratchTo(absolutePath);

            using var file = TagLib.File.Create(absolutePath);
            var info = (InfoTag)file.GetTag(TagTypes.RiffInfo, true);
            info.SetValue("INAM", title);

            if (!string.IsNullOrEmpty(album))
            {
                info.SetValue("IPRD", album);
            }

            file.Save();
        }
    }
}
