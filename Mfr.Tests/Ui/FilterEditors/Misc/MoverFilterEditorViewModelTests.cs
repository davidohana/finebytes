using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors.Misc;
using Mfr.Filters.Misc;

namespace Mfr.Tests.Ui.FilterEditors.Misc
{
    /// <summary>
    /// Unit tests for <see cref="MoverFilterEditorViewModel"/>.
    /// </summary>
    public sealed class MoverFilterEditorViewModelTests
    {
        /// <summary>
        /// Verifies Mover option edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Options_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Mover", new MoverFilter());
            var editor = new MoverFilterEditorViewModel(step);

            Assert.Equal(@"C:\", editor.RootFolder);
            Assert.Equal("MFR", editor.SubFolder);

            editor.RootFolder = @"D:\Music";
            editor.SubFolder = @"<parent-folder>\<file-name>";

            var options = ((MoverFilter)step.Filter).Options;
            Assert.Equal(@"D:\Music", options.RootFolder);
            Assert.Equal(@"<parent-folder>\<file-name>", options.SubFolder);
        }

        /// <summary>
        /// Verifies Browse applies a picked root folder to the step options.
        /// </summary>
        [Fact]
        public async Task Browse_applies_picked_root_folder()
        {
            var step = new AppliedFilterStepViewModel("Mover", new MoverFilter());
            var editor = new MoverFilterEditorViewModel(step)
            {
                PickRootFolderAsync = (current, _) =>
                {
                    Assert.Equal(@"C:\", current);
                    return Task.FromResult<string?>(@"D:\Picked");
                },
            };

            await editor.BrowseRootFolderCommand.ExecuteAsync(null);

            Assert.Equal(@"D:\Picked", editor.RootFolder);
            Assert.Equal(@"D:\Picked", ((MoverFilter)step.Filter).Options.RootFolder);
        }

        /// <summary>
        /// Verifies Browse leaves options unchanged when the picker is cancelled.
        /// </summary>
        [Fact]
        public async Task Browse_cancelled_leaves_root_unchanged()
        {
            var step = new AppliedFilterStepViewModel("Mover", new MoverFilter());
            var editor = new MoverFilterEditorViewModel(step)
            {
                PickRootFolderAsync = (_, _) => Task.FromResult<string?>(null),
            };

            await editor.BrowseRootFolderCommand.ExecuteAsync(null);

            Assert.Equal(@"C:\", editor.RootFolder);
            Assert.Equal(@"C:\", ((MoverFilter)step.Filter).Options.RootFolder);
        }

        /// <summary>
        /// Verifies Browse treats a whitespace-only pick as cancel.
        /// </summary>
        [Fact]
        public async Task Browse_whitespace_pick_leaves_root_unchanged()
        {
            var step = new AppliedFilterStepViewModel("Mover", new MoverFilter());
            var editor = new MoverFilterEditorViewModel(step)
            {
                PickRootFolderAsync = (_, _) => Task.FromResult<string?>("   "),
            };

            await editor.BrowseRootFolderCommand.ExecuteAsync(null);

            Assert.Equal(@"C:\", editor.RootFolder);
            Assert.Equal(@"C:\", ((MoverFilter)step.Filter).Options.RootFolder);
        }

        /// <summary>
        /// Verifies Browse is a no-op when no folder picker is wired.
        /// </summary>
        [Fact]
        public async Task Browse_without_picker_leaves_root_unchanged()
        {
            var step = new AppliedFilterStepViewModel("Mover", new MoverFilter());
            var editor = new MoverFilterEditorViewModel(step);

            await editor.BrowseRootFolderCommand.ExecuteAsync(null);

            Assert.Equal(@"C:\", editor.RootFolder);
            Assert.Equal(@"C:\", ((MoverFilter)step.Filter).Options.RootFolder);
        }
    }
}
