namespace Mfr.Filters.Formatting
{
    /// <summary>
    /// Marks a concrete <see cref="Tokens.IFormatToken"/> with FormatEditor catalog metadata.
    /// </summary>
    /// <param name="displayName">Human-readable label shown in the insert picker.</param>
    /// <param name="group">Menu group path; use <c>\</c> for nesting (for example <c>Audio\Tag</c>).</param>
    /// <param name="shortDescription">One-line tooltip / hint text.</param>
    /// <param name="initial">Default inner insert text without angle brackets (may include default args).</param>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class FormatTokenInfoAttribute(
        string displayName,
        string group,
        string shortDescription,
        string initial
    ) : Attribute
    {
        /// <summary>
        /// Gets the human-readable label shown in the insert picker.
        /// </summary>
        public string DisplayName { get; } = displayName;

        /// <summary>
        /// Gets the menu group path (<c>\</c>-separated for nesting).
        /// </summary>
        public string Group { get; } = group;

        /// <summary>
        /// Gets the one-line tooltip / hint text.
        /// </summary>
        public string ShortDescription { get; } = shortDescription;

        /// <summary>
        /// Gets the default inner insert text without angle brackets.
        /// </summary>
        public string Initial { get; } = initial;
    }
}
