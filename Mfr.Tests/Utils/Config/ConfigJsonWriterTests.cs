using System.Text.Json;
using Mfr.Utils.Config;

namespace Mfr.Tests.Utils.Config
{
    /// <summary>
    /// Tests for <see cref="ConfigJsonWriter"/>.
    /// </summary>
    public sealed class ConfigJsonWriterTests
    {
        private sealed class SampleOptions
        {
            [ConfigIntRange(1, 100)]
            public int Port = 10;

            [ConfigStringMaxLength(32)]
            public string Name = string.Empty;

            public bool Enabled = true;

            public SampleLeafMode Mode = SampleLeafMode.Files;
        }

        private sealed class SampleSectionRoot
        {
            [ConfigSection]
            public SampleOptions Inner = new();
        }

        private enum SampleLeafMode
        {
            Files = 0,
            Folders = 1,
        }

        [Fact]
        public void Write_writes_string_leaf_values()
        {
            var options = new SampleOptions
            {
                Port = 42,
                Enabled = false,
                Mode = SampleLeafMode.Folders,
                Name = "ok",
            };

            var root = ConfigJsonWriter.Write(options);

            Assert.Equal("42", root["port"]?.GetValue<string>());
            Assert.Equal("false", root["enabled"]?.GetValue<string>());
            Assert.Equal("folders", root["mode"]?.GetValue<string>());
            Assert.Equal("ok", root["name"]?.GetValue<string>());
        }

        [Fact]
        public void Write_writes_empty_string_leaves()
        {
            var root = ConfigJsonWriter.Write(new SampleOptions());

            Assert.Equal(string.Empty, root["name"]?.GetValue<string>());
        }

        [Fact]
        public void Write_then_Apply_round_trips_leaves_and_section()
        {
            var original = new SampleSectionRoot
            {
                Inner =
                {
                    Port = 42,
                    Enabled = false,
                    Mode = SampleLeafMode.Folders,
                    Name = "ok",
                },
            };

            var json = ConfigJsonWriter.Write(original);
            var copy = new SampleSectionRoot();
            using var doc = JsonDocument.Parse(json.ToJsonString());
            ConfigJsonApplier.Apply(doc.RootElement, copy);

            Assert.Equal(42, copy.Inner.Port);
            Assert.False(copy.Inner.Enabled);
            Assert.Equal(SampleLeafMode.Folders, copy.Inner.Mode);
            Assert.Equal("ok", copy.Inner.Name);
        }
    }
}
