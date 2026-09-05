using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors.Misc;
using Mfr.App.Ui.Views.FilterEditors;
using Mfr.Filters.Misc;

namespace Mfr.Tests.Ui.FilterEditors.Misc
{
    /// <summary>
    /// Unit tests for <see cref="PathMoverFilterEditorViewModel"/>.
    /// </summary>
    public sealed class PathMoverFilterEditorViewModelTests
    {
        /// <summary>
        /// Verifies Mover option edits replace the step filter options.
        /// </summary>
        [Fact]
        public void Options_update_step_options()
        {
            var step = new AppliedFilterStepViewModel("Path Mover", new PathMoverFilter());
            var editor = new PathMoverFilterEditorViewModel(step);

            Assert.Equal(@"C:\", editor.RootFolder);
            Assert.Equal("MFR", editor.SubFolder);

            editor.RootFolder = @"D:\Music";
            editor.SubFolder = @"<parent-folder>\<file-name>";

            var options = ((PathMoverFilter)step.Filter).Options;
            Assert.Equal(@"D:\Music", options.RootFolder);
            Assert.Equal(@"<parent-folder>\<file-name>", options.SubFolder);
        }

        /// <summary>
        /// Verifies Browse applies a picked root folder to the step options.
        /// </summary>
        [Fact]
        public async Task Browse_applies_picked_root_folder()
        {
            var step = new AppliedFilterStepViewModel("Path Mover", new PathMoverFilter());
            var editor = new PathMoverFilterEditorViewModel(step)
            {
                PickRootFolderAsync = (current, _) =>
                {
                    Assert.Equal(@"C:\", current);
                    return Task.FromResult<string?>(@"D:\Picked");
                },
            };

            await editor.BrowseRootFolderCommand.ExecuteAsync(null);

            Assert.Equal(@"D:\Picked", editor.RootFolder);
            Assert.Equal(@"D:\Picked", ((PathMoverFilter)step.Filter).Options.RootFolder);
        }

        /// <summary>
        /// Verifies Browse leaves options unchanged when the picker is cancelled.
        /// </summary>
        [Fact]
        public async Task Browse_cancelled_leaves_root_unchanged()
        {
            var step = new AppliedFilterStepViewModel("Path Mover", new PathMoverFilter());
            var editor = new PathMoverFilterEditorViewModel(step)
            {
                PickRootFolderAsync = (_, _) => Task.FromResult<string?>(null),
            };

            await editor.BrowseRootFolderCommand.ExecuteAsync(null);

            Assert.Equal(@"C:\", editor.RootFolder);
            Assert.Equal(@"C:\", ((PathMoverFilter)step.Filter).Options.RootFolder);
        }

        /// <summary>
        /// Verifies Browse treats a whitespace-only pick as cancel.
        /// </summary>
        [Fact]
        public async Task Browse_whitespace_pick_leaves_root_unchanged()
        {
            var step = new AppliedFilterStepViewModel("Path Mover", new PathMoverFilter());
            var editor = new PathMoverFilterEditorViewModel(step)
            {
                PickRootFolderAsync = (_, _) => Task.FromResult<string?>("   "),
            };

            await editor.BrowseRootFolderCommand.ExecuteAsync(null);

            Assert.Equal(@"C:\", editor.RootFolder);
            Assert.Equal(@"C:\", ((PathMoverFilter)step.Filter).Options.RootFolder);
        }

        /// <summary>
        /// Verifies Browse is a no-op when no folder picker is wired.
        /// </summary>
        [Fact]
        public async Task Browse_without_picker_leaves_root_unchanged()
        {
            var step = new AppliedFilterStepViewModel("Path Mover", new PathMoverFilter());
            var editor = new PathMoverFilterEditorViewModel(step);

            await editor.BrowseRootFolderCommand.ExecuteAsync(null);

            Assert.Equal(@"C:\", editor.RootFolder);
            Assert.Equal(@"C:\", ((PathMoverFilter)step.Filter).Options.RootFolder);
        }

        /// <summary>
        /// Verifies folder-path resolution for File List drops onto path fields.
        /// </summary>
        [Fact]
        public void File_drop_resolves_folder_or_parent()
        {
            var dir = Path.Combine(Path.GetTempPath(), "mfr-filter-drop-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var nested = Path.Combine(dir, "Nested");
                Directory.CreateDirectory(nested);
                var filePath = Path.Combine(nested, "a.txt");
                File.WriteAllText(filePath, "x");

                Assert.Equal(nested, FilterEditorFileDrop.TryResolveFolderPath([nested]));
                Assert.Equal(nested, FilterEditorFileDrop.TryResolveFolderPath([filePath]));
                Assert.Null(FilterEditorFileDrop.TryResolveFolderPath([]));
            }
            finally
            {
                try
                {
                    Directory.Delete(dir, recursive: true);
                }
                catch (IOException) { }
            }
        }
    }
}
