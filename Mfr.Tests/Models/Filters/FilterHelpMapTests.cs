using Mfr.Filters;

namespace Mfr.Tests.Models.Filters
{
    /// <summary>
    /// Guards <see cref="FilterHelpMap"/> against catalog drift.
    /// </summary>
    public sealed class FilterHelpMapTests
    {
        /// <summary>
        /// Verifies every catalog filter type has an MFR7 Help file mapping.
        /// </summary>
        [Fact]
        public void Every_Catalog_Type_Has_Help_Mapping()
        {
            var missing = FilterCatalog
                .Entries.Where(entry => string.IsNullOrEmpty(entry.HelpFileName))
                .Select(entry => entry.Type)
                .OrderBy(type => type, StringComparer.Ordinal)
                .ToList();

            Assert.Empty(missing);
        }

        /// <summary>
        /// Verifies known MFR7 help file names for a sample of filters.
        /// </summary>
        [Theory]
        [InlineData("SpaceCharacter", "spacecharfilter.html")]
        [InlineData("LettersCase", "letterscasefilter.html")]
        [InlineData("TagRemover", "id3tagremoverfilter.html")]
        [InlineData("ShrinkDuplicateCharacters", "remdupsfilter.html")]
        [InlineData("DateTimeSetter", "datefilter.html")]
        [InlineData("PathMover", "moverfilter.html")]
        public void Known_Help_File_Names(string catalogType, string helpFileName)
        {
            Assert.True(FilterHelpMap.TryGetHelpFileName(catalogType, out var mapped));
            Assert.Equal(helpFileName, mapped);

            var entry = FilterCatalog.Entries.Single(e => e.Type == catalogType);
            Assert.Equal(helpFileName, entry.HelpFileName);
        }

        /// <summary>
        /// Verifies unknown types do not resolve.
        /// </summary>
        [Fact]
        public void Unknown_Type_Returns_False()
        {
            Assert.False(FilterHelpMap.TryGetHelpFileName("NotARealFilter", out var helpFileName));
            Assert.Equal(string.Empty, helpFileName);
        }
    }
}
