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

            Assert.True(editor.RemoveId3v1);

            Assert.True(editor.RemoveId3v2);

            Assert.True(editor.RemoveXiph);

            Assert.True(editor.RemoveApe);

            Assert.True(editor.RemoveApple);

            Assert.True(editor.RemoveAsf);

            Assert.True(editor.RemoveRiffInfo);

            var options = ((TagRemoverFilter)step.Filter).Options;

            Assert.False(options.All);

            Assert.Equal(
                [
                    AudioTagBlockKind.Id3v1,
                    AudioTagBlockKind.Id3v2,
                    AudioTagBlockKind.Xiph,
                    AudioTagBlockKind.Ape,
                    AudioTagBlockKind.Apple,
                    AudioTagBlockKind.Asf,
                    AudioTagBlockKind.RiffInfo,
                ],
                options.Blocks
            );

            editor.RemoveId3v2 = false;

            editor.RemoveXiph = false;

            editor.RemoveApe = false;

            editor.RemoveApple = false;

            editor.RemoveAsf = false;

            editor.RemoveRiffInfo = false;

            options = ((TagRemoverFilter)step.Filter).Options;

            Assert.False(options.All);

            Assert.Equal([AudioTagBlockKind.Id3v1], options.Blocks);

            editor.RemoveId3v1 = false;

            Assert.True(editor.RemoveAll);

            Assert.False(editor.AreBlockTypesEnabled);

            Assert.True(((TagRemoverFilter)step.Filter).Options.All);
        }
    }
}
