namespace Mfr.Models
{
    /// <summary>
    /// Declares inclusive integer bounds for an <see cref="MfrSettings"/> field when it is read from JSON by
    /// <see cref="ConfigLoader"/>.
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
    /// Declares a maximum string length for an <see cref="MfrSettings"/> field when it is read from JSON by
    /// <see cref="ConfigLoader"/>.
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
}
