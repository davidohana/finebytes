// Layering rules: docs/mfr-folder-layering.md

using System.Xml.Linq;

namespace Mfr.Tests.Architecture
{
    /// <summary>
    /// Verifies <c>.csproj</c> references follow <c>docs/mfr-folder-layering.md</c>.
    /// </summary>
    /// <remarks>
    /// <para>Layers (from that doc):</para>
    /// <list type="bullet">
    /// <item>L4 App — <c>Mfr.App.Cli</c></item>
    /// <item>L3 Application — <c>Mfr.Core</c></item>
    /// <item>L2 Domain rules — <c>Mfr.Filters</c></item>
    /// <item>L1 Domain model — <c>Mfr.Models</c></item>
    /// <item>L0 Shared utilities — <c>Mfr.Utils</c></item>
    /// </list>
    /// <para>Supporting host <c>Mfr</c> sits above L4; supporting tests <c>Mfr.Tests</c> are checked separately.</para>
    /// <para>
    /// Each project may reference any combination of projects in strictly lower layers (see doc).
    /// </para>
    /// </remarks>
    public sealed class ProjectReferenceArchitectureTests
    {
        private static readonly StringComparer _pathComparer = StringComparer.OrdinalIgnoreCase;

        /// <summary>
        /// Relative repo path → layer number (higher = closer to entry/host). References must target strictly lower layers.
        /// </summary>
        private static readonly Dictionary<string, int> _projectRelativePathToLayer = new(_pathComparer)
        {
            [@"Mfr.Utils\Mfr.Utils.csproj"] = 0,
            [@"Mfr.Models\Mfr.Models.csproj"] = 1,
            [@"Mfr.Filters\Mfr.Filters.csproj"] = 2,
            [@"Mfr.Core\Mfr.Core.csproj"] = 3,
            [@"Mfr.App.Cli\Mfr.App.Cli.csproj"] = 4,
            [@"Mfr\Mfr.csproj"] = 5,
        };

        private static readonly string[] _testsProjectExpectedReferences = [@"Mfr\Mfr.csproj"];

        /// <summary>
        /// Layered stack + host: every reference must point at a known project in a strictly lower layer.
        /// </summary>
        [Fact]
        public void LayeredProjects_OnlyReferenceStrictlyLowerLayers()
        {
            var repoRoot = _FindRepoRoot();
            foreach (var (projectPath, layer) in _projectRelativePathToLayer.OrderByDescending(kv => kv.Value))
            {
                var refs = _LoadProjectReferencePaths(repoRoot, projectPath);
                foreach (var referenced in refs)
                {
                    var found = _projectRelativePathToLayer.TryGetValue(referenced, out var referencedLayer);
                    Assert.True(
                        found,
                        $"Project '{projectPath}' references unknown project '{referenced}'. Add it to the layer map or fix the path.");
                    Assert.True(
                        referencedLayer < layer,
                        $"Layer violation: '{projectPath}' (layer {layer}) must not reference '{referenced}' (layer {referencedLayer}).");
                }
            }
        }

        /// <summary>
        /// Supporting tests assembly references only the host (<c>Mfr</c>), per <c>docs/mfr-folder-layering.md</c>.
        /// </summary>
        [Fact]
        public void TestsProject_ReferencesSupportingHostOnly()
        {
            var repoRoot = _FindRepoRoot();
            var refs = _LoadProjectReferencePaths(repoRoot, @"Mfr.Tests\Mfr.Tests.csproj");
            Assert.Equal(
                expected: [.. _testsProjectExpectedReferences.OrderBy(p => p, _pathComparer)],
                actual: [.. refs.OrderBy(p => p, _pathComparer)],
                comparer: _pathComparer);
        }

        private static List<string> _LoadProjectReferencePaths(string repoRoot, string projectPathFromRepoRoot)
        {
            var projectFullPath = Path.Combine(repoRoot, projectPathFromRepoRoot);
            var document = XDocument.Load(projectFullPath);
            var projectDirectory = Path.GetDirectoryName(projectFullPath)
                ?? throw new InvalidOperationException($"Could not resolve directory for '{projectFullPath}'.");

            return
            [
                .. document
                    .Descendants("ProjectReference")
                    .Select(reference => reference.Attribute("Include")?.Value)
                    .Where(include => !string.IsNullOrWhiteSpace(include))
                    .Select(include => include!)
                    .Select(include => Path.GetFullPath(Path.Combine(projectDirectory, include)))
                    .Select(fullPath => Path.GetRelativePath(repoRoot, fullPath))
                    .Select(relativePath => relativePath.Replace('/', '\\'))
            ];
        }

        private static string _FindRepoRoot()
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory is not null)
            {
                var solutionPath = Path.Combine(directory.FullName, "finebytes.slnx");
                if (File.Exists(solutionPath))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException("Could not locate repository root containing finebytes.slnx.");
        }
    }
}
