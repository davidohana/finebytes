using System.Text.Json;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Space;
using Mfr.Models.Filters;
using Mfr.Models.Tags;

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
            Assert.Equal(FilterTargetKind.FilePrefix, dialog.SelectedTargetOption?.Kind);
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
            Assert.Null(dialog.SelectedTargetOption);
        }

        /// <summary>
        /// Verifies accepting Apply-To edits updates the step and <see cref="AppliedFiltersViewModel.ToChain"/>.
        /// </summary>
        [Fact]
        public void ApplyFilterOptions_updates_step_target_and_chain_json()
        {
            var applied = new AppliedFiltersViewModel();
            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            var dialog = new FilterOptionsDialogViewModel(applied.Steps[0]);
            dialog.SelectedTargetGroup = FilterTargetCatalog.Groups[0];
            dialog.SelectedTargetOption = FilterTargetCatalog.Groups[0].Targets[1];

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

        /// <summary>
        /// Verifies semantic audio targets round-trip through Filter Options.
        /// </summary>
        [Fact]
        public void ApplyFilterOptions_updates_semantic_audio_target()
        {
            var applied = new AppliedFiltersViewModel();
            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            var dialog = new FilterOptionsDialogViewModel(applied.Steps[0]);
            var audioGroup = FilterTargetCatalog.Groups.First(group => group.Label == "Audio Tag");
            var albumOption = audioGroup.Targets.First(option => option.AudioField == SemanticAudioField.Album);
            dialog.SelectedTargetGroup = audioGroup;
            dialog.SelectedTargetOption = albumOption;

            applied.ApplyFilterOptions(dialog);

            Assert.Equal("Album", applied.Steps[0].ApplyToLabel);
            var filter = (ShrinkSpacesFilter)applied.ToChain().Steps[0].Filter;
            var target = Assert.IsType<SemanticAudioFieldTarget>(filter.Target);
            Assert.Equal(SemanticAudioField.Album, target.Field);
        }

        /// <summary>
        /// Verifies substring apply scope round-trips through Filter Options.
        /// </summary>
        [Fact]
        public void ApplyFilterOptions_updates_substring_apply_scope()
        {
            var applied = new AppliedFiltersViewModel();
            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            var dialog = new FilterOptionsDialogViewModel(applied.Steps[0])
            {
                ScopeMode = FilterApplyScopeMode.Substring,
                SubstringStartPosition = 2,
                SubstringStartAnchor = StringScopeAnchor.Right,
                SubstringEndPosition = 4,
                SubstringEndAnchor = StringScopeAnchor.Left,
            };

            applied.ApplyFilterOptions(dialog);

            Assert.Equal("File Prefix (Substring)", applied.Steps[0].ApplyToLabel);
            var filter = (ShrinkSpacesFilter)applied.ToChain().Steps[0].Filter;
            var scope = Assert.IsType<SubstringApplyScope>(filter.ApplyScope);
            Assert.Equal(2, scope.StartPosition);
            Assert.Equal(StringScopeAnchor.Right, scope.StartAnchor);
            Assert.Equal(4, scope.EndPosition);
            Assert.Equal(StringScopeAnchor.Left, scope.EndAnchor);
        }
    }
}
