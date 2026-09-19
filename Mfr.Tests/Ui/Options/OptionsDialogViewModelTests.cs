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
            Assert.Equal("No confirmations are currently suppressed.", vm.SuppressedConfirmationsSummary);
            Assert.False(vm.DoubleClickAddsToRenameList);
            Assert.Equal(RenameListAddMode.Files, vm.AddMode);
            Assert.True(vm.AddFolderContents);
            Assert.False(vm.IncludeHidden);
            Assert.True(vm.RememberColumnWidths);
            Assert.Equal(string.Empty, vm.GeoNamesUsername);
            Assert.Equal(RenameLogRetentionMode.Limited, vm.RenameLogRetentionMode);
            Assert.Equal(OptionsDialogViewModel.DefaultLimitedCount, vm.RenameLogLimitedCount);
        }

        [Fact]
        public void Constructor_loads_prefs_drafts()
        {
            ConfigStore.Options.RememberWindowState = false;
            ConfigStore.Options.RememberLastFolder = false;
            ConfigStore.Options.DoubleClickAddsToRenameList = true;
            ConfigStore.Options.SuppressedConfirmations = [ConfirmationKind.ClearRenameList];
            ConfigStore.Options.AddMode = RenameListAddMode.Folders;
            ConfigStore.Options.AddFolderContents = false;
            ConfigStore.Options.IncludeHidden = true;
            ConfigStore.Options.RememberColumnWidths = false;
            ConfigStore.Options.GeoNamesUsername = "geo-user";
            ConfigStore.RenameLog.Limit = 25;

            var vm = new OptionsDialogViewModel();

            Assert.False(vm.RememberLastFolder);
            Assert.False(vm.RememberWindowState);
            Assert.Equal([ConfirmationKind.ClearRenameList], vm.SuppressedConfirmations);
            Assert.Equal("1 confirmation is currently suppressed.", vm.SuppressedConfirmationsSummary);
            Assert.True(vm.DoubleClickAddsToRenameList);
            Assert.Equal(RenameListAddMode.Folders, vm.AddMode);
            Assert.False(vm.AddFolderContents);
            Assert.True(vm.IncludeHidden);
            Assert.False(vm.RememberColumnWidths);
            Assert.Equal("geo-user", vm.GeoNamesUsername);
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
            ConfigStore.Options.RememberWindowState = true;
            ConfigStore.Options.RememberLastFolder = true;
            ConfigStore.Options.DoubleClickAddsToRenameList = false;
            ConfigStore.Options.SuppressedConfirmations = [ConfirmationKind.GoWithPreviewErrors];
            ConfigStore.Options.AddMode = RenameListAddMode.Files;
            ConfigStore.Options.AddFolderContents = true;
            ConfigStore.Options.IncludeHidden = false;
            ConfigStore.Options.RememberColumnWidths = true;
            ConfigStore.Options.GeoNamesUsername = string.Empty;
            ConfigStore.RenameLog.Limit = RenameLogConfig.DefaultLimit;

            var vm = new OptionsDialogViewModel()
            {
                RememberLastFolder = false,
                RememberWindowState = false,
                SuppressedConfirmations = [ConfirmationKind.DeletePreset],
                DoubleClickAddsToRenameList = true,
                AddMode = RenameListAddMode.FilesAndFolders,
                AddFolderContents = false,
                IncludeHidden = true,
                RememberColumnWidths = false,
                GeoNamesUsername = "override-user",
                RenameLogLimitedCount = 3,
                RenameLogRetentionMode = RenameLogRetentionMode.Limited,
            };

            vm.Commit();

            Assert.False(ConfigStore.Options.RememberLastFolder);
            Assert.False(ConfigStore.Options.RememberWindowState);
            Assert.Equal([ConfirmationKind.DeletePreset], ConfigStore.Options.SuppressedConfirmations);
            Assert.True(ConfigStore.Options.DoubleClickAddsToRenameList);
            Assert.Equal(RenameListAddMode.FilesAndFolders, ConfigStore.Options.AddMode);
            Assert.False(ConfigStore.Options.AddFolderContents);
            Assert.True(ConfigStore.Options.IncludeHidden);
            Assert.False(ConfigStore.Options.RememberColumnWidths);
            Assert.Equal("override-user", ConfigStore.Options.GeoNamesUsername);
            Assert.Equal(3, ConfigStore.RenameLog.Limit);
        }

        [Fact]
        public void Commit_trims_geonames_username()
        {
            ConfigStore.Options.GeoNamesUsername = string.Empty;

            var vm = new OptionsDialogViewModel() { GeoNamesUsername = "  my-geo  " };
            vm.Commit();

            Assert.Equal("my-geo", ConfigStore.Options.GeoNamesUsername);
        }

        [Fact]
        public void Commit_writes_options_without_creating_session_sections()
        {
            ConfigStore.Options.SuppressedConfirmations = [ConfirmationKind.UndoRename];
            Assert.Null(ConfigStore.FileList);
            Assert.Null(ConfigStore.RenameList);

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

            Assert.Null(ConfigStore.FileList);
            Assert.Null(ConfigStore.RenameList);
            Assert.False(ConfigStore.Options.RememberLastFolder);
            Assert.False(ConfigStore.Options.RememberWindowState);
            Assert.Empty(ConfigStore.Options.SuppressedConfirmations);
            Assert.True(ConfigStore.Options.DoubleClickAddsToRenameList);
            Assert.Equal(RenameListAddMode.Folders, ConfigStore.Options.AddMode);
            Assert.False(ConfigStore.Options.AddFolderContents);
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
            ConfigStore.RenameLog.Limit = RenameLogConfig.DefaultLimit;
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

        [Fact]
        public void ResetConfirmations_clears_draft_only()
        {
            ConfigStore.Options.SuppressedConfirmations =
            [
                ConfirmationKind.ClearRenameList,
                ConfirmationKind.DeletePreset,
            ];

            var vm = new OptionsDialogViewModel();
            Assert.Equal([ConfirmationKind.ClearRenameList, ConfirmationKind.DeletePreset], vm.SuppressedConfirmations);
            Assert.Equal("2 confirmations are currently suppressed.", vm.SuppressedConfirmationsSummary);

            vm.ResetConfirmations();

            Assert.Empty(vm.SuppressedConfirmations);
            Assert.Equal("No confirmations are currently suppressed.", vm.SuppressedConfirmationsSummary);
            Assert.Equal(
                [ConfirmationKind.ClearRenameList, ConfirmationKind.DeletePreset],
                ConfigStore.Options.SuppressedConfirmations
            );
        }

        [Fact]
        public void Commit_after_ResetConfirmations_writes_empty_suppress_list()
        {
            ConfigStore.Options.SuppressedConfirmations = [ConfirmationKind.GoWithPreviewErrors];
            var vm = new OptionsDialogViewModel();

            vm.ResetConfirmations();
            vm.Commit();

            Assert.Empty(vm.SuppressedConfirmations);
            Assert.Empty(ConfigStore.Options.SuppressedConfirmations);
        }
    }
}
