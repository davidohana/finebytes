using System.Reflection;
using System.Text.Json;
using Mfr.Utils.Config;

namespace Mfr.Tests.Models
{
    /// <summary>
    /// Ensures every config field participates in JSON / CLI config binding.
    /// </summary>
    public sealed class MfrConfigBindingTests
    {
        [Theory]
        [InlineData(typeof(LogConfig))]
        [InlineData(typeof(UiConfig))]
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
                var n =
                    (hasSection ? 1 : 0)
                    + (hasInt ? 1 : 0)
                    + (hasStr ? 1 : 0)
                    + (isBoolLeaf ? 1 : 0)
                    + (isEnumLeaf ? 1 : 0);
                Assert.True(
                    n == 1,
                    $"{configType.Name}.{field.Name} must be a [{nameof(ConfigSectionAttribute)}] section, "
                        + $"a [{nameof(ConfigIntRangeAttribute)}] / [{nameof(ConfigStringMaxLengthAttribute)}] leaf, "
                        + $"or an unannotated bool / enum leaf."
                );
            }
        }

        /// <summary>
        /// Verifies <c>ui.confirmationPrompts</c> binds from a JSON string leaf.
        /// </summary>
        [Fact]
        public void Ui_confirmation_leaf_binds_from_json_string()
        {
            using var doc = JsonDocument.Parse( /*lang=json,strict*/
                """
                {
                  "ui": {
                    "confirmationPrompts": "more"
                  }
                }
                """
            );
            var ui = new UiConfig();
            Assert.Equal(ConfirmationPrompts.Normal, ui.ConfirmationPrompts);

            var root = new PrefsRootForTest { Ui = ui };
            ConfigJsonApplier.Apply(doc.RootElement, root);

            Assert.Equal(ConfirmationPrompts.More, ui.ConfirmationPrompts);
        }

        /// <summary>
        /// Verifies omitted <c>ui</c> leaves stay at their defaults.
        /// </summary>
        [Fact]
        public void Ui_confirmation_leaf_defaults_when_omitted()
        {
            using var doc = JsonDocument.Parse( /*lang=json,strict*/
                """{"log":{"maxSessionFiles":"100"}}"""
            );
            var root = new PrefsRootForTest();
            ConfigJsonApplier.Apply(doc.RootElement, root);
            Assert.Equal(ConfirmationPrompts.Normal, root.Ui.ConfirmationPrompts);
        }

        /// <summary>
        /// Minimal prefs root mirroring <see cref="ConfigStore"/> private binder shape for applier tests.
        /// </summary>
        private sealed class PrefsRootForTest
        {
            [ConfigSection]
            public LogConfig Log = new();

            [ConfigSection]
            public UiConfig Ui = new();
        }
    }
}
