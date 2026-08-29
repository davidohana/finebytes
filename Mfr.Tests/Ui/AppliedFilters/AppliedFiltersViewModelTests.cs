using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters;
using Mfr.Filters.Case;
using Mfr.Filters.Space;

namespace Mfr.Tests.Ui.AppliedFilters
{
    /// <summary>
    /// Tests Applied Filters stack commands and <see cref="AppliedFiltersViewModel.ToChain"/>.
    /// </summary>
    public sealed class AppliedFiltersViewModelTests
    {
        /// <summary>
        /// Verifies add creates an enabled step with catalog defaults and Apply-To subtitle.
        /// </summary>
        [Fact]
        public void Add_Creates_Enabled_Step_With_Defaults()
        {
            var viewModel = new AppliedFiltersViewModel();
            var entry = _Entry("ShrinkSpaces");

            viewModel.AddCommand.Execute(entry);

            Assert.Equal(1, viewModel.Count);
            var step = viewModel.Steps[0];
            Assert.True(step.Enabled);
            Assert.Equal("Shrink Spaces", step.DisplayName);
            Assert.Equal("File Prefix", step.ApplyToLabel);
            Assert.IsType<ShrinkSpacesFilter>(step.Filter);
            Assert.Equal([step], viewModel.SelectedSteps);
        }

        /// <summary>
        /// Verifies duplicate catalog types get numbered display names when appended.
        /// </summary>
        [Fact]
        public void Add_Duplicate_Types_Get_Numbered_Display_Names()
        {
            var viewModel = new AppliedFiltersViewModel();
            var entry = _Entry("LettersCase");

            viewModel.AddCommand.Execute(entry);
            viewModel.SetSelectedSteps([]);
            viewModel.AddCommand.Execute(entry);

            Assert.Equal(["Letters Case", "Letters Case (2)"], viewModel.Steps.Select(step => step.DisplayName));
        }

        /// <summary>
        /// Verifies add inserts before the first selected row.
        /// </summary>
        [Fact]
        public void Add_Inserts_Before_First_Selected_Row()
        {
            var viewModel = new AppliedFiltersViewModel();
            var shrinkSpaces = _Entry("ShrinkSpaces");
            var lettersCase = _Entry("LettersCase");

            viewModel.AddCommand.Execute(shrinkSpaces);
            viewModel.SetSelectedSteps([]);
            viewModel.AddCommand.Execute(lettersCase);
            viewModel.SetSelectedSteps([viewModel.Steps[0]]);

            viewModel.AddCommand.Execute(lettersCase);

            Assert.Equal(
                ["Letters Case (2)", "Shrink Spaces", "Letters Case"],
                viewModel.Steps.Select(step => step.DisplayName)
            );
            Assert.Equal(viewModel.Steps[0], viewModel.SelectedSteps[0]);
        }

        /// <summary>
        /// Verifies remove deletes selected rows and keeps a neighbor selected.
        /// </summary>
        [Fact]
        public void RemoveSelected_Removes_Selection_And_Keeps_Neighbor()
        {
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(_Entry("ShrinkSpaces"));
            viewModel.SetSelectedSteps([]);
            viewModel.AddCommand.Execute(_Entry("LettersCase"));
            viewModel.SetSelectedSteps([viewModel.Steps[0]]);

            viewModel.RemoveSelectedCommand.Execute(null);

            Assert.Single(viewModel.Steps);
            Assert.Equal("Letters Case", viewModel.Steps[0].DisplayName);
            Assert.Equal(viewModel.Steps[0], viewModel.SelectedSteps[0]);
        }

        /// <summary>
        /// Verifies clear removes every step and selection.
        /// </summary>
        [Fact]
        public void Clear_Removes_All_Steps()
        {
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(_Entry("ShrinkSpaces"));
            viewModel.AddCommand.Execute(_Entry("LettersCase"));

            viewModel.ClearCommand.Execute(null);

            Assert.Empty(viewModel.Steps);
            Assert.Empty(viewModel.SelectedSteps);
            Assert.Equal(0, viewModel.Count);
        }

        /// <summary>
        /// Verifies move commands reorder the stack and keep selection.
        /// </summary>
        [Fact]
        public void MoveSelected_Reorders_Steps()
        {
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(_Entry("ShrinkSpaces"));
            viewModel.SetSelectedSteps([]);
            viewModel.AddCommand.Execute(_Entry("LettersCase"));
            viewModel.SetSelectedSteps([viewModel.Steps[1]]);

            viewModel.MoveSelectedUpCommand.Execute(null);

            Assert.Equal(["Letters Case", "Shrink Spaces"], viewModel.Steps.Select(step => step.DisplayName));
            Assert.Equal(viewModel.Steps[0], viewModel.SelectedSteps[0]);

            viewModel.MoveSelectedDownCommand.Execute(null);

            Assert.Equal(["Shrink Spaces", "Letters Case"], viewModel.Steps.Select(step => step.DisplayName));
            Assert.Equal(viewModel.Steps[1], viewModel.SelectedSteps[0]);
        }

        /// <summary>
        /// Verifies enabled flags and filters round-trip through <see cref="AppliedFiltersViewModel.ToChain"/>.
        /// </summary>
        [Fact]
        public void ToChain_Matches_Steps()
        {
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(_Entry("ShrinkSpaces"));
            viewModel.SetSelectedSteps([]);
            viewModel.AddCommand.Execute(_Entry("LettersCase"));
            viewModel.Steps[1].Enabled = false;

            var chain = viewModel.ToChain();

            Assert.Equal(2, chain.Steps.Count);
            Assert.True(chain.Steps[0].Enabled);
            Assert.False(chain.Steps[1].Enabled);
            Assert.IsType<ShrinkSpacesFilter>(chain.Steps[0].Filter);
            Assert.IsType<LettersCaseFilter>(chain.Steps[1].Filter);
        }

        /// <summary>
        /// Verifies non-string filters have no Apply-To subtitle.
        /// </summary>
        [Fact]
        public void ApplyToLabel_Is_Empty_For_Non_String_Filters()
        {
            var viewModel = new AppliedFiltersViewModel();

            viewModel.AddCommand.Execute(_Entry("TagRemover"));

            Assert.Equal(string.Empty, viewModel.Steps[0].ApplyToLabel);
        }

        private static FilterCatalogEntry _Entry(string type)
        {
            return FilterCatalog.Entries.Single(entry => entry.Type == type);
        }
    }
}
