using System.Reflection;
using System.Text.Json;

namespace Mfr.Utils
{
    /// <summary>
    /// Applies JSON config values to public instance fields using <see cref="ConfigIntRangeAttribute"/> and
    /// <see cref="ConfigStringMaxLengthAttribute"/> together with <see cref="ConfigValueReader"/>.
    /// </summary>
    public static class ConfigAnnotatedFields
    {
        /// <summary>
        /// For each public instance field declared on the runtime type of <paramref name="target"/> that carries
        /// <see cref="ConfigIntRangeAttribute"/> or <see cref="ConfigStringMaxLengthAttribute"/>, reads the matching
        /// JSON object property (values must be JSON strings, including integer settings) and updates the field.
        /// Omitted properties and JSON null leave the current field value unchanged.
        /// </summary>
        /// <param name="configObject">A JSON object (typically the document root).</param>
        /// <param name="target">The object whose annotated fields are updated.</param>
        /// <param name="jsonPropertyNamingPolicy">
        /// Converts CLR field names to JSON property names. When <c>null</c>, <see cref="JsonNamingPolicy.CamelCase"/> is used.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="target"/> is null.</exception>
        /// <exception cref="InvalidDataException">JSON or values are invalid (see <see cref="ConfigValueReader"/>).</exception>
        /// <exception cref="InvalidOperationException">
        /// A field has incompatible attributes or types, or both range and string-length attributes.
        /// </exception>
        public static void ApplyFromJsonObject(
            JsonElement configObject,
            object target,
            JsonNamingPolicy? jsonPropertyNamingPolicy = null)
        {
            ArgumentNullException.ThrowIfNull(target);

            var naming = jsonPropertyNamingPolicy ?? JsonNamingPolicy.CamelCase;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            foreach (var field in target.GetType().GetFields(flags))
            {
                var intRange = field.GetCustomAttribute<ConfigIntRangeAttribute>();
                var strMax = field.GetCustomAttribute<ConfigStringMaxLengthAttribute>();
                if (intRange is not null && strMax is not null)
                {
                    throw new InvalidOperationException(
                        $"Field '{field.Name}' cannot specify both [{nameof(ConfigIntRangeAttribute)}] and [{nameof(ConfigStringMaxLengthAttribute)}].");
                }

                var jsonName = naming.ConvertName(field.Name);

                if (intRange is not null)
                {
                    if (field.FieldType != typeof(int))
                    {
                        throw new InvalidOperationException(
                            $"Field '{field.Name}' has [{nameof(ConfigIntRangeAttribute)}] but is not int.");
                    }

                    var value = (int)field.GetValue(target)!;
                    ConfigValueReader.ReadInt(
                        configObject,
                        jsonName,
                        ref value,
                        minInclusive: intRange.MinInclusive,
                        maxInclusive: intRange.MaxInclusive);
                    field.SetValue(target, value);
                    continue;
                }

                if (strMax is not null)
                {
                    if (field.FieldType != typeof(string))
                    {
                        throw new InvalidOperationException(
                            $"Field '{field.Name}' has [{nameof(ConfigStringMaxLengthAttribute)}] but is not string.");
                    }

                    var value = (string)field.GetValue(target)!;
                    ConfigValueReader.ReadString(
                        configObject,
                        jsonName,
                        ref value,
                        maxLengthInclusive: strMax.MaxLengthInclusive);
                    field.SetValue(target, value);
                }
            }
        }
    }
}
