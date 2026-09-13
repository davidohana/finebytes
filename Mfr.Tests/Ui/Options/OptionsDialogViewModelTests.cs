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
        public void Constructor_uses_defaults_when_prefs_sections_missing()
        {
            var vm = new OptionsDialogViewModel();

            Assert.True(vm.RememberLastFolder);
            Assert.True(vm.RememberWindowState);
            Assert.Equal(ConfirmationPrompts.Normal, vm.ConfirmationPrompts);
            Assert.False(vm.DoubleClickAddsToRenameList);
            Assert.Equal(RenameListAddMode.Files, vm.AddMode);
            Assert.True(vm.AddFolderContents);
        }

        [Fact]
        public void Constructor_loads_prefs_drafts()
        {
            ConfigStore.MainWindow = new MainWindowPrefs { RememberWindowState = false };
            ConfigStore.FileList = new FileListPrefs { RememberLastFolder = false, DoubleClickAddsToRenameList = true };
            ConfigStore.Ui.ConfirmationPrompts = ConfirmationPrompts.More;
            ConfigStore.RenameList = new RenameListPrefs
            {
                AddMode = RenameListAddMode.Folders,
                AddFolderContents = false,
            };

            var vm = new OptionsDialogViewModel();

            Assert.False(vm.RememberLastFolder);
            Assert.False(vm.RememberWindowState);
            Assert.Equal(ConfirmationPrompts.More, vm.ConfirmationPrompts);
            Assert.True(vm.DoubleClickAddsToRenameList);
            Assert.Equal(RenameListAddMode.Folders, vm.AddMode);
            Assert.False(vm.AddFolderContents);
        }

        [Fact]
        public void Commit_writes_prefs_memory()
        {
            ConfigStore.MainWindow = new MainWindowPrefs { RememberWindowState = true };
            ConfigStore.FileList = new FileListPrefs { RememberLastFolder = true, DoubleClickAddsToRenameList = false };
            ConfigStore.Ui.ConfirmationPrompts = ConfirmationPrompts.Fewer;
            ConfigStore.RenameList = new RenameListPrefs
            {
                AddMode = RenameListAddMode.Files,
                AddFolderContents = true,
            };

            var vm = new OptionsDialogViewModel()
            {
                RememberLastFolder = false,
                RememberWindowState = false,
                ConfirmationPrompts = ConfirmationPrompts.More,
                DoubleClickAddsToRenameList = true,
                AddMode = RenameListAddMode.FilesAndFolders,
                AddFolderContents = false,
            };

            vm.Commit();

            Assert.False(ConfigStore.FileList.RememberLastFolder);
            Assert.False(ConfigStore.MainWindow.RememberWindowState);
            Assert.Equal(ConfirmationPrompts.More, ConfigStore.Ui.ConfirmationPrompts);
            Assert.True(ConfigStore.FileList.DoubleClickAddsToRenameList);
            Assert.Equal(RenameListAddMode.FilesAndFolders, ConfigStore.RenameList.AddMode);
            Assert.False(ConfigStore.RenameList.AddFolderContents);
        }

        [Fact]
        public void Commit_creates_missing_prefs_sections()
        {
            ConfigStore.Ui.ConfirmationPrompts = ConfirmationPrompts.Fewer;

            var vm = new OptionsDialogViewModel()
            {
                RememberLastFolder = false,
                RememberWindowState = false,
                ConfirmationPrompts = ConfirmationPrompts.More,
                DoubleClickAddsToRenameList = true,
                AddMode = RenameListAddMode.Folders,
                AddFolderContents = false,
            };

            vm.Commit();

            Assert.NotNull(ConfigStore.FileList);
            Assert.NotNull(ConfigStore.MainWindow);
            Assert.NotNull(ConfigStore.RenameList);
            Assert.False(ConfigStore.FileList.RememberLastFolder);
            Assert.False(ConfigStore.MainWindow.RememberWindowState);
            Assert.Equal(ConfirmationPrompts.More, ConfigStore.Ui.ConfirmationPrompts);
            Assert.True(ConfigStore.FileList.DoubleClickAddsToRenameList);
            Assert.Equal(RenameListAddMode.Folders, ConfigStore.RenameList.AddMode);
            Assert.False(ConfigStore.RenameList.AddFolderContents);
        }
    }
}
