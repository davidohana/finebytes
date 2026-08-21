
namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests for <see cref="ConfigStore.ApplyCliOverrides"/>.
    /// </summary>
    public sealed class ConfigStoreCliOverridesTests
    {
        public ConfigStoreCliOverridesTests()
        {
            var emptyConfigPath = Path.Combine(Path.GetTempPath(), "mfr-test-empty-config-" + Guid.NewGuid() + ".json");
            File.WriteAllText(emptyConfigPath, """{}""");
            ConfigStore.Load(emptyConfigPath);
        }

        [Fact]
        public void Load_Throws_When_Explicit_Config_Path_Missing()
        {
            var missing = Path.Combine(Path.GetTempPath(), "mfr-test-missing-config-" + Guid.NewGuid() + ".json");
            var ex = Assert.Throws<InvalidDataException>(() => ConfigStore.Load(missing));
            Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ApplyCliOverrides_EmptyList_Does_Not_Change_Defaults()
        {
            var expected = ConfigStore.Config.Log.MaxSessionFiles;
            ConfigStore.ApplyCliOverrides([]);
            Assert.Equal(expected, ConfigStore.Config.Log.MaxSessionFiles);
        }

        [Fact]
        public void ApplyCliOverrides_Sets_Log_DirectoryPath()
        {
            ConfigStore.ApplyCliOverrides([@"log.directoryPath=C:\Temp\mfr-logs"]);
            Assert.Equal(@"C:\Temp\mfr-logs", ConfigStore.Config.Log.DirectoryPath);
        }

        [Fact]
        public void ApplyCliOverrides_Sets_Log_Field()
        {
            ConfigStore.ApplyCliOverrides(["log.maxSessionFiles=77"]);
            Assert.Equal(77, ConfigStore.Config.Log.MaxSessionFiles);
        }

        [Fact]
        public void ApplyCliOverrides_Sets_Filters_Field()
        {
            ConfigStore.ApplyCliOverrides(["filters.maxListFileLineLength=2500"]);
            Assert.Equal(2500, ConfigStore.Config.Filters.MaxListFileLineLength);
        }

        [Fact]
        public void ApplyCliOverrides_Last_Duplicate_Key_Wins()
        {
            ConfigStore.ApplyCliOverrides(["log.maxSessionFiles=50", "log.maxSessionFiles=60"]);
            Assert.Equal(60, ConfigStore.Config.Log.MaxSessionFiles);
        }

        [Fact]
        public void ApplyCliOverrides_Rejects_Missing_Equals()
        {
            var ex = Assert.Throws<InvalidDataException>(() => ConfigStore.ApplyCliOverrides(["log.maxSessionFiles"]));
            Assert.Contains("Invalid --set argument", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void ApplyCliOverrides_Rejects_Single_Segment_Path()
        {
            var ex = Assert.Throws<InvalidDataException>(() => ConfigStore.ApplyCliOverrides(["log=1"]));
            Assert.Contains("section and a field", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void ApplyCliOverrides_Rejects_Unknown_Section()
        {
            var ex = Assert.Throws<InvalidDataException>(() =>
                ConfigStore.ApplyCliOverrides(["unknownSection.maxListFileLineLength=1"]));
            Assert.Contains("Unknown config section 'unknownSection'", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void ApplyCliOverrides_Rejects_Unknown_Leaf()
        {
            var ex = Assert.Throws<InvalidDataException>(() =>
                ConfigStore.ApplyCliOverrides(["log.notARealField=x"]));
            Assert.Contains("Unknown config field 'notARealField'", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void ApplyCliOverrides_Rejects_Int_Out_Of_Range()
        {
            var ex = Assert.Throws<InvalidDataException>(() =>
                ConfigStore.ApplyCliOverrides(["log.maxSessionFiles=0"]));
            Assert.Contains("CLI config override", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void ApplyCliOverrides_Sets_Ui_RememberLastFolder()
        {
            ConfigStore.ApplyCliOverrides(["ui.rememberLastFolder=false"]);
            Assert.False(ConfigStore.Config.Ui.RememberLastFolder);
            Assert.True(ConfigStore.Config.Ui.RememberWindowState);
        }

        [Fact]
        public void ApplyCliOverrides_Sets_Ui_RememberWindowState()
        {
            ConfigStore.ApplyCliOverrides(["ui.rememberWindowState=false"]);
            Assert.False(ConfigStore.Config.Ui.RememberWindowState);
        }
    }
}
