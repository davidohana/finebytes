using Mfr.App.Ui.Services.FileList;
using Mfr.App.Ui.ViewModels.FileList;
using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Tests for Rename List display preferences.
    /// </summary>
    public sealed class RenameListDisplayOptionsTests
    {
        [Fact]
        public void CommitDisplayOptions_updates_vm_and_in_memory_config()
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

                renameListViewModel.CommitDisplayOptions(useFixedWidthFont: true);

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
        public void Dialog_draft_does_not_change_vm_until_commit()
        {
            var fileListViewModel = new FileListViewModel(
                NullSystemIconProvider.Instance,
                Path.GetTempPath(),
                NullFileShellOpener.Instance
            );
            try
            {
                var renameListViewModel = new RenameListViewModel(fileListViewModel);
                Assert.False(renameListViewModel.UseFixedWidthFont);

                var dialogVm = new RenameListDisplayOptionsDialogViewModel(renameListViewModel.UseFixedWidthFont)
                {
                    UseFixedWidthFont = true,
                };

                Assert.True(dialogVm.UseFixedWidthFont);
                Assert.False(renameListViewModel.UseFixedWidthFont);
            }
            finally
            {
                fileListViewModel.Dispose();
            }
        }
    }
}
