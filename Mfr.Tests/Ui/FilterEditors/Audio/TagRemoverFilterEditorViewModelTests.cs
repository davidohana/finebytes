using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors.Audio;
using Mfr.Filters.Audio;
using Mfr.Models.Tags;

namespace Mfr.Tests.Ui.FilterEditors.Audio
{
    /// <summary>
    /// Unit tests for <see cref="TagRemoverFilterEditorViewModel"/>.
    /// </summary>
    public sealed class TagRemoverFilterEditorViewModelTests
    {
        /// <summary>
        /// Verifies Audio Tag Remover option edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Tag_remover_options_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Audio Tag Remover", new TagRemoverFilter());
            var editor = new TagRemoverFilterEditorViewModel(step);

            Assert.True(editor.RemoveAll);
            Assert.False(editor.AreBlockTypesEnabled);
            Assert.True(((TagRemoverFilter)step.Filter).Options.All);

            editor.RemoveAll = false;
            Assert.False(editor.RemoveAll);
            Assert.True(editor.AreBlockTypesEnabled);
            Assert.False(_Row(editor, AudioTagBlockKind.Id3v1).IsSelected);
            Assert.False(_Row(editor, AudioTagBlockKind.Id3v2).IsSelected);
            // Unchecked Remove-all with no blocks → selective no-op (not hidden nuclear).
            var options = ((TagRemoverFilter)step.Filter).Options;
            Assert.False(options.All);
            Assert.Empty(options.Blocks ?? []);

            _Row(editor, AudioTagBlockKind.Id3v1).IsSelected = true;
            options = ((TagRemoverFilter)step.Filter).Options;
            Assert.False(options.All);
            Assert.Equal([AudioTagBlockKind.Id3v1], options.Blocks);

            _Row(editor, AudioTagBlockKind.Id3v2).IsSelected = true;
            options = ((TagRemoverFilter)step.Filter).Options;
            Assert.False(options.All);
            Assert.Equal([AudioTagBlockKind.Id3v1, AudioTagBlockKind.Id3v2], options.Blocks);

            _Row(editor, AudioTagBlockKind.Id3v2).IsSelected = false;
            options = ((TagRemoverFilter)step.Filter).Options;
            Assert.False(options.All);
            Assert.Equal([AudioTagBlockKind.Id3v1], options.Blocks);

            _Row(editor, AudioTagBlockKind.Id3v1).IsSelected = false;
            Assert.False(editor.RemoveAll);
            Assert.True(editor.AreBlockTypesEnabled);
            options = ((TagRemoverFilter)step.Filter).Options;
            Assert.False(options.All);
            Assert.Empty(options.Blocks ?? []);
        }

        /// <summary>
        /// Verifies nuclear options ignore leftover Blocks when hydrating the editor.
        /// </summary>
        [Fact]
        public void Tag_remover_nuclear_sync_clears_leftover_block_checkboxes()
        {
            var step = new AppliedFilterStepViewModel(
                "Audio Tag Remover",
                new TagRemoverFilter(
                    Options: new TagRemoverOptions(All: true, Blocks: [AudioTagBlockKind.Id3v2, AudioTagBlockKind.Xiph])
                )
            );
            var editor = new TagRemoverFilterEditorViewModel(step);

            Assert.True(editor.RemoveAll);
            Assert.False(_Row(editor, AudioTagBlockKind.Id3v2).IsSelected);
            Assert.False(_Row(editor, AudioTagBlockKind.Xiph).IsSelected);

            editor.RemoveAll = false;
            Assert.False(_Row(editor, AudioTagBlockKind.Id3v2).IsSelected);
            var options = ((TagRemoverFilter)step.Filter).Options;
            Assert.False(options.All);
            Assert.Empty(options.Blocks ?? []);
        }

        /// <summary>
        /// Verifies empty selective options stay selective (no-op) on sync.
        /// </summary>
        [Fact]
        public void Tag_remover_empty_selective_options_sync_as_noop()
        {
            var step = new AppliedFilterStepViewModel(
                "Audio Tag Remover",
                new TagRemoverFilter(Options: new TagRemoverOptions(All: false, Blocks: []))
            );
            var editor = new TagRemoverFilterEditorViewModel(step);

            Assert.False(editor.RemoveAll);
            Assert.True(editor.AreBlockTypesEnabled);
            Assert.All(editor.BlockRows, row => Assert.False(row.IsSelected));
        }

        /// <summary>
        /// Verifies the block-row catalog covers every <see cref="AudioTagBlockKind"/> once.
        /// </summary>
        [Fact]
        public void Tag_remover_block_rows_match_enum_catalog()
        {
            var step = new AppliedFilterStepViewModel("Audio Tag Remover", new TagRemoverFilter());
            var editor = new TagRemoverFilterEditorViewModel(step);

            Assert.Equal(Enum.GetValues<AudioTagBlockKind>(), editor.BlockRows.Select(row => row.Kind));
            Assert.Equal(AudioTagBlockKindChoice.All.Count, editor.BlockRows.Count);
        }

        private static TagRemoverBlockRowViewModel _Row(TagRemoverFilterEditorViewModel editor, AudioTagBlockKind kind)
        {
            return editor.BlockRows.Single(row => row.Kind == kind);
        }
    }
}
