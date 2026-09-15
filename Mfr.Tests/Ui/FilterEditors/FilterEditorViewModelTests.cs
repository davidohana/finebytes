using Mfr.App.Ui.ViewModels.FilterChainPane;
using Mfr.App.Ui.ViewModels.FilterEditors;
using Mfr.Filters.Case;
using Mfr.Filters.Formatting;
using Mfr.Filters.Space;

namespace Mfr.Tests.Ui.FilterEditors
{
    /// <summary>
    /// Unit tests for <see cref="FilterEditorViewModel"/> host selection/title behavior.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class FilterEditorViewModelTests
    {
        public FilterEditorViewModelTests()
        {
            ConfigStoreTestReset.LoadEmpty();
        }

        /// <summary>
        /// Verifies an empty Applied selection shows the Filter Configuration empty-state title.
        /// </summary>
        [Fact]
        public void SyncSelection_with_no_selection_shows_empty_title()
        {
            var editor = new FilterEditorViewModel();

            editor.SyncSelection([]);

            Assert.False(editor.HasSelectedStep);
            Assert.Equal(FilterEditorViewModel.EmptyTitleText, editor.TitleText);
        }

        /// <summary>
        /// Verifies the first selected step sets the Filter title.
        /// </summary>
        [Fact]
        public void SyncSelection_with_one_step_sets_title()
        {
            var editor = new FilterEditorViewModel();
            var step = new FilterChainStepViewModel("Shrink Spaces", new ShrinkSpacesFilter());

            editor.SyncSelection([step]);

            Assert.True(editor.HasSelectedStep);
            Assert.Equal("Filter: Shrink Spaces", editor.TitleText);
        }

        /// <summary>
        /// Verifies multi-select uses the first selected row for the title.
        /// </summary>
        [Fact]
        public void SyncSelection_with_multi_select_uses_first_row()
        {
            var editor = new FilterEditorViewModel();
            var first = new FilterChainStepViewModel("Shrink Spaces", new ShrinkSpacesFilter());
            var second = new FilterChainStepViewModel("Letters Case", new LettersCaseFilter());

            editor.SyncSelection([first, second]);

            Assert.True(editor.HasSelectedStep);
            Assert.Equal("Filter: Shrink Spaces", editor.TitleText);
        }

        /// <summary>
        /// Verifies ApplySession restores the format-token picker collapse preference.
        /// </summary>
        [Fact]
        public void ApplySession_restores_format_token_picker_expanded()
        {
            ConfigStoreTestReset.LoadEmpty();
            ConfigStore.FilterEditor = new FilterEditorPrefs { FormatTokenPickerExpanded = false };
            var editor = new FilterEditorViewModel();

            editor.ApplySession(persistSession: true);

            Assert.False(editor.FormatTokenPickerExpanded);
        }

        /// <summary>
        /// Verifies toggling the shared chrome writes through to <see cref="ConfigStore.FilterEditor"/>.
        /// </summary>
        [Fact]
        public void FormatTokenPickerExpanded_writes_through_to_session()
        {
            ConfigStoreTestReset.LoadEmpty();
            var editor = new FilterEditorViewModel();
            editor.ApplySession(persistSession: true);

            Assert.Null(ConfigStore.FilterEditor);

            editor.FormatTokenPickerExpanded = false;

            Assert.NotNull(ConfigStore.FilterEditor);
            Assert.False(ConfigStore.FilterEditor.FormatTokenPickerExpanded);
        }

        /// <summary>
        /// Verifies a new format options editor inherits the shared collapse preference.
        /// </summary>
        [Fact]
        public void SyncSelection_copies_format_token_picker_expanded_onto_options_editor()
        {
            var editor = new FilterEditorViewModel { FormatTokenPickerExpanded = false };
            var step = new FilterChainStepViewModel("Formatter", new FormatterFilter());

            editor.SyncSelection([step]);

            Assert.NotNull(editor.OptionsEditor);
            Assert.False(editor.OptionsEditor.FormatTokenPickerExpanded);
        }

        /// <summary>
        /// Verifies options-editor collapse changes update the pane preference and session.
        /// </summary>
        [Fact]
        public void OptionsEditor_format_token_picker_expanded_updates_pane_and_session()
        {
            ConfigStoreTestReset.LoadEmpty();
            var editor = new FilterEditorViewModel();
            editor.ApplySession(persistSession: true);
            var step = new FilterChainStepViewModel("Formatter", new FormatterFilter());
            editor.SyncSelection([step]);

            editor.OptionsEditor!.FormatTokenPickerExpanded = false;

            Assert.False(editor.FormatTokenPickerExpanded);
            Assert.False(ConfigStore.FilterEditor!.FormatTokenPickerExpanded);
        }
    }
}
