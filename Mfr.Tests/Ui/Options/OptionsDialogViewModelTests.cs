using Mfr.App.Ui.ViewModels.Options;

namespace Mfr.Tests.Ui.Options
{
    /// <summary>
    /// Unit tests for <see cref="OptionsDialogViewModel"/>.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class OptionsDialogViewModelTests
    {
        public OptionsDialogViewModelTests()
        {
            ConfigStoreTestReset.LoadEmpty();
        }

        [Fact]
        public void Constructor_loads_session_and_config_drafts()
        {
            ConfigStore.MainWindow = new SessionStateMainWindow { RememberWindowState = false };
            ConfigStore.FileList = new SessionStateFileList
            {
                RememberLastFolder = false,
                DoubleClickAddsToRenameList = true,
            };
            ConfigStore.Ui.ConfirmationPrompts = ConfirmationPrompts.More;

            var vm = new OptionsDialogViewModel();

            Assert.False(vm.RememberLastFolder);
            Assert.False(vm.RememberWindowState);
            Assert.Equal(ConfirmationPrompts.More, vm.ConfirmationPrompts);
            Assert.True(vm.DoubleClickAddsToRenameList);
        }

        [Fact]
        public void Commit_writes_session_and_config_memory()
        {
            ConfigStore.MainWindow = new SessionStateMainWindow { RememberWindowState = true };
            ConfigStore.FileList = new SessionStateFileList
            {
                RememberLastFolder = true,
                DoubleClickAddsToRenameList = false,
            };
            ConfigStore.Ui.ConfirmationPrompts = ConfirmationPrompts.Fewer;

            var vm = new OptionsDialogViewModel()
            {
                RememberLastFolder = false,
                RememberWindowState = false,
                ConfirmationPrompts = ConfirmationPrompts.More,
                DoubleClickAddsToRenameList = true,
            };

            vm.Commit();

            Assert.False(ConfigStore.FileList.RememberLastFolder);
            Assert.False(ConfigStore.MainWindow.RememberWindowState);
            Assert.Equal(ConfirmationPrompts.More, ConfigStore.Ui.ConfirmationPrompts);
            Assert.True(ConfigStore.FileList.DoubleClickAddsToRenameList);
        }

        [Fact]
        public void Commit_creates_missing_session_sections()
        {
            ConfigStore.Ui.ConfirmationPrompts = ConfirmationPrompts.Fewer;

            var vm = new OptionsDialogViewModel()
            {
                RememberLastFolder = false,
                RememberWindowState = false,
                ConfirmationPrompts = ConfirmationPrompts.More,
                DoubleClickAddsToRenameList = true,
            };

            vm.Commit();

            Assert.NotNull(ConfigStore.FileList);
            Assert.NotNull(ConfigStore.MainWindow);
            Assert.False(ConfigStore.FileList.RememberLastFolder);
            Assert.False(ConfigStore.MainWindow.RememberWindowState);
            Assert.Equal(ConfirmationPrompts.More, ConfigStore.Ui.ConfirmationPrompts);
            Assert.True(ConfigStore.FileList.DoubleClickAddsToRenameList);
        }
    }
}
