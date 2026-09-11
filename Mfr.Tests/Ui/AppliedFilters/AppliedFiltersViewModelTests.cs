using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.Presets;
using Mfr.Filters.Case;
using Mfr.Filters.Formatting;
using Mfr.Filters.Space;
using Mfr.Models.RenameList.Fields.Basic;

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
            Assert.Equal("File Name", step.ApplyToLabel);
            Assert.IsType<ShrinkSpacesFilter>(step.Filter);
            Assert.Equal([step], viewModel.SelectedSteps);
        }

        /// <summary>
        /// Verifies AddAndSelect appends a concrete filter, keeps its options, and selects it.
        /// </summary>
        [Fact]
        public void AddAndSelect_appends_concrete_filter_and_selects()
        {
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            viewModel.SetSelectedSteps([viewModel.Steps[0]]);

            var nameList = new NameListFilter(new FilePrefixTarget(), new NameListOptions(Entries: ["alpha", "beta"]));
            var chainChanged = _CountChainChanged(viewModel, () => viewModel.AddAndSelect(nameList, "File Name List"));

            Assert.Equal(2, viewModel.Count);
            var step = viewModel.Steps[1];
            Assert.Same(nameList, step.Filter);
            Assert.Equal("File Name List", step.DisplayName);
            Assert.Equal([step], viewModel.SelectedSteps);
            Assert.Equal(1, chainChanged);
        }

        /// <summary>
        /// Verifies AddAndSelect appends <c>*</c> until the display name is unique.
        /// </summary>
        [Fact]
        public void AddAndSelect_appends_star_until_display_name_unique()
        {
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddAndSelect(
                new NameListFilter(new FilePrefixTarget(), new NameListOptions(Entries: ["a"])),
                "Title List"
            );
            viewModel.AddAndSelect(
                new NameListFilter(new FilePrefixTarget(), new NameListOptions(Entries: ["b"])),
                "Title List"
            );
            viewModel.AddAndSelect(
                new NameListFilter(new FilePrefixTarget(), new NameListOptions(Entries: ["c"])),
                "Title List"
            );

            Assert.Equal(
                ["Title List", "Title List*", "Title List**"],
                viewModel.Steps.Select(step => step.DisplayName)
            );
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
        /// Verifies Check All enables every step and raises <see cref="AppliedFiltersViewModel.ChainChanged"/> once.
        /// </summary>
        [Fact]
        public void CheckAllFilters_Enables_All_With_Single_ChainChanged()
        {
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            viewModel.SetSelectedSteps([]);
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            viewModel.Steps[0].Enabled = false;
            viewModel.Steps[1].Enabled = false;

            var count = _CountChainChanged(viewModel, () => viewModel.CheckAllFiltersCommand.Execute(null));

            Assert.Equal(1, count);
            Assert.All(viewModel.Steps, step => Assert.True(step.Enabled));
            Assert.All(viewModel.ToChain().Steps, step => Assert.True(step.Enabled));
        }

        /// <summary>
        /// Verifies Uncheck All disables every step and raises <see cref="AppliedFiltersViewModel.ChainChanged"/> once.
        /// </summary>
        [Fact]
        public void UncheckAllFilters_Disables_All_With_Single_ChainChanged()
        {
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            viewModel.SetSelectedSteps([]);
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));

            var count = _CountChainChanged(viewModel, () => viewModel.UncheckAllFiltersCommand.Execute(null));

            Assert.Equal(1, count);
            Assert.All(viewModel.Steps, step => Assert.False(step.Enabled));
            Assert.All(viewModel.ToChain().Steps, step => Assert.False(step.Enabled));
        }

        /// <summary>
        /// Verifies Invert Check flips each enabled flag and raises <see cref="AppliedFiltersViewModel.ChainChanged"/> once.
        /// </summary>
        [Fact]
        public void InvertCheck_Flips_Each_Enabled_With_Single_ChainChanged()
        {
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            viewModel.SetSelectedSteps([]);
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            viewModel.Steps[1].Enabled = false;

            var count = _CountChainChanged(viewModel, () => viewModel.InvertCheckCommand.Execute(null));

            Assert.Equal(1, count);
            Assert.False(viewModel.Steps[0].Enabled);
            Assert.True(viewModel.Steps[1].Enabled);
        }

        /// <summary>
        /// Verifies Remove All But Selected keeps the selection and drops other steps.
        /// </summary>
        [Fact]
        public void RemoveAllButSelected_Keeps_Selection()
        {
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            viewModel.SetSelectedSteps([]);
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            viewModel.SetSelectedSteps([]);
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("RemoveSpaces"));
            viewModel.SetSelectedSteps([viewModel.Steps[1]]);

            Assert.True(viewModel.RemoveAllButSelectedCommand.CanExecute(null));
            viewModel.RemoveAllButSelectedCommand.Execute(null);

            Assert.Single(viewModel.Steps);
            Assert.Equal("Letters Case", viewModel.Steps[0].DisplayName);
            Assert.Equal(viewModel.Steps[0], viewModel.SelectedSteps[0]);
        }

        /// <summary>
        /// Verifies Remove All But Selected raises <see cref="AppliedFiltersViewModel.ChainChanged"/> once.
        /// </summary>
        [Fact]
        public void RemoveAllButSelected_Raises_ChainChanged_Once()
        {
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            viewModel.SetSelectedSteps([]);
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            viewModel.SetSelectedSteps([]);
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("RemoveSpaces"));
            viewModel.SetSelectedSteps([viewModel.Steps[1]]);

            var count = _CountChainChanged(viewModel, () => viewModel.RemoveAllButSelectedCommand.Execute(null));

            Assert.Equal(1, count);
            Assert.Single(viewModel.Steps);
        }

        /// <summary>
        /// Verifies Remove All But Selected is disabled when nothing or everything is selected.
        /// </summary>
        [Fact]
        public void RemoveAllButSelected_CanExecute_Requires_Partial_Selection()
        {
            var viewModel = new AppliedFiltersViewModel();
            Assert.False(viewModel.RemoveAllButSelectedCommand.CanExecute(null));

            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            viewModel.SetSelectedSteps([]);
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            viewModel.SetSelectedSteps([viewModel.Steps[0], viewModel.Steps[1]]);
            Assert.False(viewModel.RemoveAllButSelectedCommand.CanExecute(null));

            viewModel.SetSelectedSteps([]);
            Assert.False(viewModel.RemoveAllButSelectedCommand.CanExecute(null));

            viewModel.SetSelectedSteps([viewModel.Steps[0]]);
            Assert.True(viewModel.RemoveAllButSelectedCommand.CanExecute(null));
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
        /// Verifies reset restores catalog defaults while keeping display name, enabled, and list membership.
        /// </summary>
        [Fact]
        public void ResetSelectedToDefaults_restores_options_keeps_name_and_enabled()
        {
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            var step = viewModel.Steps[0];
            step.SetDisplayName("My Letters");
            step.Enabled = false;
            step.SetFilter(
                new LettersCaseFilter(new FileExtensionTarget(), new LettersCaseOptions(LettersCaseMode.UpperCase, []))
            );

            var optionsApplied = 0;
            viewModel.FilterOptionsApplied += (_, _) => optionsApplied++;
            var chainChanged = _CountChainChanged(
                viewModel,
                () => viewModel.ResetSelectedToDefaultsCommand.Execute(null)
            );

            Assert.Same(step, viewModel.Steps[0]);
            Assert.Equal("My Letters", step.DisplayName);
            Assert.False(step.Enabled);
            Assert.Equal(new LettersCaseFilter(), step.Filter);
            Assert.Equal("File Name", step.ApplyToLabel);
            Assert.Equal(1, chainChanged);
            Assert.Equal(1, optionsApplied);
        }

        /// <summary>
        /// Verifies reset is a no-op when the selected step is already at catalog defaults.
        /// </summary>
        [Fact]
        public void ResetSelectedToDefaults_noop_when_already_default()
        {
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            var filterBefore = viewModel.Steps[0].Filter;

            var optionsApplied = 0;
            viewModel.FilterOptionsApplied += (_, _) => optionsApplied++;
            var chainChanged = _CountChainChanged(
                viewModel,
                () => viewModel.ResetSelectedToDefaultsCommand.Execute(null)
            );

            Assert.Same(filterBefore, viewModel.Steps[0].Filter);
            Assert.Equal(0, chainChanged);
            Assert.Equal(0, optionsApplied);
        }

        /// <summary>
        /// Verifies reset stays a no-op after preview setup when options are already catalog defaults.
        /// </summary>
        [Fact]
        public void ResetSelectedToDefaults_noop_after_setup_when_already_default()
        {
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            var filterBefore = viewModel.Steps[0].Filter;
            filterBefore.Setup();

            var optionsApplied = 0;
            viewModel.FilterOptionsApplied += (_, _) => optionsApplied++;
            var chainChanged = _CountChainChanged(
                viewModel,
                () => viewModel.ResetSelectedToDefaultsCommand.Execute(null)
            );

            Assert.Same(filterBefore, viewModel.Steps[0].Filter);
            Assert.Equal(0, chainChanged);
            Assert.Equal(0, optionsApplied);
        }

        /// <summary>
        /// Verifies reset requires exactly one selected step (Filter Configuration / Filter Options parity).
        /// </summary>
        [Fact]
        public void ResetSelectedToDefaults_disabled_for_multi_select()
        {
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            viewModel.SetSelectedSteps([]);
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            viewModel.SetSelectedSteps([viewModel.Steps[0], viewModel.Steps[1]]);

            Assert.False(viewModel.ResetSelectedToDefaultsCommand.CanExecute(null));
            Assert.False(viewModel.CanShowFilterOptions);
        }

        /// <summary>
        /// Verifies save-as-default persists options used on the next palette add.
        /// </summary>
        [Fact]
        public void SaveSelectedAsDefault_applies_on_next_add()
        {
            var defaultsPath = Path.Combine(Path.GetTempPath(), $"mfr-filter-defaults-test-{Guid.NewGuid():N}.json");
            try
            {
                var store = new FilterDefaultsStore(defaultsPath);
                var viewModel = new AppliedFiltersViewModel(store);
                viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
                var customized = new LettersCaseFilter(
                    new FileExtensionTarget(),
                    new LettersCaseOptions(LettersCaseMode.UpperCase, [])
                );
                viewModel.Steps[0].SetFilter(customized);

                string? savedName = null;
                viewModel.FilterDefaultSaved += (_, name) => savedName = name;
                viewModel.SaveSelectedAsDefaultCommand.Execute(null);

                Assert.Equal("Letters Case", savedName);

                viewModel.Clear();
                viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
                var added = Assert.IsType<LettersCaseFilter>(viewModel.Steps[0].Filter);
                Assert.IsType<FileExtensionTarget>(added.Target);
                Assert.Equal(LettersCaseMode.UpperCase, added.Options.Mode);
                Assert.True(viewModel.Steps[0].Enabled);
            }
            finally
            {
                if (File.Exists(defaultsPath))
                {
                    File.Delete(defaultsPath);
                }
            }
        }

        /// <summary>
        /// Verifies reset restores factory defaults even when a saved type default exists.
        /// </summary>
        [Fact]
        public void ResetSelectedToDefaults_ignores_saved_type_default()
        {
            var defaultsPath = Path.Combine(Path.GetTempPath(), $"mfr-filter-defaults-test-{Guid.NewGuid():N}.json");
            try
            {
                var store = new FilterDefaultsStore(defaultsPath);
                store.SetDefault(
                    new LettersCaseFilter(
                        new FileExtensionTarget(),
                        new LettersCaseOptions(LettersCaseMode.UpperCase, [])
                    )
                );

                var viewModel = new AppliedFiltersViewModel(store);
                viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
                Assert.Equal(LettersCaseMode.UpperCase, ((LettersCaseFilter)viewModel.Steps[0].Filter).Options.Mode);

                viewModel.ResetSelectedToDefaultsCommand.Execute(null);
                Assert.Equal(new LettersCaseFilter(), viewModel.Steps[0].Filter);
            }
            finally
            {
                if (File.Exists(defaultsPath))
                {
                    File.Delete(defaultsPath);
                }
            }
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

        /// <summary>
        /// Verifies inserting several catalog rows raises <see cref="AppliedFiltersViewModel.ChainChanged"/> once.
        /// </summary>
        [Fact]
        public void InsertFromCatalogAt_raises_chain_changed_once()
        {
            var viewModel = new AppliedFiltersViewModel();
            var count = _CountChainChanged(
                viewModel,
                () =>
                    viewModel.InsertFromCatalogAt(
                        [AppliedFiltersTestUi.Entry("LettersCase"), AppliedFiltersTestUi.Entry("TagRemover")],
                        insertIndex: 0
                    )
            );

            Assert.Equal(1, count);
        }

        /// <summary>
        /// Verifies removing several steps raises <see cref="AppliedFiltersViewModel.ChainChanged"/> once.
        /// </summary>
        [Fact]
        public void RemoveStepsAtIndices_raises_chain_changed_once()
        {
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            viewModel.SetSelectedSteps([]);
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));

            var count = _CountChainChanged(viewModel, () => viewModel.RemoveStepsAtIndices([0, 1]));

            Assert.Equal(1, count);
            Assert.Empty(viewModel.Steps);
        }

        /// <summary>
        /// Verifies a drag-reorder raises <see cref="AppliedFiltersViewModel.ChainChanged"/> once.
        /// </summary>
        [Fact]
        public void MoveStepsTo_raises_chain_changed_once()
        {
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            viewModel.SetSelectedSteps([]);
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));

            var count = _CountChainChanged(viewModel, () => viewModel.MoveStepsTo([1], targetIndex: 0));

            Assert.Equal(1, count);
        }

        /// <summary>
        /// Verifies a neighbor-swap move raises <see cref="AppliedFiltersViewModel.ChainChanged"/> once.
        /// </summary>
        [Fact]
        public void MoveSelectedUp_raises_chain_changed_once()
        {
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            viewModel.SetSelectedSteps([]);
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            viewModel.SetSelectedSteps([viewModel.Steps[1]]);

            var count = _CountChainChanged(viewModel, () => viewModel.MoveSelectedUpCommand.Execute(null));

            Assert.Equal(1, count);
        }

        /// <summary>
        /// Verifies renaming a step does not raise <see cref="AppliedFiltersViewModel.ChainChanged"/>.
        /// </summary>
        [Fact]
        public void SetDisplayName_does_not_raise_chain_changed()
        {
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));

            var count = _CountChainChanged(viewModel, () => viewModel.Steps[0].SetDisplayName("Custom"));

            Assert.Equal(0, count);
        }

        /// <summary>
        /// Verifies a fresh pane has no last-loaded preset.
        /// </summary>
        [Fact]
        public void LastLoaded_Is_Null_Initially()
        {
            var viewModel = new AppliedFiltersViewModel();
            Assert.Null(viewModel.LastLoaded);
        }

        /// <summary>
        /// Verifies <see cref="AppliedFiltersViewModel.SetLastLoaded"/> records and clears last-loaded.
        /// </summary>
        [Fact]
        public void SetLastLoaded_Records_And_Clears()
        {
            var manager = PresetManager.CreateEmpty();
            var preset = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "Demo",
                Chain = new FilterChain { Steps = [] },
            };
            manager.NameToPreset[preset.Name] = preset;
            var viewModel = new AppliedFiltersViewModel(presetManager: manager);

            viewModel.SetLastLoaded(preset);
            Assert.Same(preset, viewModel.LastLoaded);

            viewModel.SetLastLoaded(null);
            Assert.Null(viewModel.LastLoaded);
        }

        /// <summary>
        /// Verifies <see cref="AppliedFiltersViewModel.ReplaceFromChain"/> rebuilds steps, copies enabled flags,
        /// uses catalog display names, selects the first step, raises <see cref="AppliedFiltersViewModel.ChainChanged"/> once,
        /// and does not set <see cref="AppliedFiltersViewModel.LastLoaded"/> (preset load sets last-loaded after replace).
        /// </summary>
        [Fact]
        public void ReplaceFromChain_Rebuilds_With_Catalog_Names_And_Single_ChainChanged()
        {
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            viewModel.Steps[0].SetDisplayName("Custom Label");

            var letters = new LettersCaseFilter();
            var shrink = new ShrinkSpacesFilter();
            var chain = new FilterChain
            {
                Steps =
                [
                    new FilterChainStep(Enabled: false, Filter: letters),
                    new FilterChainStep(Enabled: true, Filter: shrink),
                    new FilterChainStep(Enabled: true, Filter: new LettersCaseFilter()),
                ],
            };

            var count = _CountChainChanged(viewModel, () => viewModel.ReplaceFromChain(chain));

            Assert.Equal(1, count);
            Assert.Equal(3, viewModel.Count);
            Assert.Equal("Letters Case", viewModel.Steps[0].DisplayName);
            Assert.False(viewModel.Steps[0].Enabled);
            Assert.Equal("Shrink Spaces", viewModel.Steps[1].DisplayName);
            Assert.True(viewModel.Steps[1].Enabled);
            Assert.Equal("Letters Case (2)", viewModel.Steps[2].DisplayName);
            Assert.Equal([viewModel.Steps[0]], viewModel.SelectedSteps);
            Assert.Same(letters, viewModel.Steps[0].Filter);
            Assert.Same(shrink, viewModel.Steps[1].Filter);
            Assert.Null(viewModel.LastLoaded);
        }

        /// <summary>
        /// Verifies replacing with an empty chain clears selection and raises one change when steps existed.
        /// </summary>
        [Fact]
        public void ReplaceFromChain_Empty_Clears_Stack()
        {
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));

            var count = _CountChainChanged(viewModel, () => viewModel.ReplaceFromChain(new FilterChain { Steps = [] }));

            Assert.Equal(1, count);
            Assert.Equal(0, viewModel.Count);
            Assert.Empty(viewModel.SelectedSteps);
        }

        /// <summary>
        /// Verifies empty-to-empty replace is a no-op for <see cref="AppliedFiltersViewModel.ChainChanged"/>.
        /// </summary>
        [Fact]
        public void ReplaceFromChain_Empty_When_Already_Empty_Raises_No_ChainChanged()
        {
            var viewModel = new AppliedFiltersViewModel();
            var count = _CountChainChanged(viewModel, () => viewModel.ReplaceFromChain(new FilterChain { Steps = [] }));
            Assert.Equal(0, count);
        }

        /// <summary>
        /// Verifies replaced steps no longer raise <see cref="AppliedFiltersViewModel.ChainChanged"/> after detach.
        /// </summary>
        [Fact]
        public void ReplaceFromChain_Detaches_Old_Step_Handlers()
        {
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            var oldStep = viewModel.Steps[0];

            viewModel.ReplaceFromChain(
                new FilterChain { Steps = [new FilterChainStep(Enabled: true, Filter: new LettersCaseFilter())] }
            );

            var count = _CountChainChanged(viewModel, () => oldStep.Enabled = false);
            Assert.Equal(0, count);
        }

        /// <summary>
        /// Verifies Save Preset upserts a new preset and sets last-loaded.
        /// </summary>
        [Fact]
        public void SavePreset_Creates_Preset_And_Sets_LastLoaded()
        {
            var manager = PresetManager.CreateEmpty();
            var viewModel = new AppliedFiltersViewModel(presetManager: manager);
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));

            Assert.Null(viewModel.LastLoaded);

            var saved = viewModel.SavePreset("My Preset", "A demo", visibleColumns: null);

            Assert.NotNull(saved);
            Assert.Equal("My Preset", saved.Name);
            Assert.Equal("A demo", saved.Description);
            Assert.Null(saved.VisibleColumns);
            Assert.Single(saved.Chain.Steps);
            Assert.True(manager.NameToPreset.ContainsKey("My Preset"));
            Assert.Same(saved, viewModel.LastLoaded);
            Assert.True(File.Exists(manager.PresetsFilePath));
        }

        /// <summary>
        /// Verifies Save Preset overwrite keeps the existing Id and can store columns when requested.
        /// </summary>
        [Fact]
        public void SavePreset_Overwrite_Keeps_Id_And_Stores_Columns_When_Provided()
        {
            var manager = PresetManager.CreateEmpty();
            var existingId = Guid.NewGuid();
            var existing = new FilterPreset
            {
                Id = existingId,
                Name = "KeepMe",
                Description = "old",
                Chain = new FilterChain { Steps = [] },
                VisibleColumns = null,
            };
            manager.NameToPreset[existing.Name] = existing;

            var viewModel = new AppliedFiltersViewModel(presetManager: manager);
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            var columns = new List<RenameListVisibleColumnSpec>
            {
                new(
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName),
                    Width: 220
                ),
            };

            var saved = viewModel.SavePreset("KeepMe", "new desc", columns);

            Assert.NotNull(saved);
            Assert.Equal(existingId, saved.Id);
            Assert.Equal("KeepMe", saved.Name);
            Assert.Equal("new desc", saved.Description);
            Assert.NotNull(saved.VisibleColumns);
            Assert.Single(saved.VisibleColumns);
            Assert.Equal(220, saved.VisibleColumns[0].Width);
            Assert.Single(saved.Chain.Steps);
            Assert.Same(saved, viewModel.LastLoaded);
        }

        /// <summary>
        /// Verifies Save Preset rejects a blank name.
        /// </summary>
        [Fact]
        public void SavePreset_Rejects_Blank_Name()
        {
            var manager = PresetManager.CreateEmpty();
            var viewModel = new AppliedFiltersViewModel(presetManager: manager);

            Assert.Null(viewModel.SavePreset("   ", description: null, visibleColumns: null));
            Assert.Empty(manager.NameToPreset);
            Assert.Null(viewModel.LastLoaded);
        }

        /// <summary>
        /// Verifies Save Preset overwrite with null columns clears previously stored columns.
        /// </summary>
        [Fact]
        public void SavePreset_Overwrite_With_Null_Columns_Clears_Prior()
        {
            var manager = PresetManager.CreateEmpty();
            var existingId = Guid.NewGuid();
            var existing = new FilterPreset
            {
                Id = existingId,
                Name = "ClearCols",
                Description = "old",
                Chain = new FilterChain { Steps = [] },
                VisibleColumns =
                [
                    new(
                        RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName),
                        Width: 90
                    ),
                ],
            };
            manager.NameToPreset[existing.Name] = existing;

            var viewModel = new AppliedFiltersViewModel(presetManager: manager);
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));

            var saved = viewModel.SavePreset("ClearCols", "new desc", visibleColumns: null);

            Assert.NotNull(saved);
            Assert.Equal(existingId, saved.Id);
            Assert.Equal("new desc", saved.Description);
            Assert.Null(saved.VisibleColumns);
            Assert.Single(saved.Chain.Steps);
        }

        /// <summary>
        /// Verifies <see cref="AppliedFiltersViewModel.LoadPreset"/> replaces the chain and sets last-loaded.
        /// </summary>
        [Fact]
        public void LoadPreset_Replaces_Chain_And_Sets_LastLoaded()
        {
            var manager = PresetManager.CreateEmpty();
            var letters = new LettersCaseFilter();
            var preset = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "LoadMe",
                Description = "demo",
                Chain = new FilterChain { Steps = [new FilterChainStep(Enabled: true, Filter: letters)] },
            };
            manager.NameToPreset[preset.Name] = preset;
            var viewModel = new AppliedFiltersViewModel(presetManager: manager);
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            Assert.Null(viewModel.LastLoaded);

            viewModel.LoadPreset(preset);

            Assert.Same(preset, viewModel.LastLoaded);
            Assert.Single(viewModel.Steps);
            Assert.Equal("Letters Case", viewModel.Steps[0].DisplayName);
            Assert.Same(letters, viewModel.Steps[0].Filter);
        }

        /// <summary>
        /// Verifies confirm-replace is required only when the flag is on and the stack is non-empty.
        /// </summary>
        [Fact]
        public void NeedsConfirmReplaceOnLoad_Requires_Flag_And_NonEmpty_Stack()
        {
            var viewModel = new AppliedFiltersViewModel();
            Assert.False(viewModel.NeedsConfirmReplaceOnLoad(confirmReplaceOnLoad: true));
            Assert.False(viewModel.NeedsConfirmReplaceOnLoad(confirmReplaceOnLoad: false));

            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            Assert.True(viewModel.NeedsConfirmReplaceOnLoad(confirmReplaceOnLoad: true));
            Assert.False(viewModel.NeedsConfirmReplaceOnLoad(confirmReplaceOnLoad: false));
        }

        /// <summary>
        /// Verifies deleting the last-loaded preset clears last-loaded.
        /// </summary>
        [Fact]
        public void DeletePreset_Clears_LastLoaded_When_Deleted()
        {
            var manager = PresetManager.CreateEmpty();
            var preset = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "Gone",
                Chain = new FilterChain { Steps = [] },
            };
            manager.NameToPreset[preset.Name] = preset;
            var other = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "Keep",
                Chain = new FilterChain { Steps = [] },
            };
            manager.NameToPreset[other.Name] = other;
            var viewModel = new AppliedFiltersViewModel(presetManager: manager);
            viewModel.SetLastLoaded(preset);
            Assert.Same(preset, viewModel.LastLoaded);

            Assert.True(viewModel.DeletePreset("Gone"));

            Assert.Null(viewModel.LastLoaded);
            Assert.False(manager.NameToPreset.ContainsKey("Gone"));
            Assert.True(manager.NameToPreset.ContainsKey("Keep"));
        }

        /// <summary>
        /// Verifies renaming the last-loaded preset updates its name.
        /// </summary>
        [Fact]
        public void RenamePreset_Updates_LastLoaded_Name()
        {
            var manager = PresetManager.CreateEmpty();
            var id = Guid.NewGuid();
            var preset = new FilterPreset
            {
                Id = id,
                Name = "Old",
                Description = "keep",
                Chain = new FilterChain { Steps = [] },
            };
            manager.NameToPreset[preset.Name] = preset;
            var viewModel = new AppliedFiltersViewModel(presetManager: manager);
            viewModel.SetLastLoaded(preset);

            var result = viewModel.RenamePreset("Old", "New");

            Assert.Equal(PresetRenameStatus.Success, result.Status);
            Assert.NotNull(result.Preset);
            Assert.Equal(id, result.Preset.Id);
            Assert.Equal("New", result.Preset.Name);
            Assert.Equal("keep", result.Preset.Description);
            Assert.Same(result.Preset, viewModel.LastLoaded);
            Assert.False(manager.NameToPreset.ContainsKey("Old"));
            Assert.True(manager.NameToPreset.ContainsKey("New"));
        }

        /// <summary>
        /// Verifies rename validation: blank, same name, and name taken.
        /// </summary>
        [Fact]
        public void RenamePreset_Validates_Blank_Unchanged_And_Taken()
        {
            var manager = PresetManager.CreateEmpty();
            var alpha = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "Alpha",
                Chain = new FilterChain { Steps = [] },
            };
            var beta = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "Beta",
                Chain = new FilterChain { Steps = [] },
            };
            manager.NameToPreset[alpha.Name] = alpha;
            manager.NameToPreset[beta.Name] = beta;
            var viewModel = new AppliedFiltersViewModel(presetManager: manager);

            Assert.Equal(PresetRenameStatus.BlankName, viewModel.RenamePreset("Alpha", "   ").Status);
            Assert.Equal(PresetRenameStatus.Unchanged, viewModel.RenamePreset("Alpha", "Alpha").Status);
            Assert.Equal(PresetRenameStatus.NameTaken, viewModel.RenamePreset("Alpha", "Beta").Status);
            Assert.Equal(PresetRenameStatus.NotFound, viewModel.RenamePreset("Missing", "Other").Status);
            Assert.True(manager.NameToPreset.ContainsKey("Alpha"));
        }

        /// <summary>
        /// Verifies deleting a preset that is not last-loaded leaves last-loaded unchanged.
        /// </summary>
        [Fact]
        public void DeletePreset_Leaves_LastLoaded_When_Other_Deleted()
        {
            var manager = PresetManager.CreateEmpty();
            var keep = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "Keep",
                Chain = new FilterChain { Steps = [] },
            };
            var gone = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "Gone",
                Chain = new FilterChain { Steps = [] },
            };
            manager.NameToPreset[keep.Name] = keep;
            manager.NameToPreset[gone.Name] = gone;
            var viewModel = new AppliedFiltersViewModel(presetManager: manager);
            viewModel.SetLastLoaded(keep);

            Assert.True(viewModel.DeletePreset("Gone"));

            Assert.Same(keep, viewModel.LastLoaded);
            Assert.False(manager.NameToPreset.ContainsKey("Gone"));
        }

        /// <summary>
        /// Counts <see cref="AppliedFiltersViewModel.ChainChanged"/> raises during <paramref name="action"/>.
        /// </summary>
        private static int _CountChainChanged(AppliedFiltersViewModel viewModel, Action action)
        {
            var count = 0;
            void OnChanged(object? sender, EventArgs e)
            {
                count++;
            }

            viewModel.ChainChanged += OnChanged;
            try
            {
                action();
            }
            finally
            {
                viewModel.ChainChanged -= OnChanged;
            }

            return count;
        }
    }
}
