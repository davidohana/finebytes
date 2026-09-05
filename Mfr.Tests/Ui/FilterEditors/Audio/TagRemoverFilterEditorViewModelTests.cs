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
            Assert.False(editor.RemoveId3v1);
            Assert.False(editor.RemoveId3v2);
            // Selective UI with no blocks yet keeps nuclear options on the step.
            Assert.True(((TagRemoverFilter)step.Filter).Options.All);

            editor.RemoveId3v1 = true;
            var options = ((TagRemoverFilter)step.Filter).Options;
            Assert.False(options.All);
            Assert.Equal([AudioTagBlockKind.Id3v1], options.Blocks);

            editor.RemoveId3v2 = true;
            options = ((TagRemoverFilter)step.Filter).Options;
            Assert.False(options.All);
            Assert.Equal([AudioTagBlockKind.Id3v1, AudioTagBlockKind.Id3v2], options.Blocks);

            editor.RemoveId3v2 = false;
            options = ((TagRemoverFilter)step.Filter).Options;
            Assert.False(options.All);
            Assert.Equal([AudioTagBlockKind.Id3v1], options.Blocks);

            editor.RemoveId3v1 = false;
            Assert.True(editor.RemoveAll);
            Assert.False(editor.AreBlockTypesEnabled);
            Assert.True(((TagRemoverFilter)step.Filter).Options.All);
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
            Assert.False(editor.RemoveId3v2);
            Assert.False(editor.RemoveXiph);

            editor.RemoveAll = false;
            Assert.False(editor.RemoveId3v2);
            Assert.True(((TagRemoverFilter)step.Filter).Options.All);
        }

        /// <summary>
        /// Verifies empty selective options normalize to nuclear on sync.
        /// </summary>
        [Fact]
        public void Tag_remover_empty_selective_options_sync_as_nuclear()
        {
            var step = new AppliedFilterStepViewModel(
                "Audio Tag Remover",
                new TagRemoverFilter(Options: new TagRemoverOptions(All: false, Blocks: []))
            );
            var editor = new TagRemoverFilterEditorViewModel(step);

            Assert.True(editor.RemoveAll);
            Assert.False(editor.AreBlockTypesEnabled);
        }
    }
}
