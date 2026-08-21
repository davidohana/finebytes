using System.Reflection;
using Mfr.Utils.Config;

namespace Mfr.Tests.Models
{
    /// <summary>
    /// Ensures every settings field participates in JSON / CLI config binding.
    /// </summary>
    public sealed class MfrSettingsConfigBindingTests
    {
        [Theory]
        [InlineData(typeof(MfrSettings))]
        [InlineData(typeof(FilterSettings))]
        [InlineData(typeof(LogSettings))]
        [InlineData(typeof(UiSettings))]
        public void Every_public_instance_field_participates_in_config_binding(Type settingsType)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            foreach (var field in settingsType.GetFields(flags))
            {
                var hasSection = field.GetCustomAttribute<ConfigSectionAttribute>() is not null;
                var hasInt = field.GetCustomAttribute<ConfigIntRangeAttribute>() is not null;
                var hasStr = field.GetCustomAttribute<ConfigStringMaxLengthAttribute>() is not null;
                var isBoolLeaf = field.FieldType == typeof(bool);
                var n = (hasSection ? 1 : 0) + (hasInt ? 1 : 0) + (hasStr ? 1 : 0) + (isBoolLeaf ? 1 : 0);
                Assert.True(
                    n == 1,
                    $"{settingsType.Name}.{field.Name} must be a [{nameof(ConfigSectionAttribute)}] section, " +
                    $"a [{nameof(ConfigIntRangeAttribute)}] / [{nameof(ConfigStringMaxLengthAttribute)}] leaf, " +
                    $"or an unannotated bool leaf.");
            }
        }
    }
}
