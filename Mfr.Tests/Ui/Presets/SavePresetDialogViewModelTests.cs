using Mfr.App.Ui.ViewModels.Presets;
using Mfr.Models.RenameList.Fields.Basic;

namespace Mfr.Tests.Ui.Presets
{
    /// <summary>
    /// Unit tests for <see cref="SavePresetDialogViewModel"/>.
    /// </summary>
    public sealed class SavePresetDialogViewModelTests
    {
        /// <summary>
        /// Verifies an empty dialog starts with blank fields and only Save as new gated off.
        /// </summary>
        [Fact]
        public void Default_Starts_Empty_With_Actions_Disabled()
        {
            var viewModel = new SavePresetDialogViewModel();

            Assert.False(viewModel.CanUpdate);
            Assert.False(viewModel.CanUpdateAction);
            Assert.False(viewModel.CanSaveAsAction);
            Assert.Equal(string.Empty, viewModel.Name);
            Assert.Equal(string.Empty, viewModel.Description);
            Assert.False(viewModel.SaveRenameListColumns);
        }

        /// <summary>
        /// Verifies can-update prefills fields and enables Update when the name is present.
        /// </summary>
        [Fact]
        public void Prefills_From_LastLoaded_And_Enables_Update()
        {
            var lastLoaded = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "Rock",
                Description = "Loud",
                Chain = new FilterChain { Steps = [] },
                VisibleColumns =
                [
                    new(RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName)),
                ],
            };

            var viewModel = new SavePresetDialogViewModel(lastLoaded, canUpdate: true);

            Assert.True(viewModel.CanUpdate);
            Assert.True(viewModel.CanUpdateAction);
            Assert.True(viewModel.CanSaveAsAction);
            Assert.Equal("Rock", viewModel.OriginalName);
            Assert.Equal("Rock", viewModel.Name);
            Assert.Equal("Loud", viewModel.Description);
            Assert.True(viewModel.SaveRenameListColumns);
            Assert.Equal("Rock", viewModel.TrimmedName);
            Assert.Equal("Loud", viewModel.TrimmedDescriptionOrNull);
        }

        /// <summary>
        /// Verifies last-loaded without can-update still prefills but keeps Update disabled.
        /// </summary>
        [Fact]
        public void LastLoaded_Without_CanUpdate_Disables_Update()
        {
            var lastLoaded = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "Gone",
                Chain = new FilterChain { Steps = [] },
            };

            var viewModel = new SavePresetDialogViewModel(lastLoaded, canUpdate: false);

            Assert.False(viewModel.CanUpdate);
            Assert.False(viewModel.CanUpdateAction);
            Assert.True(viewModel.CanSaveAsAction);
            Assert.Equal("Gone", viewModel.Name);
        }

        /// <summary>
        /// Verifies blank description becomes null and whitespace-only name disables both actions.
        /// </summary>
        [Fact]
        public void Trimmed_Helpers_And_Actions_Track_Whitespace()
        {
            var viewModel = new SavePresetDialogViewModel { Name = "  ", Description = "  " };

            Assert.False(viewModel.CanSaveAsAction);
            Assert.False(viewModel.CanUpdateAction);
            Assert.Equal(string.Empty, viewModel.TrimmedName);
            Assert.Null(viewModel.TrimmedDescriptionOrNull);

            viewModel.Name = "  Demo  ";
            viewModel.Description = "  Notes  ";

            Assert.True(viewModel.CanSaveAsAction);
            Assert.Equal("Demo", viewModel.TrimmedName);
            Assert.Equal("Notes", viewModel.TrimmedDescriptionOrNull);
        }

        /// <summary>
        /// Verifies Update stays disabled when can-update is true but the name is cleared.
        /// </summary>
        [Fact]
        public void Update_Requires_NonBlank_Name()
        {
            var lastLoaded = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "Live",
                Chain = new FilterChain { Steps = [] },
            };
            var viewModel = new SavePresetDialogViewModel(lastLoaded, canUpdate: true) { Name = "   " };

            Assert.True(viewModel.CanUpdate);
            Assert.False(viewModel.CanUpdateAction);
            Assert.False(viewModel.CanSaveAsAction);

            viewModel.Name = "Renamed";
            Assert.True(viewModel.CanUpdateAction);
            Assert.True(viewModel.CanSaveAsAction);
        }

        /// <summary>
        /// Verifies last-loaded without columns leaves the checkbox unchecked.
        /// </summary>
        [Fact]
        public void Prefill_Without_Columns_Leaves_Checkbox_Off()
        {
            var lastLoaded = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "Plain",
                Chain = new FilterChain { Steps = [] },
            };

            var viewModel = new SavePresetDialogViewModel(lastLoaded, canUpdate: true);

            Assert.False(viewModel.SaveRenameListColumns);
        }

        /// <summary>
        /// Verifies clearing Applied Filters leaves the Name blank even when last-loaded remains.
        /// </summary>
        [Fact]
        public void Empty_Chain_Skips_Prefill_From_LastLoaded()
        {
            var lastLoaded = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "Rock",
                Description = "Loud",
                Chain = new FilterChain { Steps = [] },
            };

            var viewModel = new SavePresetDialogViewModel(
                lastLoaded,
                canUpdate: true,
                existingPresets: [lastLoaded],
                prefillFromLastLoaded: false
            );

            Assert.True(viewModel.CanUpdate);
            Assert.Equal("Rock", viewModel.OriginalName);
            Assert.Equal(string.Empty, viewModel.Name);
            Assert.Equal(string.Empty, viewModel.Description);
            Assert.False(viewModel.SaveRenameListColumns);
            Assert.False(viewModel.CanUpdateAction);
        }

        /// <summary>
        /// Verifies existing preset names are exposed sorted for the Name suggestions list.
        /// </summary>
        [Fact]
        public void ExistingNames_Are_Sorted()
        {
            var presets = new[]
            {
                new FilterPreset
                {
                    Id = Guid.NewGuid(),
                    Name = "beta",
                    Chain = new FilterChain { Steps = [] },
                },
                new FilterPreset
                {
                    Id = Guid.NewGuid(),
                    Name = "Alpha",
                    Chain = new FilterChain { Steps = [] },
                },
                new FilterPreset
                {
                    Id = Guid.NewGuid(),
                    Name = "alpha2",
                    Chain = new FilterChain { Steps = [] },
                },
            };

            var viewModel = new SavePresetDialogViewModel(existingPresets: presets);

            Assert.Equal(["Alpha", "alpha2", "beta"], viewModel.ExistingNames);
        }

        /// <summary>
        /// Verifies choosing an existing name prefills description and columns from that preset.
        /// </summary>
        [Fact]
        public void Selecting_Existing_Name_Prefills_Description_And_Columns()
        {
            var other = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "Other",
                Description = "from other",
                Chain = new FilterChain { Steps = [] },
                VisibleColumns =
                [
                    new(RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name)),
                ],
            };
            var lastLoaded = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "Mine",
                Description = "mine",
                Chain = new FilterChain { Steps = [] },
            };

            var viewModel = new SavePresetDialogViewModel(lastLoaded, canUpdate: true, existingPresets: [lastLoaded, other]);

            Assert.Equal("mine", viewModel.Description);
            Assert.False(viewModel.SaveRenameListColumns);

            viewModel.Name = "Other";

            Assert.Equal("from other", viewModel.Description);
            Assert.True(viewModel.SaveRenameListColumns);
        }
    }
}
