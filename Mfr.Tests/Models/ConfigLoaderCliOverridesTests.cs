using System.Text;
using Mfr.Models;

namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests for <see cref="ConfigLoader.ApplyCliOverrides"/>.
    /// </summary>
    public sealed class ConfigLoaderCliOverridesTests
    {
        public ConfigLoaderCliOverridesTests()
        {
            var emptyConfigPath = Path.Combine(Path.GetTempPath(), "mfr-test-empty-config-" + Guid.NewGuid() + ".json");
            File.WriteAllText(emptyConfigPath, """{}""", Encoding.UTF8);
            ConfigLoader.Load(emptyConfigPath);
        }

        [Fact]
        public void Load_Throws_When_Explicit_Config_Path_Missing()
        {
            var missing = Path.Combine(Path.GetTempPath(), "mfr-test-missing-config-" + Guid.NewGuid() + ".json");
            var ex = Assert.Throws<InvalidDataException>(() => ConfigLoader.Load(missing));
            Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ApplyCliOverrides_EmptyList_Does_Not_Change_Defaults()
        {
            var expected = ConfigLoader.Settings.Log.MaxSessionFiles;
            ConfigLoader.ApplyCliOverrides([]);
            Assert.Equal(expected, ConfigLoader.Settings.Log.MaxSessionFiles);
        }

        [Fact]
        public void ApplyCliOverrides_Sets_Log_Field()
        {
            ConfigLoader.ApplyCliOverrides(["log.maxSessionFiles=77"]);
            Assert.Equal(77, ConfigLoader.Settings.Log.MaxSessionFiles);
        }

        [Fact]
        public void ApplyCliOverrides_Sets_Filters_Field()
        {
            ConfigLoader.ApplyCliOverrides(["filters.maxListFileLineLength=2500"]);
            Assert.Equal(2500, ConfigLoader.Settings.Filters.MaxListFileLineLength);
        }

        [Fact]
        public void ApplyCliOverrides_Last_Duplicate_Key_Wins()
        {
            ConfigLoader.ApplyCliOverrides(["log.maxSessionFiles=50", "log.maxSessionFiles=60"]);
            Assert.Equal(60, ConfigLoader.Settings.Log.MaxSessionFiles);
        }

        [Fact]
        public void ApplyCliOverrides_Rejects_Missing_Equals()
        {
            var ex = Assert.Throws<InvalidDataException>(() => ConfigLoader.ApplyCliOverrides(["log.maxSessionFiles"]));
            Assert.Contains("Invalid --set argument", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void ApplyCliOverrides_Rejects_Single_Segment_Path()
        {
            var ex = Assert.Throws<InvalidDataException>(() => ConfigLoader.ApplyCliOverrides(["log=1"]));
            Assert.Contains("section and a field", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void ApplyCliOverrides_Rejects_Unknown_Section()
        {
            var ex = Assert.Throws<InvalidDataException>(() =>
                ConfigLoader.ApplyCliOverrides(["unknownSection.maxListFileLineLength=1"]));
            Assert.Contains("Unknown config section 'unknownSection'", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void ApplyCliOverrides_Rejects_Unknown_Leaf()
        {
            var ex = Assert.Throws<InvalidDataException>(() =>
                ConfigLoader.ApplyCliOverrides(["log.notARealField=x"]));
            Assert.Contains("Unknown config field 'notARealField'", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void ApplyCliOverrides_Rejects_Int_Out_Of_Range()
        {
            var ex = Assert.Throws<InvalidDataException>(() =>
                ConfigLoader.ApplyCliOverrides(["log.maxSessionFiles=0"]));
            Assert.Contains("CLI config override", ex.Message, StringComparison.Ordinal);
        }
    }
}
