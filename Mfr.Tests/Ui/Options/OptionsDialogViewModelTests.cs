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
            var session = new SessionState
            {
                MainWindow = new SessionStateMainWindow { RememberWindowState = false },
                FileList = new SessionStateFileList { RememberLastFolder = false },
            };
            ConfigStore.Config.Ui.ConfirmationPrompts = ConfirmationPrompts.More;
            ConfigStore.Config.Ui.DoubleClickAddsToRenameList = true;

            var vm = new OptionsDialogViewModel(session);

            Assert.False(vm.RememberLastFolder);
            Assert.False(vm.RememberWindowState);
            Assert.Equal(ConfirmationPrompts.More, vm.ConfirmationPrompts);
            Assert.True(vm.DoubleClickAddsToRenameList);
        }

        [Fact]
        public void Commit_writes_session_and_config_memory()
        {
            var session = new SessionState
            {
                MainWindow = new SessionStateMainWindow { RememberWindowState = true },
                FileList = new SessionStateFileList { RememberLastFolder = true },
            };
            ConfigStore.Config.Ui.ConfirmationPrompts = ConfirmationPrompts.Fewer;
            ConfigStore.Config.Ui.DoubleClickAddsToRenameList = false;

            var vm = new OptionsDialogViewModel(session)
            {
                RememberLastFolder = false,
                RememberWindowState = false,
                ConfirmationPrompts = ConfirmationPrompts.More,
                DoubleClickAddsToRenameList = true,
            };

            vm.Commit();

            Assert.False(session.FileList.RememberLastFolder);
            Assert.False(session.MainWindow.RememberWindowState);
            Assert.Equal(ConfirmationPrompts.More, ConfigStore.Config.Ui.ConfirmationPrompts);
            Assert.True(ConfigStore.Config.Ui.DoubleClickAddsToRenameList);
        }

        [Fact]
        public void Commit_creates_missing_session_sections()
        {
            var session = new SessionState();
            ConfigStore.Config.Ui.ConfirmationPrompts = ConfirmationPrompts.Fewer;
            ConfigStore.Config.Ui.DoubleClickAddsToRenameList = false;

            var vm = new OptionsDialogViewModel(session)
            {
                RememberLastFolder = false,
                RememberWindowState = false,
                ConfirmationPrompts = ConfirmationPrompts.More,
                DoubleClickAddsToRenameList = true,
            };

            vm.Commit();

            Assert.NotNull(session.FileList);
            Assert.NotNull(session.MainWindow);
            Assert.False(session.FileList.RememberLastFolder);
            Assert.False(session.MainWindow.RememberWindowState);
            Assert.Equal(ConfirmationPrompts.More, ConfigStore.Config.Ui.ConfirmationPrompts);
            Assert.True(ConfigStore.Config.Ui.DoubleClickAddsToRenameList);
        }
    }
}
