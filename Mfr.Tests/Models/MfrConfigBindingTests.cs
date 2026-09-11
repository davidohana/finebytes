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
        [InlineData(typeof(MfrConfig))]
        [InlineData(typeof(FilterConfig))]
        [InlineData(typeof(LogConfig))]
        [InlineData(typeof(UiConfig))]
        [InlineData(typeof(PresetsUiConfig))]
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
        /// Verifies <c>ui.presets.confirmReplaceAppliedFiltersOnLoad</c> binds from a JSON string bool.
        /// </summary>
        [Fact]
        public void Ui_Presets_ConfirmReplaceAppliedFiltersOnLoad_binds_from_json_string()
        {
            using var doc = JsonDocument.Parse( /*lang=json,strict*/
                """
                {
                  "ui": {
                    "presets": {
                      "confirmReplaceAppliedFiltersOnLoad": "true"
                    }
                  }
                }
                """
            );
            var config = new MfrConfig();
            Assert.False(config.Ui.Presets.ConfirmReplaceAppliedFiltersOnLoad);

            ConfigJsonApplier.Apply(doc.RootElement, config);

            Assert.True(config.Ui.Presets.ConfirmReplaceAppliedFiltersOnLoad);
        }

        /// <summary>
        /// Verifies omitted <c>ui.presets</c> leaves the confirm-replace flag at its default.
        /// </summary>
        [Fact]
        public void Ui_Presets_ConfirmReplaceAppliedFiltersOnLoad_defaults_false_when_omitted()
        {
            using var doc = JsonDocument.Parse( /*lang=json,strict*/
                """{"filters":{"maxListFileLineLength":"1000"}}"""
            );
            var config = new MfrConfig();
            ConfigJsonApplier.Apply(doc.RootElement, config);
            Assert.False(config.Ui.Presets.ConfirmReplaceAppliedFiltersOnLoad);
        }
    }
}
