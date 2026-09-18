// Models friend assemblies: docs/mfr-folder-layering.md

using System.Xml.Linq;

namespace Mfr.Tests.Architecture
{
    /// <summary>
    /// Verifies <c>Mfr.Models</c> <c>InternalsVisibleTo</c> friends stay Engine / Filters / Tests only.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Metadata maps TagLib/ME to public Models DTOs and must not regain Models friend access.
    /// App.Ui must not be friended — keep <c>RenameItem</c> / <c>BaseFilter</c> internals out of the UI.
    /// </para>
    /// </remarks>
    public sealed class ModelsFriendAssemblyArchitectureTests
    {
        private static readonly StringComparer _NameComparer = StringComparer.Ordinal;

        private static readonly HashSet<string> _AllowedFriends = new(_NameComparer)
        {
            "Mfr.Engine",
            "Mfr.Filters",
            "Mfr.Tests",
        };

        /// <summary>
        /// <c>Mfr.Models.csproj</c> friends must be exactly Engine, Filters, and Tests.
        /// </summary>
        [Fact]
        public void Models_InternalsVisibleTo_Only_Engine_Filters_Tests()
        {
            var repoRoot = ArchitectureRepoPaths.FindRepoRoot();
            var projectPath = Path.Combine(repoRoot, "Mfr.Models", "Mfr.Models.csproj");
            Assert.True(File.Exists(projectPath), $"Expected Models project at '{projectPath}'.");

            var friends = _LoadInternalsVisibleTo(projectPath).OrderBy(name => name, _NameComparer).ToList();
            var expected = _AllowedFriends.OrderBy(name => name, _NameComparer).ToList();

            Assert.Equal(expected, friends);
        }

        private static List<string> _LoadInternalsVisibleTo(string projectFullPath)
        {
            var document = XDocument.Load(projectFullPath);
            return
            [
                .. document
                    .Descendants("InternalsVisibleTo")
                    .Select(element => element.Attribute("Include")?.Value)
                    .Where(include => !string.IsNullOrWhiteSpace(include))
                    .Select(include => include!),
            ];
        }
    }
}
