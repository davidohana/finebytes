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
            };

            ConfigJsonWriter.MergeInto(root, options);

            Assert.Equal("42", root["port"]?.GetValue<string>());
            Assert.Equal("false", root["enabled"]?.GetValue<string>());
            Assert.Equal("folders", root["mode"]?.GetValue<string>());
        }

        [Fact]
        public void MergeInto_preserves_unrelated_keys_in_same_section()
        {
            var root = new JsonObject { ["customUiFlag"] = "legacy" };
            var ui = new JsonObject { ["addMode"] = "files" };
            root["ui"] = ui;

            ConfigJsonWriter.MergeInto(ui, new UiConfig { RenameListUseFixedWidthFont = true });

            Assert.Equal("legacy", root["customUiFlag"]?.GetValue<string>());
            Assert.Equal("files", ui["addMode"]?.GetValue<string>());
            Assert.Equal("true", ui["renameListUseFixedWidthFont"]?.GetValue<string>());
        }
    }
}
