using System.Text.Json;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Space;

namespace Mfr.Tests.Ui.AppliedFilters
{
    /// <summary>
    /// Unit tests for <see cref="FilterEditorViewModel"/>.
    /// </summary>
    public sealed class FilterEditorViewModelTests
    {
        /// <summary>
        /// Verifies an empty Applied selection clears the configuration title.
        /// </summary>
        [Fact]
        public void SyncSelection_with_no_selection_clears_title()
        {
            var editor = new FilterEditorViewModel();

            editor.SyncSelection([]);

            Assert.False(editor.HasSelectedStep);
            Assert.Equal(string.Empty, editor.TitleText);
        }

        /// <summary>
        /// Verifies the first selected step sets the Applied Filter title.
        /// </summary>
        [Fact]
        public void SyncSelection_with_one_step_sets_title()
        {
            var editor = new FilterEditorViewModel();
            var step = new AppliedFilterStepViewModel("Shrink Spaces", new ShrinkSpacesFilter());

            editor.SyncSelection([step]);

            Assert.True(editor.HasSelectedStep);
            Assert.Equal("Applied Filter: Shrink Spaces", editor.TitleText);
        }

        /// <summary>
        /// Verifies multi-select uses the first selected row for the title.
        /// </summary>
        [Fact]
        public void SyncSelection_with_multi_select_uses_first_row()
        {
            var editor = new FilterEditorViewModel();
            var first = new AppliedFilterStepViewModel("Shrink Spaces", new ShrinkSpacesFilter());
            var second = new AppliedFilterStepViewModel("Letters Case", new Filters.Case.LettersCaseFilter());

            editor.SyncSelection([first, second]);

            Assert.True(editor.HasSelectedStep);
            Assert.Equal("Applied Filter: Shrink Spaces", editor.TitleText);
        }

        /// <summary>
        /// Verifies string-target filters expose Apply-To and non-string filters do not.
        /// </summary>
        [Fact]
        public void SyncSelection_sets_apply_to_visibility()
        {
            var editor = new FilterEditorViewModel();
            var stringStep = new AppliedFilterStepViewModel("Shrink Spaces", new ShrinkSpacesFilter());
            var audioStep = new AppliedFilterStepViewModel("Audio Tag Remover", new Filters.Audio.TagRemoverFilter());

            editor.SyncSelection([stringStep]);
            Assert.True(editor.HasApplyTo);
            Assert.Equal(FilterApplyToOption.All[0], editor.SelectedApplyTo);

            editor.SyncSelection([audioStep]);
            Assert.False(editor.HasApplyTo);
            Assert.Null(editor.SelectedApplyTo);
        }

        /// <summary>
        /// Verifies changing Apply-To replaces the step filter and updates <see cref="AppliedFiltersViewModel.ToChain"/>.
        /// </summary>
        [Fact]
        public void Changing_apply_to_updates_step_target_and_chain_json()
        {
            var applied = new AppliedFiltersViewModel();
            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            var editor = new FilterEditorViewModel();
            editor.SyncSelection(applied.SelectedSteps);

            editor.SelectedApplyTo = FilterApplyToOption.All[1];

            Assert.Equal("Extension", applied.Steps[0].ApplyToLabel);
            var filter = (ShrinkSpacesFilter)applied.ToChain().Steps[0].Filter;
            Assert.IsType<FileExtensionTarget>(filter.Target);

            var json = JsonSerializer.Serialize<BaseFilter>(filter, PresetJsonOptions.Default);
            Assert.Contains("\"targetType\": \"FileExtension\"", json, StringComparison.Ordinal);
        }
    }
}
