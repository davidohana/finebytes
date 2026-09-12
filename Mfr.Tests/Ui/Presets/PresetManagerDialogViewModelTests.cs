using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.Presets;

namespace Mfr.Tests.Ui.Presets
{
    /// <summary>
    /// Unit tests for <see cref="PresetManagerDialogViewModel"/>.
    /// </summary>
    public sealed class PresetManagerDialogViewModelTests
    {
        /// <summary>
        /// Verifies presets follow stored order and single selection drives the description pane.
        /// </summary>
        [Fact]
        public void Refresh_Uses_Stored_Order_And_Exposes_Selected_Description()
        {
            var (_, viewModel) = _CreateWithPresets(("Zebra", "last"), ("Alpha", "first"));

            Assert.Equal(["Zebra", "Alpha"], viewModel.Presets.Select(preset => preset.Name));
            Assert.Equal(["Zebra"], viewModel.SelectedPresets.Select(preset => preset.Name));
            Assert.Equal("last", viewModel.SelectedDescription);
            Assert.True(viewModel.HasSelection);
            Assert.True(viewModel.HasSingleSelection);

            viewModel.SetSelectedPresets([viewModel.Presets[1]]);
            Assert.Equal("first", viewModel.SelectedDescription);
        }

        /// <summary>
        /// Verifies Refresh keeps selection by name after a rename-style dictionary update.
        /// </summary>
        [Fact]
        public void Refresh_Preserves_Selection_By_Name()
        {
            var (manager, viewModel) = _CreateWithPresets(("A", "a"), ("B", "b"));
            viewModel.SetSelectedPresets([viewModel.Presets.First(preset => preset.Name == "B")]);

            var renamed = manager.NameToPreset["B"] with { Name = "B2", Description = "b2" };
            Assert.True(manager.TryRename("B", renamed));
            viewModel.Refresh(preferredName: "B2");

            Assert.Equal(["B2"], viewModel.SelectedPresets.Select(preset => preset.Name));
            Assert.Equal("b2", viewModel.SelectedDescription);
            Assert.Equal(["A", "B2"], viewModel.Presets.Select(preset => preset.Name));
        }

        /// <summary>
        /// Verifies Load/Rename require exactly one selection; Delete any selection; description only when single.
        /// </summary>
        [Fact]
        public void Selection_Gates_Load_Rename_Delete_And_Description()
        {
            var (_, viewModel) = _CreateWithPresets("A", "B", "C");

            viewModel.SetSelectedPresets([]);
            Assert.False(viewModel.HasSelection);
            Assert.False(viewModel.HasSingleSelection);
            Assert.Equal(string.Empty, viewModel.SelectedDescription);

            viewModel.SetSelectedPresets([viewModel.Presets[0], viewModel.Presets[1]]);
            Assert.True(viewModel.HasSelection);
            Assert.False(viewModel.HasSingleSelection);
            Assert.Equal(string.Empty, viewModel.SelectedDescription);

            viewModel.SetSelectedPresets([viewModel.Presets[1]]);
            Assert.True(viewModel.HasSelection);
            Assert.True(viewModel.HasSingleSelection);
            Assert.Equal("desc-B", viewModel.SelectedDescription);
        }

        /// <summary>
        /// Verifies Up/Down reorder selected presets, persist, and restore multi-selection.
        /// </summary>
        [Fact]
        public void MoveSelected_Reorders_Persists_And_Keeps_Selection()
        {
            var (manager, viewModel) = _CreateWithPresets("A", "B", "C");
            viewModel.SetSelectedPresets([viewModel.Presets[0], viewModel.Presets[1]]);

            Assert.True(viewModel.MoveSelectedDownCommand.CanExecute(null));
            viewModel.MoveSelectedDownCommand.Execute(null);

            Assert.Equal(["C", "A", "B"], viewModel.Presets.Select(preset => preset.Name));
            Assert.Equal(["A", "B"], viewModel.SelectedPresets.Select(preset => preset.Name));
            Assert.Equal(["C", "A", "B"], manager.Presets.Select(preset => preset.Name));

            Assert.False(viewModel.MoveSelectedDownCommand.CanExecute(null));
            Assert.True(viewModel.MoveSelectedUpCommand.CanExecute(null));
            viewModel.MoveSelectedUpCommand.Execute(null);

            Assert.Equal(["A", "B", "C"], viewModel.Presets.Select(preset => preset.Name));
            Assert.Equal(["A", "B"], viewModel.SelectedPresets.Select(preset => preset.Name));
        }

        private static (PresetManager Manager, PresetManagerDialogViewModel ViewModel) _CreateWithPresets(
            params string[] names
        )
        {
            return _CreateWithPresets([.. names.Select(name => (name, $"desc-{name}"))]);
        }

        private static (PresetManager Manager, PresetManagerDialogViewModel ViewModel) _CreateWithPresets(
            params (string Name, string Description)[] presets
        )
        {
            var manager = PresetManager.CreateEmpty();
            foreach (var (name, description) in presets)
            {
                manager.Upsert(
                    new FilterPreset
                    {
                        Id = Guid.NewGuid(),
                        Name = name,
                        Description = description,
                        Chain = new FilterChain { Steps = [] },
                    }
                );
            }

            return (manager, new PresetManagerDialogViewModel(new AppliedFiltersViewModel(presetManager: manager)));
        }
    }
}
