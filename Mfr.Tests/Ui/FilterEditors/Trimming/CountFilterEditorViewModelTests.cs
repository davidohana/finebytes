using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors.Trimming;
using Mfr.Filters;
using Mfr.Filters.Trimming;
using Mfr.Tests.Models.Filters;
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
        /// Verifies selection on the placeholder text still updates Count (MFR7 parity).
        /// </summary>
        [Fact]
        public void Placeholder_selection_updates_count()
        {
            var step = new AppliedFilterStepViewModel("Trim Left", new TrimLeftFilter());
            var editor = new CountFilterEditorViewModel(step);
            Assert.False(editor.TrimHelper.HasSample);
            Assert.Equal(VisualTrimHelperViewModel.PlaceholderText, editor.TrimHelper.DisplayText);

            Assert.True(editor.TrimHelper.TryApplyPointerSelection(selectionStart: 0, selectionLength: 5));
            Assert.Equal(5, editor.Count);
            Assert.Equal(0, editor.TrimHelper.HighlightStart);
            Assert.Equal(5, editor.TrimHelper.HighlightLength);
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
            // Default count highlight already applies to the placeholder.
            Assert.Equal(0, editor.TrimHelper.HighlightStart);
            Assert.Equal(3, editor.TrimHelper.HighlightLength);

            editor.TrimHelper.SetSampleText("abcdef");

            Assert.Equal(0, editor.TrimHelper.HighlightStart);
            Assert.Equal(3, editor.TrimHelper.HighlightLength);
        }

        /// <summary>
        /// Verifies ▲/▼ cycle Rename List samples and update the index label.
        /// </summary>
        [Fact]
        public void Navigate_cycles_rename_list_samples()
        {
            var items = new[]
            {
                FilterTestHelpers.CreateRenameItem(prefix: "alpha", extension: "txt"),
                FilterTestHelpers.CreateRenameItem(prefix: "beta", extension: "txt", renameListIndex: 1),
            };
            var step = new AppliedFilterStepViewModel("Trim Left", new TrimLeftFilter());
            var editor = new CountFilterEditorViewModel(step, sampleRenameItems: items);

            Assert.Equal("alpha", editor.TrimHelper.SampleText);
            Assert.Equal("1", editor.TrimHelper.ItemIndexLabel);
            Assert.False(editor.TrimHelper.CanGoPrevious);
            Assert.True(editor.TrimHelper.CanGoNext);

            Assert.True(editor.TrimHelper.GoNextCommand.CanExecute(null));
            editor.TrimHelper.GoNext();
            Assert.Equal("beta", editor.TrimHelper.SampleText);
            Assert.Equal("2", editor.TrimHelper.ItemIndexLabel);
            Assert.True(editor.TrimHelper.CanGoPrevious);
            Assert.False(editor.TrimHelper.CanGoNext);

            editor.TrimHelper.GoPrevious();
            Assert.Equal("alpha", editor.TrimHelper.SampleText);
            Assert.Equal("1", editor.TrimHelper.ItemIndexLabel);
        }

        /// <summary>
        /// Verifies a Rename List drop jumps the navigator to that item.
        /// </summary>
        [Fact]
        public void Drop_syncs_navigator_index()
        {
            var items = new[]
            {
                FilterTestHelpers.CreateRenameItem(prefix: "alpha", extension: "txt"),
                FilterTestHelpers.CreateRenameItem(prefix: "beta", extension: "txt", renameListIndex: 1),
            };
            var step = new AppliedFilterStepViewModel("Trim Left", new TrimLeftFilter());
            var editor = new CountFilterEditorViewModel(
                step,
                sampleRenameItems: items,
                resolveRenameItemByFullPath: path =>
                    items.FirstOrDefault(i => PathComparers.Os.Equals(path, i.Original.FullPath))
            );

            Assert.True(editor.TrimHelper.TryApplyRenameListDrop(items[1].Original.FullPath));
            Assert.Equal("beta", editor.TrimHelper.SampleText);
            Assert.Equal("2", editor.TrimHelper.ItemIndexLabel);
            Assert.True(editor.TrimHelper.CanGoPrevious);
            Assert.False(editor.TrimHelper.CanGoNext);
        }

        /// <summary>
        /// Verifies RefreshRenameItems picks up items added after the editor was created.
        /// </summary>
        [Fact]
        public void Refresh_enables_navigation_after_late_list_add()
        {
            var items = new List<RenameItem>();
            var step = new AppliedFilterStepViewModel("Trim Left", new TrimLeftFilter());
            var editor = new CountFilterEditorViewModel(step, resolveSampleRenameItems: () => items);
            Assert.False(editor.TrimHelper.HasSample);
            Assert.False(editor.TrimHelper.CanGoNext);
            Assert.Equal(string.Empty, editor.TrimHelper.ItemIndexLabel);

            items.Add(FilterTestHelpers.CreateRenameItem(prefix: "alpha", extension: "txt"));
            items.Add(FilterTestHelpers.CreateRenameItem(prefix: "beta", extension: "txt", renameListIndex: 1));
            editor.TrimHelper.RefreshRenameItems();

            Assert.Equal("alpha", editor.TrimHelper.SampleText);
            Assert.Equal("1", editor.TrimHelper.ItemIndexLabel);
            Assert.True(editor.TrimHelper.CanGoNext);
        }

        /// <summary>
        /// Verifies an unmatched custom sample clears the index without inventing a list position.
        /// </summary>
        [Fact]
        public void Refresh_unmatched_sample_clears_index_and_next_selects_first()
        {
            var items = new List<RenameItem>
            {
                FilterTestHelpers.CreateRenameItem(prefix: "alpha", extension: "txt"),
                FilterTestHelpers.CreateRenameItem(prefix: "beta", extension: "txt", renameListIndex: 1),
            };
            var step = new AppliedFilterStepViewModel("Trim Left", new TrimLeftFilter());
            var editor = new CountFilterEditorViewModel(step, resolveSampleRenameItems: () => items);
            editor.TrimHelper.SetSampleText("custom-sample");

            editor.TrimHelper.RefreshRenameItems();

            Assert.Equal("custom-sample", editor.TrimHelper.SampleText);
            Assert.Equal(string.Empty, editor.TrimHelper.ItemIndexLabel);
            Assert.False(editor.TrimHelper.CanGoPrevious);
            Assert.True(editor.TrimHelper.CanGoNext);

            editor.TrimHelper.GoNext();
            Assert.Equal("alpha", editor.TrimHelper.SampleText);
            Assert.Equal("1", editor.TrimHelper.ItemIndexLabel);
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
