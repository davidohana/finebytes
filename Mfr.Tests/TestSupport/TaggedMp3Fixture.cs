using TagLib;

namespace Mfr.Tests.TestSupport
{
    /// <summary>
    /// Copies the MPEG fixture and applies ID3 tags, so tests can exercise ID3v1/ID3v2 coexistence.
    /// </summary>
    internal static class TaggedMp3Fixture
    {
        private const string _FixtureFileName = "l3-compl-cut.mp3";

        /// <summary>
        /// Copies the MPEG fixture to <paramref name="absolutePath"/> and writes the requested ID3 titles.
        /// </summary>
        /// <param name="absolutePath">Destination path (parent directories must exist).</param>
        /// <param name="id3v1Title">Title for the ID3v1 trailer; omit to leave the trailer absent.</param>
        /// <param name="id3v2Title">Title for the ID3v2 tag; omit to leave that tag absent.</param>
        internal static void WriteTagged(string absolutePath, string? id3v1Title = null, string? id3v2Title = null)
        {
            var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", _FixtureFileName);
            System.IO.File.Copy(fixturePath, absolutePath, overwrite: true);

            using var file = TagLib.File.Create(absolutePath);

            if (id3v2Title is not null)
                ((TagLib.Id3v2.Tag)file.GetTag(TagTypes.Id3v2, true)).Title = id3v2Title;

            if (id3v1Title is not null)
                ((TagLib.Id3v1.Tag)file.GetTag(TagTypes.Id3v1, true)).Title = id3v1Title;

            file.Save();
        }
    }
}
