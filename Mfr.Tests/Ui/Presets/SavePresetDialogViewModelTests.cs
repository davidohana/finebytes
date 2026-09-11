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
        /// Verifies an empty dialog starts with blank fields and disabled Save.
        /// </summary>
        [Fact]
        public void Default_Starts_Empty_And_Cannot_Save()
        {
            var viewModel = new SavePresetDialogViewModel();

            Assert.False(viewModel.CanSave);
            Assert.Equal(string.Empty, viewModel.Name);
            Assert.Equal(string.Empty, viewModel.Description);
            Assert.False(viewModel.SaveRenameListColumns);
            Assert.Empty(viewModel.ExistingNames);
        }

        /// <summary>
        /// Verifies last-loaded prefills name, description, and columns checkbox.
        /// </summary>
        [Fact]
        public void Prefills_From_LastLoaded()
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

            var viewModel = new SavePresetDialogViewModel(lastLoaded, existingPresets: [lastLoaded]);

            Assert.Equal("Rock", viewModel.Name);
            Assert.Equal("Loud", viewModel.Description);
            Assert.True(viewModel.SaveRenameListColumns);
            Assert.True(viewModel.CanSave);
            Assert.Equal("Rock", viewModel.TrimmedName);
            Assert.Equal("Loud", viewModel.TrimmedDescriptionOrNull);
        }

        /// <summary>
        /// Verifies blank description becomes null and whitespace-only name cannot save.
        /// </summary>
        [Fact]
        public void Trimmed_Helpers_And_CanSave_Track_Whitespace()
        {
            var viewModel = new SavePresetDialogViewModel { Name = "  ", Description = "  " };

            Assert.False(viewModel.CanSave);
            Assert.Equal(string.Empty, viewModel.TrimmedName);
            Assert.Null(viewModel.TrimmedDescriptionOrNull);

            viewModel.Name = "  Demo  ";
            viewModel.Description = "  Notes  ";

            Assert.True(viewModel.CanSave);
            Assert.Equal("Demo", viewModel.TrimmedName);
            Assert.Equal("Notes", viewModel.TrimmedDescriptionOrNull);
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

            var viewModel = new SavePresetDialogViewModel(lastLoaded);

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
                existingPresets: [lastLoaded],
                prefillFromLastLoaded: false
            );

            Assert.Equal(string.Empty, viewModel.Name);
            Assert.Equal(string.Empty, viewModel.Description);
            Assert.False(viewModel.SaveRenameListColumns);
            Assert.False(viewModel.CanSave);
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
        /// Verifies choosing an existing suggestion prefills description and columns from that preset.
        /// </summary>
        [Fact]
        public void ApplySuggestion_Prefills_Description_And_Columns()
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

            var viewModel = new SavePresetDialogViewModel(lastLoaded, existingPresets: [lastLoaded, other]);

            Assert.Equal("mine", viewModel.Description);
            Assert.False(viewModel.SaveRenameListColumns);

            viewModel.Name = "Other";
            viewModel.ApplySuggestion("Other");

            Assert.Equal("from other", viewModel.Description);
            Assert.True(viewModel.SaveRenameListColumns);
        }

        /// <summary>
        /// Verifies typing an existing name does not wipe description/columns (selection-only prefill).
        /// </summary>
        [Fact]
        public void Typing_Existing_Name_Does_Not_Prefill()
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

            var viewModel = new SavePresetDialogViewModel(lastLoaded, existingPresets: [lastLoaded, other])
            {
                Description = "keep my edits",
                SaveRenameListColumns = false,

                Name = "Other"
            };

            Assert.Equal("keep my edits", viewModel.Description);
            Assert.False(viewModel.SaveRenameListColumns);
        }
    }
}
