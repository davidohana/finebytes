using Mfr.App.Ui.ViewModels.AppliedFilters;
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
            var entry = AppliedFiltersTestUi.Entry("ShrinkSpaces");

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
            var entry = AppliedFiltersTestUi.Entry("LettersCase");

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
            var shrinkSpaces = AppliedFiltersTestUi.Entry("ShrinkSpaces");
            var lettersCase = AppliedFiltersTestUi.Entry("LettersCase");

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
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            viewModel.SetSelectedSteps([]);
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
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
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));

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
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            viewModel.SetSelectedSteps([]);
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
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
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            viewModel.SetSelectedSteps([]);
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
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

            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("TagRemover"));

            Assert.Equal(string.Empty, viewModel.Steps[0].ApplyToLabel);
        }

        /// <summary>
        /// Verifies Filter Options is available only for a single selected row.
        /// </summary>
        [Fact]
        public void CanShowFilterOptions_requires_single_selection()
        {
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));

            Assert.True(viewModel.CanShowFilterOptions);

            viewModel.SetSelectedSteps([viewModel.Steps[0], viewModel.Steps[1]]);
            Assert.False(viewModel.CanShowFilterOptions);

            viewModel.SetSelectedSteps([]);
            Assert.False(viewModel.CanShowFilterOptions);
        }

        /// <summary>
        /// Verifies append always adds at the end even when another row is selected.
        /// </summary>
        [Fact]
        public void Append_Adds_At_End_Even_With_Selection()
        {
            var viewModel = new AppliedFiltersViewModel();
            var shrinkSpaces = AppliedFiltersTestUi.Entry("ShrinkSpaces");
            var lettersCase = AppliedFiltersTestUi.Entry("LettersCase");

            viewModel.AddCommand.Execute(shrinkSpaces);
            viewModel.SetSelectedSteps([viewModel.Steps[0]]);
            viewModel.AppendCommand.Execute(lettersCase);

            Assert.Equal(["Shrink Spaces", "Letters Case"], viewModel.Steps.Select(step => step.DisplayName));
            Assert.Equal(viewModel.Steps[1], viewModel.SelectedSteps[0]);
        }

        /// <summary>
        /// Verifies drag-drop insert moves selected steps and keeps them selected.
        /// </summary>
        [Fact]
        public void MoveStepsTo_reorders_selected_block()
        {
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            viewModel.SetSelectedSteps([]);
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            viewModel.SetSelectedSteps([]);
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("TagRemover"));
            viewModel.SetSelectedSteps([viewModel.Steps[1]]);

            viewModel.MoveStepsTo([1], targetIndex: 0);

            Assert.Equal(
                ["Letters Case", "Shrink Spaces", "Audio Tag Remover"],
                viewModel.Steps.Select(step => step.DisplayName)
            );
            Assert.Equal(viewModel.Steps[0], viewModel.SelectedSteps[0]);
        }

        /// <summary>
        /// Verifies inserting catalog rows at an index preserves order and selects the new steps.
        /// </summary>
        [Fact]
        public void InsertFromCatalogAt_inserts_at_index_and_selects_new_steps()
        {
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            viewModel.SetSelectedSteps([]);

            viewModel.InsertFromCatalogAt(
                [AppliedFiltersTestUi.Entry("LettersCase"), AppliedFiltersTestUi.Entry("TagRemover")],
                insertIndex: 0
            );

            Assert.Equal(
                ["Letters Case", "Audio Tag Remover", "Shrink Spaces"],
                viewModel.Steps.Select(step => step.DisplayName)
            );
            Assert.Equal(2, viewModel.SelectedSteps.Count);
            Assert.Equal(viewModel.Steps[0], viewModel.SelectedSteps[0]);
            Assert.Equal(viewModel.Steps[1], viewModel.SelectedSteps[1]);
        }

        /// <summary>
        /// Verifies drag-back removal deletes steps by index and updates selection.
        /// </summary>
        [Fact]
        public void RemoveStepsAtIndices_removes_rows_and_selects_neighbor()
        {
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            viewModel.SetSelectedSteps([]);
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            viewModel.SetSelectedSteps([]);

            viewModel.RemoveStepsAtIndices([0]);

            Assert.Single(viewModel.Steps);
            Assert.Equal("Letters Case", viewModel.Steps[0].DisplayName);
            Assert.Equal(viewModel.Steps[0], viewModel.SelectedSteps[0]);
        }
    }
}
