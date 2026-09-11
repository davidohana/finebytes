using Mfr.Filters;

namespace Mfr.Tests.Models.Filters
{
    /// <summary>
    /// Guards catalog Help naming convention (<c>{Type}.html</c>) and shipped <c>help/</c> files.
    /// </summary>
    public sealed class FilterCatalogHelpTests
    {
        /// <summary>
        /// Verifies every catalog entry uses <c>{Type}.html</c> as its Help file name.
        /// </summary>
        [Fact]
        public void Every_Catalog_HelpFileName_Is_Type_Dot_Html()
        {
            var mismatches = FilterCatalog
                .Entries.Where(entry => entry.HelpFileName != $"{entry.Type}.html")
                .Select(entry => $"{entry.Type} → {entry.HelpFileName}")
                .OrderBy(line => line, StringComparer.Ordinal)
                .ToList();

            Assert.Empty(mismatches);
        }

        /// <summary>
        /// Verifies Help file names are <c>{Type}.html</c> for a sample of filters.
        /// </summary>
        [Theory]
        [InlineData("SpaceCharacter")]
        [InlineData("LettersCase")]
        [InlineData("TagRemover")]
        [InlineData("ShrinkDuplicateCharacters")]
        [InlineData("DateTimeSetter")]
        [InlineData("PathMover")]
        public void Sample_Help_File_Names_Follow_Convention(string catalogType)
        {
            var entry = FilterCatalog.Entries.Single(e => e.Type == catalogType);
            Assert.Equal($"{catalogType}.html", entry.HelpFileName);
        }

        /// <summary>
        /// Verifies every catalog Help file exists under the repo-root <c>help/</c> folder.
        /// </summary>
        [Fact]
        public void Every_Catalog_HelpFileName_Exists_Under_Repo_Help()
        {
            var helpRoot = _ResolveRepoHelpRoot();
            Assert.True(Directory.Exists(helpRoot), $"Expected help folder at '{helpRoot}'.");

            var missing = FilterCatalog
                .Entries.Select(entry => entry.HelpFileName)
                .Distinct(StringComparer.Ordinal)
                .Where(fileName => !File.Exists(Path.Combine(helpRoot, fileName)))
                .OrderBy(fileName => fileName, StringComparer.Ordinal)
                .ToList();

            Assert.Empty(missing);
        }

        private static string _ResolveRepoHelpRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null)
            {
                var candidate = Path.Combine(dir.FullName, "help");
                if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "filters.html")))
                {
                    return candidate;
                }

                dir = dir.Parent;
            }

            throw new InvalidOperationException(
                "Could not locate repo-root help/ (expected filters.html) by walking up from BaseDirectory."
            );
        }
    }
}
