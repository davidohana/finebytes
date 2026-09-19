using System.Reflection;
using System.Text.Json;
using Mfr.Utils.Config;

namespace Mfr.Tests.Models
{
    /// <summary>
    /// Ensures every prefs field participates in JSON / CLI config binding.
    /// </summary>
    public sealed class PrefsBindingTests
    {
        [Theory]
        [InlineData(typeof(LogConfig))]
        [InlineData(typeof(OptionsConfig))]
        [InlineData(typeof(RenameLogConfig))]
        public void Every_public_instance_field_participates_in_config_binding(Type configType)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            foreach (var field in configType.GetFields(flags))
            {
                var hasSection = field.GetCustomAttribute<ConfigSectionAttribute>() is not null;
                var hasInt = field.GetCustomAttribute<ConfigIntRangeAttribute>() is not null;
                var hasStr = field.GetCustomAttribute<ConfigStringMaxLengthAttribute>() is not null;
                var isBoolLeaf = field.FieldType == typeof(bool);
                var isEnumLeaf = field.FieldType.IsEnum;
                var isEnumListLeaf =
                    field.FieldType.IsGenericType
                    && field.FieldType.GetGenericTypeDefinition() == typeof(List<>)
                    && field.FieldType.GetGenericArguments()[0].IsEnum;
                var n =
                    (hasSection ? 1 : 0)
                    + (hasInt ? 1 : 0)
                    + (hasStr ? 1 : 0)
                    + (isBoolLeaf ? 1 : 0)
                    + (isEnumLeaf ? 1 : 0)
                    + (isEnumListLeaf ? 1 : 0);
                Assert.True(
                    n == 1,
                    $"{configType.Name}.{field.Name} must be a [{nameof(ConfigSectionAttribute)}] section, "
                        + $"a [{nameof(ConfigIntRangeAttribute)}] / [{nameof(ConfigStringMaxLengthAttribute)}] leaf, "
                        + $"or an unannotated bool / enum / enum-list leaf."
                );
            }
        }

        /// <summary>
        /// Verifies <c>options.suppressedConfirmations</c> binds from a JSON string array.
        /// </summary>
        [Fact]
        public void Options_suppressed_confirmations_binds_from_json_array()
        {
            using var doc = JsonDocument.Parse( /*lang=json,strict*/
                """
                {
                  "options": {
                    "suppressedConfirmations": ["clearRenameList", "deletePreset"]
                  }
                }
                """
            );
            var options = new OptionsConfig();
            Assert.Empty(options.SuppressedConfirmations);

            var root = new PrefsRootForTest { Options = options };
            ConfigJsonApplier.Apply(doc.RootElement, root);

            Assert.Equal(
                [ConfirmationKind.ClearRenameList, ConfirmationKind.DeletePreset],
                options.SuppressedConfirmations
            );
        }

        /// <summary>
        /// Verifies soft-load skips unknown enum members and keeps known ones.
        /// </summary>
        [Fact]
        public void Options_suppressed_confirmations_soft_skips_unknown_members()
        {
            using var doc = JsonDocument.Parse( /*lang=json,strict*/
                """
                {
                  "options": {
                    "suppressedConfirmations": ["clearRenameList", "notARealKind", "undoRename"]
                  }
                }
                """
            );
            var root = new PrefsRootForTest();
            ConfigJsonApplier.ApplySoft(doc.RootElement, root);

            Assert.Equal(
                [ConfirmationKind.ClearRenameList, ConfirmationKind.UndoRename],
                root.Options.SuppressedConfirmations
            );
        }

        /// <summary>
        /// Verifies hard Apply rejects unknown enum-list members (CLI / strict binding).
        /// </summary>
        [Fact]
        public void Options_suppressed_confirmations_hard_rejects_unknown_members()
        {
            using var doc = JsonDocument.Parse( /*lang=json,strict*/
                """
                {
                  "options": {
                    "suppressedConfirmations": ["clearRenameList", "notARealKind"]
                  }
                }
                """
            );
            var root = new PrefsRootForTest();
            Assert.Throws<InvalidDataException>(() => ConfigJsonApplier.Apply(doc.RootElement, root));
            Assert.Empty(root.Options.SuppressedConfirmations);
        }

        /// <summary>
        /// Verifies <c>renameLog.limit</c> binds from a JSON string leaf (including 0).
        /// </summary>
        [Fact]
        public void RenameLog_limit_leaf_binds_from_json_string()
        {
            using var doc = JsonDocument.Parse( /*lang=json,strict*/
                """
                {
                  "renameLog": {
                    "limit": "0"
                  }
                }
                """
            );
            var root = new PrefsRootForTest();
            Assert.Equal(10, root.RenameLog.Limit);

            ConfigJsonApplier.Apply(doc.RootElement, root);

            Assert.Equal(0, root.RenameLog.Limit);
        }

        /// <summary>
        /// Verifies omitted <c>options</c> leaves stay at their defaults.
        /// </summary>
        [Fact]
        public void Options_suppressed_confirmations_defaults_when_omitted()
        {
            using var doc = JsonDocument.Parse( /*lang=json,strict*/
                """{"log":{"maxSessionFiles":"100"}}"""
            );
            var root = new PrefsRootForTest();
            ConfigJsonApplier.Apply(doc.RootElement, root);
            Assert.Empty(root.Options.SuppressedConfirmations);
            Assert.True(root.Options.RememberWindowState);
        }

        /// <summary>
        /// Verifies soft-load binds <c>options.geoNamesUsername</c>.
        /// </summary>
        [Fact]
        public void Options_geoNamesUsername_soft_binds_string()
        {
            using var doc = JsonDocument.Parse( /*lang=json,strict*/
                """
                {
                  "options": {
                    "geoNamesUsername": "my-geo-user"
                  }
                }
                """
            );
            var root = new PrefsRootForTest();
            Assert.Equal(string.Empty, root.Options.GeoNamesUsername);

            ConfigJsonApplier.ApplySoft(doc.RootElement, root);

            Assert.Equal("my-geo-user", root.Options.GeoNamesUsername);
        }

        /// <summary>
        /// Verifies soft-load leaves GeoNames username empty when the leaf is missing.
        /// </summary>
        [Fact]
        public void Options_geoNamesUsername_missing_stays_empty()
        {
            using var doc = JsonDocument.Parse( /*lang=json,strict*/
                """
                {
                  "options": {
                    "rememberLastFolder": "false"
                  }
                }
                """
            );
            var root = new PrefsRootForTest();
            ConfigJsonApplier.ApplySoft(doc.RootElement, root);

            Assert.Equal(string.Empty, root.Options.GeoNamesUsername);
            Assert.False(root.Options.RememberLastFolder);
        }

        /// <summary>
        /// Minimal prefs root mirroring <see cref="ConfigStore"/> private binder shape for applier tests.
        /// </summary>
        private sealed class PrefsRootForTest
        {
            [ConfigSection]
            public LogConfig Log = new();

            [ConfigSection("options")]
            public OptionsConfig Options = new();

            [ConfigSection]
            public RenameLogConfig RenameLog = new();
        }
    }
}
