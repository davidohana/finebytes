namespace Mfr.Utils.Config
{
    /// <summary>
    /// Declares inclusive integer bounds for a field when it is populated from JSON by
    /// <see cref="ConfigApplier.Apply"/>.
    /// </summary>
    /// <param name="minInclusive">Minimum allowed value when the JSON property is present.</param>
    /// <param name="maxInclusive">Maximum allowed value when the JSON property is present.</param>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ConfigIntRangeAttribute(int minInclusive, int maxInclusive) : Attribute
    {
        /// <summary>
        /// Gets the minimum allowed value when the JSON property is present.
        /// </summary>
        public int MinInclusive { get; } = minInclusive;

        /// <summary>
        /// Gets the maximum allowed value when the JSON property is present.
        /// </summary>
        public int MaxInclusive { get; } = maxInclusive;
    }

    /// <summary>
    /// Declares a maximum string length for a field when it is populated from JSON by
    /// <see cref="ConfigApplier.Apply"/>.
    /// </summary>
    /// <param name="maxLengthInclusive">Maximum allowed length when the JSON property is present.</param>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ConfigStringMaxLengthAttribute(int maxLengthInclusive) : Attribute
    {
        /// <summary>
        /// Gets the maximum allowed length when the JSON property is present.
        /// </summary>
        public int MaxLengthInclusive { get; } = maxLengthInclusive;
    }

    /// <summary>
    /// Marks a field whose value is a nested settings object mapped from a JSON object property.
    /// <see cref="ConfigApplier.Apply"/> reads that property (by <see cref="JsonName"/> or the field name via the naming policy) and applies recursively.
    /// </summary>
    /// <param name="jsonName">Optional JSON object property name; when null, the name is derived from the field name using the applier naming policy.</param>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ConfigSectionAttribute(string? jsonName = null) : Attribute
    {
        /// <summary>
        /// Gets the JSON property name when set; otherwise the applier derives the name from the field.
        /// </summary>
        public string? JsonName { get; } = jsonName;
    }
}
