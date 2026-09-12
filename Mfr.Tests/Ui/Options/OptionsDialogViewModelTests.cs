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
            var emptyPath = Path.Combine(Path.GetTempPath(), "mfr-test-options-empty-" + Guid.NewGuid() + ".json");
            File.WriteAllText(emptyPath, """{}""");
            try
            {
                ConfigStore.Load(emptyPath);
            }
            finally
            {
                File.Delete(emptyPath);
            }
        }

        [Fact]
        public void Constructor_loads_session_and_config_drafts()
        {
            var session = new SessionState
            {
                MainWindow = new SessionStateMainWindow { RememberWindowState = false },
                FileList = new SessionStateFileList { RememberLastFolder = false },
            };
            ConfigStore.Config.Ui.Presets.ConfirmReplaceAppliedFiltersOnLoad = true;

            var vm = new OptionsDialogViewModel(session);

            Assert.False(vm.RememberLastFolder);
            Assert.False(vm.RememberWindowState);
            Assert.True(vm.ConfirmReplaceAppliedFiltersOnLoad);
        }

        [Fact]
        public void Commit_writes_session_and_config_memory()
        {
            var session = new SessionState
            {
                MainWindow = new SessionStateMainWindow { RememberWindowState = true },
                FileList = new SessionStateFileList { RememberLastFolder = true },
            };
            ConfigStore.Config.Ui.Presets.ConfirmReplaceAppliedFiltersOnLoad = false;

            var vm = new OptionsDialogViewModel(session)
            {
                RememberLastFolder = false,
                RememberWindowState = false,
                ConfirmReplaceAppliedFiltersOnLoad = true,
            };

            vm.Commit();

            Assert.False(session.FileList.RememberLastFolder);
            Assert.False(session.MainWindow.RememberWindowState);
            Assert.True(ConfigStore.Config.Ui.Presets.ConfirmReplaceAppliedFiltersOnLoad);
        }

        [Fact]
        public void Commit_creates_missing_session_sections()
        {
            var session = new SessionState();
            ConfigStore.Config.Ui.Presets.ConfirmReplaceAppliedFiltersOnLoad = false;

            var vm = new OptionsDialogViewModel(session)
            {
                RememberLastFolder = false,
                RememberWindowState = false,
                ConfirmReplaceAppliedFiltersOnLoad = true,
            };

            vm.Commit();

            Assert.NotNull(session.FileList);
            Assert.NotNull(session.MainWindow);
            Assert.False(session.FileList.RememberLastFolder);
            Assert.False(session.MainWindow.RememberWindowState);
            Assert.True(ConfigStore.Config.Ui.Presets.ConfirmReplaceAppliedFiltersOnLoad);
        }

        [Fact]
        public void Cancel_path_leaves_sources_unchanged_when_commit_skipped()
        {
            var session = new SessionState
            {
                MainWindow = new SessionStateMainWindow { RememberWindowState = true },
                FileList = new SessionStateFileList { RememberLastFolder = true },
            };
            ConfigStore.Config.Ui.Presets.ConfirmReplaceAppliedFiltersOnLoad = false;

            var vm = new OptionsDialogViewModel(session)
            {
                RememberLastFolder = false,
                RememberWindowState = false,
                ConfirmReplaceAppliedFiltersOnLoad = true,
            };

            Assert.True(session.FileList.RememberLastFolder);
            Assert.True(session.MainWindow.RememberWindowState);
            Assert.False(ConfigStore.Config.Ui.Presets.ConfirmReplaceAppliedFiltersOnLoad);
            Assert.False(vm.RememberLastFolder);
            Assert.True(vm.ConfirmReplaceAppliedFiltersOnLoad);
        }
    }
}
