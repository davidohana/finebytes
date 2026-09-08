using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors;
using Mfr.Filters.Case;
using Mfr.Filters.Formatting;
using Mfr.Filters.Space;

namespace Mfr.Tests.Ui.FilterEditors
{
    /// <summary>
    /// Unit tests for <see cref="FilterEditorViewModel"/> host selection/title behavior.
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
            var second = new AppliedFilterStepViewModel("Letters Case", new LettersCaseFilter());

            editor.SyncSelection([first, second]);

            Assert.True(editor.HasSelectedStep);
            Assert.Equal("Applied Filter: Shrink Spaces", editor.TitleText);
        }

        /// <summary>
        /// Verifies ApplySession restores the format-token picker collapse preference.
        /// </summary>
        [Fact]
        public void ApplySession_restores_format_token_picker_expanded()
        {
            var session = new SessionState
            {
                FilterEditor = new SessionStateFilterEditor { FormatTokenPickerExpanded = false },
            };
            var editor = new FilterEditorViewModel();

            editor.ApplySession(session);

            Assert.False(editor.FormatTokenPickerExpanded);
        }

        /// <summary>
        /// Verifies toggling the shared chrome writes through to the attached session document.
        /// </summary>
        [Fact]
        public void FormatTokenPickerExpanded_writes_through_to_session()
        {
            var session = new SessionState();
            var editor = new FilterEditorViewModel();
            editor.ApplySession(session);

            Assert.Null(session.FilterEditor);

            editor.FormatTokenPickerExpanded = false;

            Assert.NotNull(session.FilterEditor);
            Assert.False(session.FilterEditor.FormatTokenPickerExpanded);
        }

        /// <summary>
        /// Verifies a new format options editor inherits the shared collapse preference.
        /// </summary>
        [Fact]
        public void SyncSelection_copies_format_token_picker_expanded_onto_options_editor()
        {
            var editor = new FilterEditorViewModel { FormatTokenPickerExpanded = false };
            var step = new AppliedFilterStepViewModel("Formatter", new FormatterFilter());

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
            var session = new SessionState();
            var editor = new FilterEditorViewModel();
            editor.ApplySession(session);
            var step = new AppliedFilterStepViewModel("Formatter", new FormatterFilter());
            editor.SyncSelection([step]);

            editor.OptionsEditor!.FormatTokenPickerExpanded = false;

            Assert.False(editor.FormatTokenPickerExpanded);
            Assert.False(session.FilterEditor!.FormatTokenPickerExpanded);
        }
    }
}
