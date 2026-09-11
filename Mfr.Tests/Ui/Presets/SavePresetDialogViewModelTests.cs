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
        /// Verifies an empty dialog starts with blank fields and disabled OK.
        /// </summary>
        [Fact]
        public void Default_Starts_Empty_And_Cannot_Confirm()
        {
            var viewModel = new SavePresetDialogViewModel();

            Assert.Equal(string.Empty, viewModel.Name);
            Assert.Equal(string.Empty, viewModel.Description);
            Assert.False(viewModel.SaveRenameListColumns);
            Assert.False(viewModel.CanConfirm);
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

            var viewModel = new SavePresetDialogViewModel(lastLoaded);

            Assert.Equal("Rock", viewModel.Name);
            Assert.Equal("Loud", viewModel.Description);
            Assert.True(viewModel.SaveRenameListColumns);
            Assert.True(viewModel.CanConfirm);
            Assert.Equal("Rock", viewModel.TrimmedName);
            Assert.Equal("Loud", viewModel.TrimmedDescriptionOrNull);
        }

        /// <summary>
        /// Verifies blank description becomes null and whitespace-only name cannot confirm.
        /// </summary>
        [Fact]
        public void Trimmed_Helpers_And_CanConfirm_Track_Whitespace()
        {
            var viewModel = new SavePresetDialogViewModel { Name = "  ", Description = "  " };

            Assert.False(viewModel.CanConfirm);
            Assert.Equal(string.Empty, viewModel.TrimmedName);
            Assert.Null(viewModel.TrimmedDescriptionOrNull);

            viewModel.Name = "  Demo  ";
            viewModel.Description = "  Notes  ";

            Assert.True(viewModel.CanConfirm);
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
    }
}
