// UI internal layering: docs/mfr-folder-layering.md

namespace Mfr.Tests.Architecture
{
    /// <summary>
    /// Verifies <c>Mfr.App.Ui/Services</c> does not depend on ViewModels, Views, Filters, or Metadata.
    /// </summary>
    /// <remarks>
    /// <para>Target flow inside the UI project: Views → ViewModels → Services (see docs/mfr-folder-layering.md).</para>
    /// <para>
    /// Services may use Engine.Config / Models. They must not import Filters or Metadata (orchestration stays
    /// on ViewModels). Views→Services shortcuts (MainWindow session grids, PathMover folder picker, File List
    /// address-bar segments, Rename List add-source checks, DialogSession dialogs) are intentional glue — see
    /// <c>docs/mfr-folder-layering.md</c>. Do not add a blanket Views↛Services forbid without an allowlist.
    /// </para>
    /// </remarks>
    public sealed class UiServicesLayerArchitectureTests
    {
        private static readonly string[] _ForbiddenNamespaces =
        [
            "Mfr.App.Ui.ViewModels",
            "Mfr.App.Ui.Views",
            "Mfr.Filters",
            "Mfr.Metadata",
        ];

        /// <summary>
        /// Services source must not import or qualify ViewModels, Views, Filters, or Metadata types.
        /// </summary>
        [Fact]
        public void Services_DoNotReference_ViewModels_Views_Filters_Or_Metadata()
        {
            var repoRoot = ArchitectureRepoPaths.FindRepoRoot();
            var servicesRoot = Path.Combine(repoRoot, "Mfr.App.Ui", "Services");
            Assert.True(Directory.Exists(servicesRoot), $"Expected Services folder at '{servicesRoot}'.");

            var violations = Directory
                .EnumerateFiles(servicesRoot, "*.cs", SearchOption.AllDirectories)
                .SelectMany(path =>
                    File.ReadLines(path)
                        .Select((line, index) => (Path: path, LineNumber: index + 1, Line: line))
                        .Where(entry => _LineReferencesForbiddenNamespace(entry.Line))
                        .Select(entry =>
                            $"{Path.GetRelativePath(repoRoot, entry.Path)}:{entry.LineNumber}: {entry.Line.Trim()}"
                        )
                )
                .ToList();

            Assert.True(
                violations.Count == 0,
                "Mfr.App.Ui/Services must not reference ViewModels, Views, Filters, or Metadata. Violations:"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, violations)
            );
        }

        /// <summary>
        /// Matches a forbidden namespace as a whole identifier prefix (avoids matching shorter substrings).
        /// </summary>
        /// <param name="line">Source line to inspect.</param>
        /// <returns><see langword="true"/> when the line references a forbidden namespace.</returns>
        private static bool _LineReferencesForbiddenNamespace(string line)
        {
            foreach (var ns in _ForbiddenNamespaces)
            {
                var index = 0;
                while ((index = line.IndexOf(ns, index, StringComparison.Ordinal)) >= 0)
                {
                    var afterIndex = index + ns.Length;
                    if (afterIndex >= line.Length || !_IsIdentifierContinue(line[afterIndex]))
                    {
                        return true;
                    }

                    index = afterIndex;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns whether <paramref name="c"/> can continue a C# identifier after a namespace segment.
        /// </summary>
        /// <param name="c">Character immediately after a candidate namespace match.</param>
        /// <returns><see langword="true"/> for letters, digits, or underscore.</returns>
        private static bool _IsIdentifierContinue(char c)
        {
            return char.IsAsciiLetterOrDigit(c) || c == '_';
        }
    }
}
