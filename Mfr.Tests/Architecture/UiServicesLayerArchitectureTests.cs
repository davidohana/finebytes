// UI internal layering: docs/mfr-folder-layering.md

namespace Mfr.Tests.Architecture
{
    /// <summary>
    /// Verifies <c>Mfr.App.Ui/Services</c> does not depend on ViewModels.
    /// </summary>
    /// <remarks>
    /// Target flow inside the UI project: Views → ViewModels → Services → Engine / Models / Utils.
    /// </remarks>
    public sealed class UiServicesLayerArchitectureTests
    {
        private const string _ForbiddenNamespace = "Mfr.App.Ui.ViewModels";

        /// <summary>
        /// Services source must not import or qualify ViewModels types.
        /// </summary>
        [Fact]
        public void Services_DoNotReference_ViewModels()
        {
            var repoRoot = _FindRepoRoot();
            var servicesRoot = Path.Combine(repoRoot, "Mfr.App.Ui", "Services");
            Assert.True(Directory.Exists(servicesRoot), $"Expected Services folder at '{servicesRoot}'.");

            var violations = Directory
                .EnumerateFiles(servicesRoot, "*.cs", SearchOption.AllDirectories)
                .SelectMany(path =>
                    File.ReadLines(path)
                        .Select((line, index) => (Path: path, LineNumber: index + 1, Line: line))
                        .Where(entry => entry.Line.Contains(_ForbiddenNamespace, StringComparison.Ordinal))
                        .Select(entry =>
                            $"{Path.GetRelativePath(repoRoot, entry.Path)}:{entry.LineNumber}: {entry.Line.Trim()}"
                        )
                )
                .ToList();

            Assert.True(
                violations.Count == 0,
                "Mfr.App.Ui/Services must not reference ViewModels. Violations:"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, violations)
            );
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
