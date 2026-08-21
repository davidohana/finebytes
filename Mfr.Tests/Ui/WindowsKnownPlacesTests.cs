using Mfr.App.Ui.Services.FileExplorer;
using Mfr.Utils;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Tests the Windows This PC known-folder map.
    /// </summary>
    public sealed class WindowsKnownPlacesTests
    {
        /// <summary>
        /// Verifies Documents, Music, and Pictures resolve when those folders exist.
        /// </summary>
        [Fact]
        public void Resolves_Documents_Music_And_Pictures_On_Windows()
        {
            if (!OperatingSystem.IsWindows())
            {
                Assert.Empty(WindowsKnownPlaces.GetPlaces());
                Assert.False(WindowsKnownPlaces.TryResolveAlias("Documents", out _));
                return;
            }

            var documents = _ExistingSpecialFolder(Environment.SpecialFolder.MyDocuments);
            if (documents is not null)
            {
                Assert.True(WindowsKnownPlaces.TryResolveAlias("Documents", out var path));
                Assert.True(WindowsKnownPlaces.TryResolveAlias("documents", out _));
                Assert.True(PathRelations.IsSamePath(documents, path));
                Assert.True(WindowsKnownPlaces.TryGetPlace(documents, out var place));
                Assert.Equal("Documents", place.Name);
            }

            var music = _ExistingSpecialFolder(Environment.SpecialFolder.MyMusic);
            if (music is not null)
            {
                Assert.True(WindowsKnownPlaces.TryResolveAlias("Music", out var path));
                Assert.True(PathRelations.IsSamePath(music, path));
            }

            var pictures = _ExistingSpecialFolder(Environment.SpecialFolder.MyPictures);
            if (pictures is not null)
            {
                Assert.True(WindowsKnownPlaces.TryResolveAlias("Pictures", out var path));
                Assert.True(PathRelations.IsSamePath(pictures, path));
            }

            Assert.False(WindowsKnownPlaces.TryResolveAlias("Control Panel", out _));
        }

        /// <summary>
        /// Verifies a nested folder is contained by its known place.
        /// </summary>
        [Fact]
        public void ContainingPlace_Uses_Longest_Known_Prefix()
        {
            if (!OperatingSystem.IsWindows())
                return;

            var documents = _ExistingSpecialFolder(Environment.SpecialFolder.MyDocuments);
            if (documents is null)
                return;

            var nested = Path.Combine(documents, "Work");
            Assert.True(WindowsKnownPlaces.TryGetContainingPlace(nested, out var place));
            Assert.Equal("Documents", place.Name);
            Assert.False(WindowsKnownPlaces.TryGetPlace(nested, out _));
        }

        private static string? _ExistingSpecialFolder(Environment.SpecialFolder folder)
        {
            var path = Environment.GetFolderPath(folder);
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                return null;

            return new DirectoryInfo(path).FullName;
        }
    }
}
