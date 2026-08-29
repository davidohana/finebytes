using Mfr.App.Ui.Services.FileList;
using Mfr.App.Ui.ViewModels.FileList;
using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Tests for Rename List fixed-width font preference.
    /// </summary>
    public sealed class RenameListFixedWidthFontTests
    {
        [Fact]
        public void SetUseFixedWidthFont_updates_vm_and_in_memory_config()
        {
            ConfigStore.Config.Ui.RenameListUseFixedWidthFont = false;

            var fileListViewModel = new FileListViewModel(
                NullSystemIconProvider.Instance,
                Path.GetTempPath(),
                NullFileShellOpener.Instance
            );
            try
            {
                var renameListViewModel = new RenameListViewModel(fileListViewModel);
                Assert.False(renameListViewModel.UseFixedWidthFont);

                renameListViewModel.SetUseFixedWidthFont(true);

                Assert.True(renameListViewModel.UseFixedWidthFont);
                Assert.True(ConfigStore.Config.Ui.RenameListUseFixedWidthFont);
            }
            finally
            {
                fileListViewModel.Dispose();
                ConfigStore.Config.Ui.RenameListUseFixedWidthFont = false;
            }
        }

        [Fact]
        public void ToggleUseFixedWidthFont_flips_value()
        {
            ConfigStore.Config.Ui.RenameListUseFixedWidthFont = false;

            var fileListViewModel = new FileListViewModel(
                NullSystemIconProvider.Instance,
                Path.GetTempPath(),
                NullFileShellOpener.Instance
            );
            try
            {
                var renameListViewModel = new RenameListViewModel(fileListViewModel);
                Assert.False(renameListViewModel.UseFixedWidthFont);

                renameListViewModel.ToggleUseFixedWidthFontCommand.Execute(null);

                Assert.True(renameListViewModel.UseFixedWidthFont);
                Assert.True(ConfigStore.Config.Ui.RenameListUseFixedWidthFont);

                renameListViewModel.ToggleUseFixedWidthFontCommand.Execute(null);

                Assert.False(renameListViewModel.UseFixedWidthFont);
                Assert.False(ConfigStore.Config.Ui.RenameListUseFixedWidthFont);
            }
            finally
            {
                fileListViewModel.Dispose();
                ConfigStore.Config.Ui.RenameListUseFixedWidthFont = false;
            }
        }
    }
}
