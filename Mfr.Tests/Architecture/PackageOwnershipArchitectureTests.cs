// Package ownership: docs/mfr-folder-layering.md (TagLib / MetadataExtractor stay in L2)

using System.Xml.Linq;

namespace Mfr.Tests.Architecture
{
    /// <summary>
    /// Verifies TagLib Sharp and MetadataExtractor packages stay in allowed projects.
    /// </summary>
    public sealed class PackageOwnershipArchitectureTests
    {
        private static readonly StringComparer _pathComparer = StringComparer.OrdinalIgnoreCase;

        private static readonly HashSet<string> _forbiddenMediaPackages = new(_pathComparer)
        {
            "TagLibSharp",
            "MetadataExtractor",
        };

        /// <summary>
        /// Production packages: only <c>Mfr.Metadata</c>. Tests may also reference TagLib for fixtures.
        /// </summary>
        private static readonly HashSet<string> _allowedPackageOwners = new(_pathComparer)
        {
            @"Mfr.Metadata\Mfr.Metadata.csproj",
            @"Mfr.Tests\Mfr.Tests.csproj",
        };

        /// <summary>
        /// No layered project outside Metadata (and Tests fixtures) may take a TagLib/ME PackageReference.
        /// </summary>
        [Fact]
        public void TagLib_And_MetadataExtractor_Only_In_Allowed_Projects()
        {
            var repoRoot = ArchitectureRepoPaths.FindRepoRoot();
            var violations = Directory
                .EnumerateFiles(repoRoot, "*.csproj", SearchOption.AllDirectories)
                .Where(path =>
                    !path.Contains(
                        $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                        StringComparison.Ordinal
                    )
                )
                .Select(path => (Path: path, Relative: _ToRepoRelative(repoRoot, path)))
                .SelectMany(entry =>
                    _LoadPackageReferenceIds(entry.Path)
                        .Where(id => _forbiddenMediaPackages.Contains(id))
                        .Where(_ => !_allowedPackageOwners.Contains(entry.Relative))
                        .Select(id => $"{entry.Relative}: PackageReference Include=\"{id}\"")
                )
                .ToList();

            Assert.True(
                violations.Count == 0,
                "TagLibSharp / MetadataExtractor must stay in Mfr.Metadata (and Mfr.Tests fixtures). Violations:"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, violations)
            );
        }

        private static IEnumerable<string> _LoadPackageReferenceIds(string projectFullPath)
        {
            var document = XDocument.Load(projectFullPath);
            return document
                .Descendants("PackageReference")
                .Select(reference => reference.Attribute("Include")?.Value)
                .Where(include => !string.IsNullOrWhiteSpace(include))
                .Select(include => include!);
        }

        private static string _ToRepoRelative(string repoRoot, string fullPath)
        {
            return Path.GetRelativePath(repoRoot, fullPath).Replace('/', '\\');
        }
    }
}
