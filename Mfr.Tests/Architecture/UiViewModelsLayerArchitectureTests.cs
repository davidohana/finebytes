// UI internal layering: docs/mfr-folder-layering.md

namespace Mfr.Tests.Architecture
{
    /// <summary>
    /// Verifies <c>Mfr.App.Ui/ViewModels</c> does not depend on Views.
    /// </summary>
    /// <remarks>
    /// <para>Target flow inside the UI project: Views → ViewModels → Services → Engine / Models / Utils.</para>
    /// <para>
    /// This test only gates ViewModels↛Views. Views→Services remains intentional glue (session grids,
    /// folder picker, File List / Rename List helpers) and must not be blanket-forbidden without an
    /// allowlist — see <c>docs/mfr-folder-layering.md</c> and <see cref="UiServicesLayerArchitectureTests"/>.
    /// </para>
    /// </remarks>
    public sealed class UiViewModelsLayerArchitectureTests
    {
        private static readonly string[] _ForbiddenNamespaces = ["Mfr.App.Ui.Views"];

        /// <summary>
        /// ViewModels source must not import or qualify Views types.
        /// </summary>
        [Fact]
        public void ViewModels_DoNotReference_Views()
        {
            var repoRoot = ArchitectureRepoPaths.FindRepoRoot();
            var viewModelsRoot = Path.Combine(repoRoot, "Mfr.App.Ui", "ViewModels");
            Assert.True(Directory.Exists(viewModelsRoot), $"Expected ViewModels folder at '{viewModelsRoot}'.");

            var violations = Directory
                .EnumerateFiles(viewModelsRoot, "*.cs", SearchOption.AllDirectories)
                .SelectMany(path =>
                    File.ReadLines(path)
                        .Select((line, index) => (Path: path, LineNumber: index + 1, Line: line))
                        .Where(entry =>
                            _ForbiddenNamespaces.Any(ns => entry.Line.Contains(ns, StringComparison.Ordinal))
                        )
                        .Select(entry =>
                            $"{Path.GetRelativePath(repoRoot, entry.Path)}:{entry.LineNumber}: {entry.Line.Trim()}"
                        )
                )
                .ToList();

            Assert.True(
                violations.Count == 0,
                "Mfr.App.Ui/ViewModels must not reference Views. Violations:"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, violations)
            );
        }
    }
}
