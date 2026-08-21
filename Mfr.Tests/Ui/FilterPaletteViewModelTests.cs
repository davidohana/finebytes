using Mfr.App.Ui.ViewModels;
using Mfr.Filters;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Tests Available Filters grouping and quick-search behavior.
    /// </summary>
    public sealed class FilterPaletteViewModelTests
    {
        /// <summary>
        /// Verifies All shows every catalog entry sorted by display name.
        /// </summary>
        [Fact]
        public void All_Shows_Every_Filter_Sorted_By_Display_Name()
        {
            var viewModel = new FilterPaletteViewModel();

            Assert.Null(viewModel.SelectedGroup);
            Assert.Equal(FilterCatalog.Entries.Count, viewModel.VisibleFilters.Count);
            Assert.Equal(
                FilterCatalog.Entries.OrderBy(e => e.DisplayName, StringComparer.OrdinalIgnoreCase).ToList(),
                [.. viewModel.VisibleFilters]
            );
            Assert.True(viewModel.Groups[0].IsSelected);
            Assert.All(viewModel.Groups.Skip(1), g => Assert.False(g.IsSelected));
        }

        /// <summary>
        /// Verifies selecting Case shows only Case filters and updates selection flags.
        /// </summary>
        [Fact]
        public void SelectGroup_Case_Filters_List()
        {
            var viewModel = new FilterPaletteViewModel();
            var caseButton = viewModel.Groups.Single(g => g.Group == FilterGroup.Case);

            viewModel.SelectGroup(caseButton);

            Assert.Equal(FilterGroup.Case, viewModel.SelectedGroup);
            Assert.True(caseButton.IsSelected);
            Assert.False(viewModel.Groups[0].IsSelected);
            Assert.All(viewModel.VisibleFilters, e => Assert.Equal(FilterGroup.Case, e.Group));
            Assert.Equal(
                FilterCatalog
                    .Entries.Where(e => e.Group == FilterGroup.Case)
                    .OrderBy(e => e.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                [.. viewModel.VisibleFilters]
            );
        }

        /// <summary>
        /// Verifies switching groups keeps selection when the row remains visible.
        /// </summary>
        [Fact]
        public void Switching_Group_Keeps_Selection_When_Still_Visible()
        {
            var viewModel = new FilterPaletteViewModel();
            var cleaner = FilterCatalog.Entries.Single(e => e.Type == "Cleaner");
            viewModel.SelectedFilter = cleaner;

            viewModel.SelectGroup(viewModel.Groups.Single(g => g.Group == FilterGroup.Replace));

            Assert.Equal(cleaner, viewModel.SelectedFilter);
        }

        /// <summary>
        /// Verifies switching groups selects the first remaining row when the prior selection drops out.
        /// </summary>
        [Fact]
        public void Switching_Group_Reselects_First_When_Selection_Leaves()
        {
            var viewModel = new FilterPaletteViewModel();
            var cleaner = FilterCatalog.Entries.Single(e => e.Type == "Cleaner");
            viewModel.SelectedFilter = cleaner;

            viewModel.SelectGroup(viewModel.Groups.Single(g => g.Group == FilterGroup.Case));

            Assert.NotNull(viewModel.SelectedFilter);
            Assert.Equal(FilterGroup.Case, viewModel.SelectedFilter.Group);
            Assert.Equal(viewModel.VisibleFilters[0], viewModel.SelectedFilter);
        }

        /// <summary>
        /// Verifies case-insensitive substring search against display name and type.
        /// </summary>
        [Fact]
        public void SearchText_Filters_By_Substring()
        {
            var viewModel = new FilterPaletteViewModel
            {
                SearchText = "tag"
            };
            Assert.Contains(viewModel.VisibleFilters, e => e.Type == "AudioTagSetter");
            Assert.Contains(viewModel.VisibleFilters, e => e.Type == "TagRemover");
            Assert.DoesNotContain(viewModel.VisibleFilters, e => e.Type == "Cleaner");

            viewModel.SearchText = "0's";
            Assert.Equal(["FixLeadingZeros"], viewModel.VisibleFilters.Select(e => e.Type).ToList());

            viewModel.SearchText = "ID3";
            Assert.Contains(viewModel.VisibleFilters, e => e.Type == "Id3v2FieldSetter");

            viewModel.SearchText = "letterscase";
            Assert.Equal(["LettersCase"], viewModel.VisibleFilters.Select(e => e.Type).ToList());
        }

        /// <summary>
        /// Verifies group and search combine with AND.
        /// </summary>
        [Fact]
        public void Group_And_Search_Combine()
        {
            var viewModel = new FilterPaletteViewModel();
            viewModel.SelectGroup(viewModel.Groups.Single(g => g.Group == FilterGroup.Audio));
            viewModel.SearchText = "tag";

            Assert.All(viewModel.VisibleFilters, e => Assert.Equal(FilterGroup.Audio, e.Group));
            Assert.Contains(viewModel.VisibleFilters, e => e.Type == "AudioTagSetter");
            Assert.Contains(viewModel.VisibleFilters, e => e.Type == "TagRemover");
            Assert.DoesNotContain(viewModel.VisibleFilters, e => e.Type == "Id3v2FieldSetter");
        }

        /// <summary>
        /// Verifies clearing search restores the full group list.
        /// </summary>
        [Fact]
        public void Empty_Search_Restores_Group_List()
        {
            var viewModel = new FilterPaletteViewModel();
            viewModel.SelectGroup(viewModel.Groups.Single(g => g.Group == FilterGroup.Misc));
            viewModel.SearchText = "0's";
            Assert.Single(viewModel.VisibleFilters);

            viewModel.SearchText = string.Empty;

            Assert.Equal(FilterCatalog.Entries.Count(e => e.Group == FilterGroup.Misc), viewModel.VisibleFilters.Count);
        }
    }
}
