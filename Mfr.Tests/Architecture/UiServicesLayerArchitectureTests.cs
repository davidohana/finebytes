// UI internal layering: docs/mfr-folder-layering.md

namespace Mfr.Tests.Architecture
{
    /// <summary>
    /// Verifies <c>Mfr.App.Ui/Services</c> does not depend on ViewModels or Views.
    /// </summary>
    /// <remarks>
    /// <para>Target flow inside the UI project: Views → ViewModels → Services → Engine / Models / Utils.</para>
    /// <para>
    /// Views→Services shortcuts (MainWindow session grids, PathMover folder picker, File List address-bar
    /// segments, Rename List add-source checks) are intentional glue — see
    /// <c>docs/mfr-folder-layering.md</c>. Do not add a blanket Views↛Services forbid without an allowlist.
    /// </para>
    /// </remarks>
    public sealed class UiServicesLayerArchitectureTests
    {
        private static readonly string[] _ForbiddenNamespaces = ["Mfr.App.Ui.ViewModels", "Mfr.App.Ui.Views"];

        /// <summary>
        /// Services source must not import or qualify ViewModels or Views types.
        /// </summary>
        [Fact]
        public void Services_DoNotReference_ViewModelsOrViews()
        {
            var repoRoot = ArchitectureRepoPaths.FindRepoRoot();
            var servicesRoot = Path.Combine(repoRoot, "Mfr.App.Ui", "Services");
            Assert.True(Directory.Exists(servicesRoot), $"Expected Services folder at '{servicesRoot}'.");

            var violations = Directory
                .EnumerateFiles(servicesRoot, "*.cs", SearchOption.AllDirectories)
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
                "Mfr.App.Ui/Services must not reference ViewModels or Views. Violations:"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, violations)
            );
        }
    }
}
