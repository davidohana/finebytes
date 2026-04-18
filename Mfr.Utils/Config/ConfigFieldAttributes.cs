namespace Mfr.Utils.Config
{
    /// <summary>
    /// Declares inclusive integer bounds for JSON-backed fields.
    /// <para>Used by <see cref="ConfigJsonApplier.Apply"/>.</para>
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
    /// Declares a maximum string length for JSON-backed fields.
    /// <para>Used by <see cref="ConfigJsonApplier.Apply"/>.</para>
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
    /// Marks a nested settings object mapped from a JSON object property.
    /// <para>
    /// <see cref="ConfigJsonApplier.Apply"/> reads by <see cref="JsonName"/> or the field name, then recurses.
    /// </para>
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
