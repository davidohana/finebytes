using System.Text.Json;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Space;
using Mfr.Models.Filters;

namespace Mfr.Tests.Ui.AppliedFilters
{
    /// <summary>
    /// Unit tests for <see cref="FilterOptionsDialogViewModel"/> and <see cref="AppliedFiltersViewModel.ApplyFilterOptions"/>.
    /// </summary>
    public sealed class FilterOptionsDialogViewModelTests
    {
        /// <summary>
        /// Verifies string-target filters expose Apply-To in the dialog draft.
        /// </summary>
        [Fact]
        public void Dialog_initializes_apply_to_for_string_filters()
        {
            var step = new AppliedFilterStepViewModel("Shrink Spaces", new ShrinkSpacesFilter());
            var dialog = new FilterOptionsDialogViewModel(step);

            Assert.Equal("Shrink Spaces", dialog.Name);
            Assert.True(dialog.HasApplyTo);
            Assert.Equal(FilterTargetOption.All[0], dialog.SelectedApplyTo);
        }

        /// <summary>
        /// Verifies non-string filters hide Apply-To in the dialog draft.
        /// </summary>
        [Fact]
        public void Dialog_hides_apply_to_for_non_string_filters()
        {
            var step = new AppliedFilterStepViewModel(
                "Audio Tag Remover",
                new Filters.Audio.TagRemoverFilter()
            );
            var dialog = new FilterOptionsDialogViewModel(step);

            Assert.False(dialog.HasApplyTo);
            Assert.Null(dialog.SelectedApplyTo);
        }

        /// <summary>
        /// Verifies accepting Apply-To edits updates the step and <see cref="AppliedFiltersViewModel.ToChain"/>.
        /// </summary>
        [Fact]
        public void ApplyFilterOptions_updates_step_target_and_chain_json()
        {
            var applied = new AppliedFiltersViewModel();
            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            var dialog = new FilterOptionsDialogViewModel(applied.Steps[0]) { SelectedApplyTo = FilterTargetOption.All[1] };

            applied.ApplyFilterOptions(dialog);

            Assert.Equal("Extension", applied.Steps[0].ApplyToLabel);
            var filter = (ShrinkSpacesFilter)applied.ToChain().Steps[0].Filter;
            Assert.IsType<FileExtensionTarget>(filter.Target);

            var json = JsonSerializer.Serialize<BaseFilter>(filter, PresetJsonOptions.Default);
            Assert.Contains("\"targetType\": \"FileExtension\"", json, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies accepting a name edit updates the list label.
        /// </summary>
        [Fact]
        public void ApplyFilterOptions_updates_display_name()
        {
            var applied = new AppliedFiltersViewModel();
            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            var dialog = new FilterOptionsDialogViewModel(applied.Steps[0]) { Name = "Cleaner" };

            applied.ApplyFilterOptions(dialog);

            Assert.Equal("Cleaner", applied.Steps[0].DisplayName);
        }
    }
}
