using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors.Trimming;
using Mfr.Filters;
using Mfr.Filters.Trimming;
using Mfr.Utils;

namespace Mfr.Tests.Ui.FilterEditors.Trimming
{
    /// <summary>
    /// Unit tests for Visual Trim Helper wiring on count editors.
    /// </summary>
    public sealed class CountFilterEditorViewModelTests
    {
        /// <summary>
        /// Verifies left-edge selection updates Count on Trim Left.
        /// </summary>
        [Fact]
        public void Trim_helper_selection_updates_count()
        {
            var step = new AppliedFilterStepViewModel("Trim Left", new TrimLeftFilter());
            var editor = new CountFilterEditorViewModel(step);
            editor.TrimHelper.SetSampleText("abcdef");

            Assert.True(editor.TrimHelper.TryApplyPointerSelection(selectionStart: 0, selectionLength: 3));
            Assert.Equal(3, editor.Count);
            Assert.Equal(3, ((TrimLeftFilter)step.Filter).Options.Count);
        }

        /// <summary>
        /// Verifies spinner changes request a right-edge highlight.
        /// </summary>
        [Fact]
        public void Spinner_updates_right_edge_highlight()
        {
            var step = new AppliedFilterStepViewModel("Trim Right", new TrimRightFilter());
            var editor = new CountFilterEditorViewModel(step);
            editor.TrimHelper.SetSampleText("abcdef");
            editor.Count = 2;

            Assert.Equal(4, editor.TrimHelper.HighlightStart);
            Assert.Equal(2, editor.TrimHelper.HighlightLength);
        }

        /// <summary>
        /// Verifies init fills sample from the first Rename List item's Apply Target.
        /// </summary>
        [Fact]
        public void Init_uses_first_rename_item_prefix()
        {
            var item = new RenameItem(
                new FileMeta(
                    renameListIndex: 0,
                    inFolderIndex: 0,
                    directoryPath: TestPaths.Absolute("album"),
                    prefix: "sample-name",
                    extension: "txt"
                )
            );
            var step = new AppliedFilterStepViewModel("Extract Left", new ExtractLeftFilter());
            var editor = new CountFilterEditorViewModel(step, sampleRenameItems: [item]);

            Assert.Equal("sample-name", editor.TrimHelper.SampleText);
            Assert.Equal("sample-name", editor.TrimHelper.DisplayText);
            // Default ExtractLeft count is 1 → highlight first character.
            Assert.Equal(0, editor.TrimHelper.HighlightStart);
            Assert.Equal(1, editor.TrimHelper.HighlightLength);
        }

        /// <summary>
        /// Verifies dropping a sample into an empty helper applies the current count highlight.
        /// </summary>
        [Fact]
        public void First_sample_drop_syncs_highlight_from_count()
        {
            var filter = new TrimLeftFilter(new FilePrefixTarget(), new CountFilterOptions(Count: 3));
            var step = new AppliedFilterStepViewModel("Trim Left", filter);
            var editor = new CountFilterEditorViewModel(step);
            Assert.False(editor.TrimHelper.HasSample);

            editor.TrimHelper.SetSampleText("abcdef");

            Assert.Equal(0, editor.TrimHelper.HighlightStart);
            Assert.Equal(3, editor.TrimHelper.HighlightLength);
        }

        /// <summary>
        /// Verifies Rename List path resolve uses the filter Apply Target string.
        /// </summary>
        [Fact]
        public void TryResolveRenameListDrop_reads_apply_target()
        {
            var item = new RenameItem(
                new FileMeta(
                    renameListIndex: 0,
                    inFolderIndex: 0,
                    directoryPath: TestPaths.Absolute("album"),
                    prefix: "track",
                    extension: "mp3"
                )
            );
            var step = new AppliedFilterStepViewModel("Trim Left", new TrimLeftFilter());
            var editor = new CountFilterEditorViewModel(
                step,
                resolveRenameItemByFullPath: path => PathComparers.Os.Equals(path, item.Original.FullPath) ? item : null
            );

            Assert.True(editor.TrimHelper.TryResolveRenameListDrop(item.Original.FullPath, out var resolved));
            Assert.Equal("track", resolved);
        }
    }
}
