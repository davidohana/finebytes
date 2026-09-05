using System.Text.Json;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Case;
using Mfr.Filters.Formatting;
using Mfr.Filters.Space;
using Mfr.Models.Tags;
using Mfr.Models.Tags.Id3v1;

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

            Assert.IsType<FilePrefixTarget>(dialog.SelectedTargetOption?.Prototype);
        }

        /// <summary>
        /// Verifies non-string filters hide Apply-To in the dialog draft.
        /// </summary>
        [Fact]
        public void Dialog_hides_apply_to_for_non_string_filters()
        {
            var step = new AppliedFilterStepViewModel("Audio Tag Remover", new Filters.Audio.TagRemoverFilter());

            var dialog = new FilterOptionsDialogViewModel(step);

            Assert.False(dialog.HasApplyTo);

            Assert.Null(dialog.SelectedTargetOption);
        }

        /// <summary>
        /// Verifies Sentence End Characters hides Apply-To (state-only filter).
        /// </summary>
        [Fact]
        public void Dialog_hides_apply_to_for_sentence_end_characters()
        {
            var step = new AppliedFilterStepViewModel("Sentence End Characters", new SentenceEndCharactersFilter());

            var dialog = new FilterOptionsDialogViewModel(step);

            Assert.False(dialog.HasApplyTo);
            Assert.Empty(step.ApplyToLabel);
        }

        /// <summary>
        /// Verifies accepting Apply-To edits updates the step and <see cref="AppliedFiltersViewModel.ToChain"/>.
        /// </summary>
        [Fact]
        public void ApplyFilterOptions_updates_step_target_and_chain_json()
        {
            var applied = new AppliedFiltersViewModel();

            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));

            var dialog = new FilterOptionsDialogViewModel(applied.Steps[0])
            {
                SelectedTargetGroup = FilterTargetCatalog.Groups[0],

                SelectedTargetOption = FilterTargetCatalog.Groups[0].Targets[1],
            };

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

            var albumOption = audioGroup.Targets.First(option =>
                option.Prototype is SemanticAudioFieldTarget semantic && semantic.Field == SemanticAudioField.Album
            );

            dialog.SelectedTargetGroup = audioGroup;

            dialog.SelectedTargetOption = albumOption;

            applied.ApplyFilterOptions(dialog);

            Assert.Equal("Album", applied.Steps[0].ApplyToLabel);

            var filter = (ShrinkSpacesFilter)applied.ToChain().Steps[0].Filter;

            var target = Assert.IsType<SemanticAudioFieldTarget>(filter.Target);

            Assert.Equal(SemanticAudioField.Album, target.Field);
        }

        /// <summary>
        /// Verifies ID3v1 targets round-trip through Filter Options.
        /// </summary>
        [Fact]
        public void ApplyFilterOptions_updates_id3v1_target()
        {
            var applied = new AppliedFiltersViewModel();

            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));

            var dialog = new FilterOptionsDialogViewModel(applied.Steps[0]);

            var id3v1Group = FilterTargetCatalog.Groups.First(group => group.Label == "ID3v1");

            var artistOption = id3v1Group.Targets.First(option =>
                option.Prototype is Id3v1FieldTarget id3v1 && id3v1.Field == Id3v1Field.Artist
            );

            dialog.SelectedTargetGroup = id3v1Group;

            dialog.SelectedTargetOption = artistOption;

            applied.ApplyFilterOptions(dialog);

            Assert.Equal("Artist", applied.Steps[0].ApplyToLabel);

            var filter = (ShrinkSpacesFilter)applied.ToChain().Steps[0].Filter;

            var target = Assert.IsType<Id3v1FieldTarget>(filter.Target);

            Assert.Equal(Id3v1Field.Artist, target.Field);

            var json = JsonSerializer.Serialize<BaseFilter>(filter, PresetJsonOptions.Default);

            Assert.Contains("\"targetType\": \"Id3v1Field\"", json, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies Xiph targets round-trip through Filter Options.
        /// </summary>
        [Fact]
        public void ApplyFilterOptions_updates_xiph_target()
        {
            var applied = new AppliedFiltersViewModel();

            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));

            var dialog = new FilterOptionsDialogViewModel(applied.Steps[0]);

            var xiphGroup = FilterTargetCatalog.Groups.First(group => group.Label == "Xiph");

            var titleOption = xiphGroup.Targets.First(option =>
                option.Prototype is XiphFieldTarget xiph && xiph.Key == "TITLE"
            );

            dialog.SelectedTargetGroup = xiphGroup;

            dialog.SelectedTargetOption = titleOption;

            applied.ApplyFilterOptions(dialog);

            Assert.Equal("Title", applied.Steps[0].ApplyToLabel);

            var filter = (ShrinkSpacesFilter)applied.ToChain().Steps[0].Filter;

            var target = Assert.IsType<XiphFieldTarget>(filter.Target);

            Assert.Equal("TITLE", target.Key);
        }

        /// <summary>
        /// Verifies ID3v2 singleton frames round-trip through Filter Options.
        /// </summary>
        [Fact]
        public void ApplyFilterOptions_updates_id3v2_singleton_target()
        {
            var applied = new AppliedFiltersViewModel();

            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));

            var dialog = new FilterOptionsDialogViewModel(applied.Steps[0]);

            var id3v2Group = FilterTargetCatalog.Groups.First(group => group.Label == "ID3v2");

            var titleOption = id3v2Group.Targets.First(option =>
                option.Prototype is Id3v2FrameTarget frame && frame.FrameId == "TIT2"
            );

            dialog.SelectedTargetGroup = id3v2Group;

            dialog.SelectedTargetOption = titleOption;

            applied.ApplyFilterOptions(dialog);

            Assert.Equal("TIT2 (Title)", applied.Steps[0].ApplyToLabel);

            var filter = (ShrinkSpacesFilter)applied.ToChain().Steps[0].Filter;

            var target = Assert.IsType<Id3v2FrameTarget>(filter.Target);

            Assert.Equal("TIT2", target.FrameId);

            Assert.Null(target.Language);

            Assert.Null(target.Description);
        }

        /// <summary>
        /// Verifies ID3v2 multi-instance frames round-trip language and description.
        /// </summary>
        [Fact]
        public void ApplyFilterOptions_updates_id3v2_multi_instance_target()
        {
            var applied = new AppliedFiltersViewModel();

            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));

            var dialog = new FilterOptionsDialogViewModel(applied.Steps[0]);

            var id3v2Group = FilterTargetCatalog.Groups.First(group => group.Label == "ID3v2");

            var commentOption = id3v2Group.Targets.First(option =>
                option.Prototype is Id3v2FrameTarget frame && frame.FrameId == "COMM"
            );

            dialog.SelectedTargetGroup = id3v2Group;

            dialog.SelectedTargetOption = commentOption;

            dialog.Id3v2Language = "eng";

            dialog.Id3v2Description = "Short";

            applied.ApplyFilterOptions(dialog);

            Assert.Equal("COMM (Short)", applied.Steps[0].ApplyToLabel);

            var filter = (ShrinkSpacesFilter)applied.ToChain().Steps[0].Filter;

            var target = Assert.IsType<Id3v2FrameTarget>(filter.Target);

            Assert.Equal("COMM", target.FrameId);

            Assert.Equal("eng", target.Language);

            Assert.Equal("Short", target.Description);
        }

        /// <summary>
        /// Verifies leftover COMM language/description are not written onto a singleton frame.
        /// </summary>
        [Fact]
        public void ApplyFilterOptions_id3v2_singleton_ignores_stale_multi_instance_fields()
        {
            var applied = new AppliedFiltersViewModel();
            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            var dialog = new FilterOptionsDialogViewModel(applied.Steps[0]);
            var id3v2Group = FilterTargetCatalog.Groups.First(group => group.Label == "ID3v2");
            var commentOption = id3v2Group.Targets.First(option =>
                option.Prototype is Id3v2FrameTarget frame && frame.FrameId == "COMM"
            );
            dialog.SelectedTargetGroup = id3v2Group;
            dialog.SelectedTargetOption = commentOption;
            dialog.Id3v2Language = "eng";
            dialog.Id3v2Description = "Short";

            var titleOption = id3v2Group.Targets.First(option =>
                option.Prototype is Id3v2FrameTarget frame && frame.FrameId == "TIT2"
            );
            dialog.SelectedTargetOption = titleOption;

            applied.ApplyFilterOptions(dialog);

            var filter = (ShrinkSpacesFilter)applied.ToChain().Steps[0].Filter;
            var target = Assert.IsType<Id3v2FrameTarget>(filter.Target);
            Assert.Equal("TIT2", target.FrameId);
            Assert.Null(target.Language);
            Assert.Null(target.Description);
        }

        /// <summary>
        /// Verifies loading an ID3v1 target resolves the matching catalog group and option.
        /// </summary>
        [Fact]
        public void Dialog_loads_id3v1_target_from_filter()
        {
            var filter = new FormatterFilter(new Id3v1FieldTarget(Id3v1Field.Album), new FormatterOptions("x"));

            var step = new AppliedFilterStepViewModel("Formatter", filter);

            var dialog = new FilterOptionsDialogViewModel(step);

            Assert.Equal("ID3v1", dialog.SelectedTargetGroup?.Label);

            Assert.IsType<Id3v1FieldTarget>(dialog.SelectedTargetOption?.Prototype);

            Assert.Equal(Id3v1Field.Album, ((Id3v1FieldTarget)dialog.SelectedTargetOption.Prototype).Field);
        }

        /// <summary>
        /// Verifies loading a Xiph target resolves the matching catalog group and option.
        /// </summary>
        [Fact]
        public void Dialog_loads_xiph_target_from_filter()
        {
            var filter = new FormatterFilter(new XiphFieldTarget("TITLE"), new FormatterOptions("x"));

            var step = new AppliedFilterStepViewModel("Formatter", filter);

            var dialog = new FilterOptionsDialogViewModel(step);

            Assert.Equal("Xiph", dialog.SelectedTargetGroup?.Label);

            var prototype = Assert.IsType<XiphFieldTarget>(dialog.SelectedTargetOption?.Prototype);

            Assert.Equal("TITLE", prototype.Key);
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

                SubstringStartAnchorOption = StringScopeAnchorOption.FromAnchor(StringScopeAnchor.Right),

                SubstringEndPosition = 4,

                SubstringEndAnchorOption = StringScopeAnchorOption.FromAnchor(StringScopeAnchor.Left),
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

        /// <summary>
        /// Verifies token apply scope round-trips through Filter Options.
        /// </summary>
        [Fact]
        public void ApplyFilterOptions_updates_token_apply_scope()
        {
            var applied = new AppliedFiltersViewModel();

            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));

            var dialog = new FilterOptionsDialogViewModel(applied.Steps[0])
            {
                ScopeMode = FilterApplyScopeMode.Token,

                TokenSeparator = "-",

                TokenNumber = 2,
            };

            applied.ApplyFilterOptions(dialog);

            Assert.Equal("File Prefix (Token)", applied.Steps[0].ApplyToLabel);

            var filter = (ShrinkSpacesFilter)applied.ToChain().Steps[0].Filter;

            var scope = Assert.IsType<TokenApplyScope>(filter.ApplyScope);

            Assert.Equal("-", scope.Separator);

            Assert.Equal(2, scope.TokenNumber);
        }

        /// <summary>
        /// Verifies OK is blocked while token scope has an empty separator.
        /// </summary>
        [Fact]
        public void CanConfirm_is_false_when_token_separator_empty()
        {
            var applied = new AppliedFiltersViewModel();
            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));

            var dialog = new FilterOptionsDialogViewModel(applied.Steps[0])
            {
                ScopeMode = FilterApplyScopeMode.Token,
                TokenSeparator = string.Empty,
            };

            Assert.False(dialog.CanConfirm);
            Assert.Equal("Token Separator required", dialog.ConfirmDisabledReason);

            dialog.TokenSeparator = "-";
            Assert.True(dialog.CanConfirm);
            Assert.Null(dialog.ConfirmDisabledReason);

            dialog.ScopeMode = FilterApplyScopeMode.Whole;
            dialog.TokenSeparator = string.Empty;
            Assert.True(dialog.CanConfirm);
            Assert.Null(dialog.ConfirmDisabledReason);
        }

        /// <summary>
        /// Verifies ancestor-folder level 1 uses the Parent Folder list subtitle.
        /// </summary>
        [Fact]
        public void ApplyFilterOptions_ancestor_level_1_uses_parent_folder_label()
        {
            var applied = new AppliedFiltersViewModel();

            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));

            var dialog = new FilterOptionsDialogViewModel(applied.Steps[0]);

            var pathGroup = FilterTargetCatalog.Groups.First(group => group.Label == "Path");

            var ancestorOption = pathGroup.Targets.First(option => option.Prototype is AncestorFolderTarget);

            dialog.SelectedTargetGroup = pathGroup;

            dialog.SelectedTargetOption = ancestorOption;

            dialog.AncestorFolderLevel = 1;

            applied.ApplyFilterOptions(dialog);

            Assert.Equal("Parent Folder", applied.Steps[0].ApplyToLabel);

            var filter = (ShrinkSpacesFilter)applied.ToChain().Steps[0].Filter;

            var target = Assert.IsType<AncestorFolderTarget>(filter.Target);

            Assert.Equal(1, target.Level);
        }

        /// <summary>
        /// Verifies ancestor-folder level is written onto the step target.
        /// </summary>
        [Fact]
        public void ApplyFilterOptions_updates_ancestor_folder_level()
        {
            var applied = new AppliedFiltersViewModel();

            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));

            var dialog = new FilterOptionsDialogViewModel(applied.Steps[0]);

            var pathGroup = FilterTargetCatalog.Groups.First(group => group.Label == "Path");

            var ancestorOption = pathGroup.Targets.First(option => option.Prototype is AncestorFolderTarget);

            dialog.SelectedTargetGroup = pathGroup;

            dialog.SelectedTargetOption = ancestorOption;

            dialog.AncestorFolderLevel = 3;

            applied.ApplyFilterOptions(dialog);

            Assert.Equal("Ancestor Folder (3)", applied.Steps[0].ApplyToLabel);

            var filter = (ShrinkSpacesFilter)applied.ToChain().Steps[0].Filter;

            var target = Assert.IsType<AncestorFolderTarget>(filter.Target);

            Assert.Equal(3, target.Level);
        }
    }
}
