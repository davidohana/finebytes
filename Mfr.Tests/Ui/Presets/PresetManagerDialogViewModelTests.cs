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
        /// Verifies presets are listed sorted by name and selection drives the description pane.
        /// </summary>
        [Fact]
        public void Refresh_Sorts_And_Exposes_Selected_Description()
        {
            var manager = PresetManager.CreateEmpty();
            manager.NameToPreset["Zebra"] = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "Zebra",
                Description = "last",
                Chain = new FilterChain { Steps = [] },
            };
            manager.NameToPreset["Alpha"] = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "Alpha",
                Description = "first",
                Chain = new FilterChain { Steps = [] },
            };
            var applied = new AppliedFiltersViewModel(presetManager: manager);
            var viewModel = new PresetManagerDialogViewModel(applied);

            Assert.Equal(["Alpha", "Zebra"], viewModel.Presets.Select(preset => preset.Name));
            Assert.Equal("Alpha", viewModel.SelectedPreset?.Name);
            Assert.Equal("first", viewModel.SelectedDescription);
            Assert.True(viewModel.HasSelection);

            viewModel.SelectedPreset = viewModel.Presets[1];
            Assert.Equal("last", viewModel.SelectedDescription);
        }

        /// <summary>
        /// Verifies Refresh keeps selection by name after a rename-style dictionary update.
        /// </summary>
        [Fact]
        public void Refresh_Preserves_Selection_By_Name()
        {
            var manager = PresetManager.CreateEmpty();
            manager.NameToPreset["A"] = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "A",
                Description = "a",
                Chain = new FilterChain { Steps = [] },
            };
            manager.NameToPreset["B"] = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "B",
                Description = "b",
                Chain = new FilterChain { Steps = [] },
            };
            var applied = new AppliedFiltersViewModel(presetManager: manager);
            var viewModel = new PresetManagerDialogViewModel(applied);
            viewModel.SelectedPreset = viewModel.Presets.First(preset => preset.Name == "B");

            var renamed = manager.NameToPreset["B"] with { Name = "B2", Description = "b2" };
            manager.NameToPreset.Remove("B");
            manager.NameToPreset["B2"] = renamed;
            viewModel.Refresh(preferredName: "B2");

            Assert.Equal("B2", viewModel.SelectedPreset?.Name);
            Assert.Equal("b2", viewModel.SelectedDescription);
            Assert.Equal(["A", "B2"], viewModel.Presets.Select(preset => preset.Name));
        }
    }
}
