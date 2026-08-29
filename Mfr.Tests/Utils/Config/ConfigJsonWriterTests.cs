using System.Text.Json.Nodes;
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

        private enum SampleLeafMode
        {
            Files = 0,
            Folders = 1,
        }

        [Fact]
        public void MergeInto_writes_string_leaf_values()
        {
            var root = new JsonObject();
            var options = new SampleOptions
            {
                Port = 42,
                Enabled = false,
                Mode = SampleLeafMode.Folders,
                Name = "ok",
            };

            ConfigJsonWriter.MergeInto(root, options);

            Assert.Equal("42", root["port"]?.GetValue<string>());
            Assert.Equal("false", root["enabled"]?.GetValue<string>());
            Assert.Equal("folders", root["mode"]?.GetValue<string>());
            Assert.Equal("ok", root["name"]?.GetValue<string>());
        }

        [Fact]
        public void MergeInto_writes_empty_string_leaves()
        {
            var root = new JsonObject();
            ConfigJsonWriter.MergeInto(root, new SampleOptions());

            Assert.Equal(string.Empty, root["name"]?.GetValue<string>());
        }

        [Fact]
        public void MergeInto_preserves_unrelated_keys_in_same_section()
        {
            var section = new JsonObject { ["custom"] = "keep" };
            ConfigJsonWriter.MergeInto(section, new SampleOptions { Port = 7 });

            Assert.Equal("keep", section["custom"]?.GetValue<string>());
            Assert.Equal("7", section["port"]?.GetValue<string>());
        }
    }
}
