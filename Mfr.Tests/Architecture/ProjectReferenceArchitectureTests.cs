using System.Xml.Linq;

namespace Mfr.Tests.Architecture
{
    /// <summary>
    /// Verifies project reference boundaries enforce the intended layer direction.
    /// </summary>
    public sealed class ProjectReferenceArchitectureTests
    {
        private static readonly StringComparer _pathComparer = StringComparer.OrdinalIgnoreCase;

        /// <summary>
        /// Verifies the host project references only the expected layer projects.
        /// </summary>
        [Fact]
        public void HostProject_ReferencesExpectedLayerProjects()
        {
            _AssertProjectReferences(
                projectPathFromRepoRoot: @"Mfr\Mfr.csproj",
                expectedReferencesFromRepoRoot:
                [
                    @"Mfr.App.Cli\Mfr.App.Cli.csproj",
                    @"Mfr.Core\Mfr.Core.csproj",
                    @"Mfr.Filters\Mfr.Filters.csproj",
                    @"Mfr.Models\Mfr.Models.csproj",
                    @"Mfr.Utils\Mfr.Utils.csproj"
                ]);
        }

        /// <summary>
        /// Verifies application-layer CLI project references only allowed lower layers.
        /// </summary>
        [Fact]
        public void AppCliProject_ReferencesExpectedLowerLayers()
        {
            _AssertProjectReferences(
                projectPathFromRepoRoot: @"Mfr.App.Cli\Mfr.App.Cli.csproj",
                expectedReferencesFromRepoRoot:
                [
                    @"Mfr.Core\Mfr.Core.csproj",
                    @"Mfr.Models\Mfr.Models.csproj",
                    @"Mfr.Utils\Mfr.Utils.csproj"
                ]);
        }

        /// <summary>
        /// Verifies core project references only allowed lower layers.
        /// </summary>
        [Fact]
        public void CoreProject_ReferencesExpectedLowerLayers()
        {
            _AssertProjectReferences(
                projectPathFromRepoRoot: @"Mfr.Core\Mfr.Core.csproj",
                expectedReferencesFromRepoRoot:
                [
                    @"Mfr.Filters\Mfr.Filters.csproj",
                    @"Mfr.Models\Mfr.Models.csproj",
                    @"Mfr.Utils\Mfr.Utils.csproj"
                ]);
        }

        /// <summary>
        /// Verifies filters project references only model and utility layers.
        /// </summary>
        [Fact]
        public void FiltersProject_ReferencesExpectedLowerLayers()
        {
            _AssertProjectReferences(
                projectPathFromRepoRoot: @"Mfr.Filters\Mfr.Filters.csproj",
                expectedReferencesFromRepoRoot:
                [
                    @"Mfr.Models\Mfr.Models.csproj",
                    @"Mfr.Utils\Mfr.Utils.csproj"
                ]);
        }

        /// <summary>
        /// Verifies models project references only utilities.
        /// </summary>
        [Fact]
        public void ModelsProject_ReferencesExpectedLowerLayers()
        {
            _AssertProjectReferences(
                projectPathFromRepoRoot: @"Mfr.Models\Mfr.Models.csproj",
                expectedReferencesFromRepoRoot:
                [
                    @"Mfr.Utils\Mfr.Utils.csproj"
                ]);
        }

        /// <summary>
        /// Verifies utility project has no project references.
        /// </summary>
        [Fact]
        public void UtilsProject_HasNoProjectReferences()
        {
            _AssertProjectReferences(
                projectPathFromRepoRoot: @"Mfr.Utils\Mfr.Utils.csproj",
                expectedReferencesFromRepoRoot: []);
        }

        private static void _AssertProjectReferences(
            string projectPathFromRepoRoot,
            IReadOnlyCollection<string> expectedReferencesFromRepoRoot)
        {
            var repoRoot = _FindRepoRoot();
            var projectFullPath = Path.Combine(repoRoot, projectPathFromRepoRoot);
            var document = XDocument.Load(projectFullPath);

            var projectDirectory = Path.GetDirectoryName(projectFullPath)
                ?? throw new InvalidOperationException($"Could not resolve directory for '{projectFullPath}'.");
            var actualReferencesFromRepoRoot = document
                .Descendants("ProjectReference")
                .Select(reference => reference.Attribute("Include")?.Value)
                .Where(include => !string.IsNullOrWhiteSpace(include))
                .Select(include => include!)
                .Select(include => Path.GetFullPath(Path.Combine(projectDirectory, include)))
                .Select(fullPath => Path.GetRelativePath(repoRoot, fullPath))
                .Select(relativePath => relativePath.Replace('/', '\\'))
                .OrderBy(path => path, _pathComparer)
                .ToList();

            var expectedReferences = expectedReferencesFromRepoRoot
                .Select(path => path.Replace('/', '\\'))
                .OrderBy(path => path, _pathComparer)
                .ToList();

            Assert.Equal(expected: expectedReferences, actual: actualReferencesFromRepoRoot, comparer: _pathComparer);
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
