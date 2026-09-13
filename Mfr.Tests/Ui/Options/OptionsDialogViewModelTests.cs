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
            Assert.Empty(vm.SuppressedConfirmations);
            Assert.False(vm.DoubleClickAddsToRenameList);
            Assert.Equal(RenameListAddMode.Files, vm.AddMode);
            Assert.True(vm.AddFolderContents);
            Assert.Equal(RenameLogRetentionMode.Limited, vm.RenameLogRetentionMode);
            Assert.Equal(OptionsDialogViewModel.DefaultLimitedCount, vm.RenameLogLimitedCount);
        }

        [Fact]
        public void Constructor_loads_prefs_drafts()
        {
            ConfigStore.MainWindow = new MainWindowPrefs { RememberWindowState = false };
            ConfigStore.FileList = new FileListPrefs { RememberLastFolder = false, DoubleClickAddsToRenameList = true };
            ConfigStore.Ui.SuppressedConfirmations = [ConfirmationKind.ClearRenameList];
            ConfigStore.RenameList = new RenameListPrefs
            {
                AddMode = RenameListAddMode.Folders,
                AddFolderContents = false,
            };
            ConfigStore.RenameLog.Limit = 25;

            var vm = new OptionsDialogViewModel();

            Assert.False(vm.RememberLastFolder);
            Assert.False(vm.RememberWindowState);
            Assert.Equal([ConfirmationKind.ClearRenameList], vm.SuppressedConfirmations);
            Assert.True(vm.DoubleClickAddsToRenameList);
            Assert.Equal(RenameListAddMode.Folders, vm.AddMode);
            Assert.False(vm.AddFolderContents);
            Assert.Equal(RenameLogRetentionMode.Limited, vm.RenameLogRetentionMode);
            Assert.Equal(25, vm.RenameLogLimitedCount);
        }

        [Theory]
        [InlineData(0, RenameLogRetentionMode.Disabled, OptionsDialogViewModel.DefaultLimitedCount)]
        [InlineData(int.MaxValue, RenameLogRetentionMode.Unlimited, OptionsDialogViewModel.DefaultLimitedCount)]
        [InlineData(7, RenameLogRetentionMode.Limited, 7)]
        public void Constructor_maps_renameLog_limit(int limit, RenameLogRetentionMode mode, decimal limitedCount)
        {
            ConfigStore.RenameLog.Limit = limit;

            var vm = new OptionsDialogViewModel();

            Assert.Equal(mode, vm.RenameLogRetentionMode);
            Assert.Equal(limitedCount, vm.RenameLogLimitedCount);
        }

        [Fact]
        public void Commit_writes_prefs_memory()
        {
            ConfigStore.MainWindow = new MainWindowPrefs { RememberWindowState = true };
            ConfigStore.FileList = new FileListPrefs { RememberLastFolder = true, DoubleClickAddsToRenameList = false };
            ConfigStore.Ui.SuppressedConfirmations = [ConfirmationKind.GoWithPreviewErrors];
            ConfigStore.RenameList = new RenameListPrefs
            {
                AddMode = RenameListAddMode.Files,
                AddFolderContents = true,
            };
            ConfigStore.RenameLog.Limit = 10;

            var vm = new OptionsDialogViewModel()
            {
                RememberLastFolder = false,
                RememberWindowState = false,
                SuppressedConfirmations = [ConfirmationKind.DeletePreset],
                DoubleClickAddsToRenameList = true,
                AddMode = RenameListAddMode.FilesAndFolders,
                AddFolderContents = false,
                RenameLogLimitedCount = 3,
                RenameLogRetentionMode = RenameLogRetentionMode.Limited,
            };

            vm.Commit();

            Assert.False(ConfigStore.FileList.RememberLastFolder);
            Assert.False(ConfigStore.MainWindow.RememberWindowState);
            Assert.Equal([ConfirmationKind.DeletePreset], ConfigStore.Ui.SuppressedConfirmations);
            Assert.True(ConfigStore.FileList.DoubleClickAddsToRenameList);
            Assert.Equal(RenameListAddMode.FilesAndFolders, ConfigStore.RenameList.AddMode);
            Assert.False(ConfigStore.RenameList.AddFolderContents);
            Assert.Equal(3, ConfigStore.RenameLog.Limit);
        }

        [Fact]
        public void Commit_creates_missing_prefs_sections()
        {
            ConfigStore.Ui.SuppressedConfirmations = [ConfirmationKind.UndoRename];

            var vm = new OptionsDialogViewModel()
            {
                RememberLastFolder = false,
                RememberWindowState = false,
                SuppressedConfirmations = [],
                DoubleClickAddsToRenameList = true,
                AddMode = RenameListAddMode.Folders,
                AddFolderContents = false,
                RenameLogRetentionMode = RenameLogRetentionMode.Unlimited,
            };

            vm.Commit();

            Assert.NotNull(ConfigStore.FileList);
            Assert.NotNull(ConfigStore.MainWindow);
            Assert.NotNull(ConfigStore.RenameList);
            Assert.False(ConfigStore.FileList.RememberLastFolder);
            Assert.False(ConfigStore.MainWindow.RememberWindowState);
            Assert.Empty(ConfigStore.Ui.SuppressedConfirmations);
            Assert.True(ConfigStore.FileList.DoubleClickAddsToRenameList);
            Assert.Equal(RenameListAddMode.Folders, ConfigStore.RenameList.AddMode);
            Assert.False(ConfigStore.RenameList.AddFolderContents);
            Assert.Equal(int.MaxValue, ConfigStore.RenameLog.Limit);
        }

        [Theory]
        [InlineData(RenameLogRetentionMode.Disabled, 10, 0)]
        [InlineData(RenameLogRetentionMode.Unlimited, 10, int.MaxValue)]
        [InlineData(RenameLogRetentionMode.Limited, 42, 42)]
        public void Commit_round_trips_renameLog_limit(
            RenameLogRetentionMode mode,
            decimal limitedCount,
            int expectedLimit
        )
        {
            ConfigStore.RenameLog.Limit = 10;
            var vm = new OptionsDialogViewModel { RenameLogLimitedCount = limitedCount, RenameLogRetentionMode = mode };

            vm.Commit();

            Assert.Equal(expectedLimit, ConfigStore.RenameLog.Limit);
        }

        [Fact]
        public void Changing_limited_count_selects_Limited_mode()
        {
            ConfigStore.RenameLog.Limit = 0;
            var vm = new OptionsDialogViewModel();
            Assert.Equal(RenameLogRetentionMode.Disabled, vm.RenameLogRetentionMode);

            vm.RenameLogLimitedCount = 5;

            Assert.Equal(RenameLogRetentionMode.Limited, vm.RenameLogRetentionMode);
            Assert.Equal(5, vm.RenameLogLimitedCount);
        }
    }
}
