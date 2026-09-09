using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors.Trimming;
using Mfr.Filters.Trimming;

namespace Mfr.Tests.Ui.FilterEditors.Trimming
{
    /// <summary>
    /// Unit tests for <see cref="TrimBetweenFilterEditorViewModel"/>.
    /// </summary>
    public sealed class TrimBetweenFilterEditorViewModelTests
    {
        /// <summary>
        /// Verifies Trim Between position/anchor edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Trim_between_positions_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Trim Between", new TrimBetweenFilter());
            var editor = new TrimBetweenFilterEditorViewModel(step);

            Assert.Equal(2, editor.StartValue);
            Assert.Equal(Side.Left, editor.StartAnchor);
            Assert.Equal(4, editor.EndValue);
            Assert.Equal(Side.Left, editor.EndAnchor);

            editor.StartValue = 13;
            editor.EndValue = 5;
            editor.EndAnchor = Side.Right;

            var options = ((TrimBetweenFilter)step.Filter).Options;
            Assert.Equal(new Position(13, Side.Left), options.Start);
            Assert.Equal(new Position(5, Side.Right), options.End);
        }

        /// <summary>
        /// Verifies helper selection writes left-anchored start/end positions.
        /// </summary>
        [Fact]
        public void Trim_helper_selection_updates_left_anchored_range()
        {
            var step = new AppliedFilterStepViewModel("Trim Between", new TrimBetweenFilter());
            var editor = new TrimBetweenFilterEditorViewModel(step);
            editor.TrimHelper.SetSampleText("abcdef");

            Assert.True(editor.TrimHelper.TryApplyPointerSelection(selectionStart: 1, selectionLength: 3));
            Assert.Equal(2, editor.StartValue);
            Assert.Equal(Side.Left, editor.StartAnchor);
            Assert.Equal(4, editor.EndValue);
            Assert.Equal(Side.Left, editor.EndAnchor);

            var options = ((TrimBetweenFilter)step.Filter).Options;
            Assert.Equal(new Position(2, Side.Left), options.Start);
            Assert.Equal(new Position(4, Side.Left), options.End);
        }

        /// <summary>
        /// Verifies selection on the placeholder text still updates start/end (MFR7 parity).
        /// </summary>
        [Fact]
        public void Placeholder_selection_updates_left_anchored_range()
        {
            var step = new AppliedFilterStepViewModel("Trim Between", new TrimBetweenFilter());
            var editor = new TrimBetweenFilterEditorViewModel(step);
            Assert.False(editor.TrimHelper.HasSample);
            Assert.Equal(VisualTrimHelperViewModel.PlaceholderText, editor.TrimHelper.DisplayText);

            Assert.True(editor.TrimHelper.TryApplyPointerSelection(selectionStart: 2, selectionLength: 4));
            Assert.Equal(3, editor.StartValue);
            Assert.Equal(Side.Left, editor.StartAnchor);
            Assert.Equal(6, editor.EndValue);
            Assert.Equal(Side.Left, editor.EndAnchor);
        }

        /// <summary>
        /// Verifies spinner/anchor changes update the helper highlight.
        /// </summary>
        [Fact]
        public void Spinner_updates_helper_highlight()
        {
            var step = new AppliedFilterStepViewModel("Trim Between", new TrimBetweenFilter());
            var editor = new TrimBetweenFilterEditorViewModel(step);
            editor.TrimHelper.SetSampleText("abcdef");
            // Defaults are already 2–4; change to force highlight sync after sample is set.
            editor.EndValue = 5;

            Assert.Equal(1, editor.TrimHelper.HighlightStart);
            Assert.Equal(4, editor.TrimHelper.HighlightLength);
        }

        /// <summary>
        /// Verifies first add with a Rename List sample computes the default 2–4 highlight.
        /// </summary>
        [Fact]
        public void Init_with_sample_sets_default_highlight()
        {
            var item = new RenameItem(
                new FileMeta(
                    renameListIndex: 0,
                    inFolderIndex: 0,
                    directoryPath: TestPaths.Absolute("album"),
                    prefix: "abcdef",
                    extension: "txt"
                )
            );
            var step = new AppliedFilterStepViewModel("Trim Between", new TrimBetweenFilter());
            var editor = new TrimBetweenFilterEditorViewModel(step, sampleRenameItems: [item]);

            Assert.Equal("abcdef", editor.TrimHelper.SampleText);
            Assert.Equal(1, editor.TrimHelper.HighlightStart);
            Assert.Equal(3, editor.TrimHelper.HighlightLength);
        }

        /// <summary>
        /// Verifies dropping a sample into an empty helper applies the current options highlight.
        /// </summary>
        [Fact]
        public void First_sample_drop_syncs_highlight_from_options()
        {
            var step = new AppliedFilterStepViewModel("Trim Between", new TrimBetweenFilter());
            var editor = new TrimBetweenFilterEditorViewModel(step);
            Assert.False(editor.TrimHelper.HasSample);
            // Default 2–4 highlight already applies to the placeholder.
            Assert.Equal(1, editor.TrimHelper.HighlightStart);
            Assert.Equal(3, editor.TrimHelper.HighlightLength);

            editor.TrimHelper.SetSampleText("abcdef");

            Assert.Equal(1, editor.TrimHelper.HighlightStart);
            Assert.Equal(3, editor.TrimHelper.HighlightLength);
        }
    }
}
