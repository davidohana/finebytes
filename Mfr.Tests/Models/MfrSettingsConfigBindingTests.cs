using System.Reflection;
using Mfr.Models;
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
        public void Every_public_instance_field_has_exactly_one_config_binding_attribute(Type settingsType)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            foreach (var field in settingsType.GetFields(flags))
            {
                var hasSection = field.GetCustomAttribute<ConfigSectionAttribute>() is not null;
                var hasInt = field.GetCustomAttribute<ConfigIntRangeAttribute>() is not null;
                var hasStr = field.GetCustomAttribute<ConfigStringMaxLengthAttribute>() is not null;
                var n = (hasSection ? 1 : 0) + (hasInt ? 1 : 0) + (hasStr ? 1 : 0);
                Assert.True(
                    n == 1,
                    $"{settingsType.Name}.{field.Name} must have exactly one of " +
                    $"{nameof(ConfigSectionAttribute)}, {nameof(ConfigIntRangeAttribute)}, or {nameof(ConfigStringMaxLengthAttribute)}.");
            }
        }
    }
}
