namespace Mfr.Filters
{
    /// <summary>
    /// Marks a concrete filter for the Available Filters palette catalog.
    /// </summary>
    /// <param name="group">Toolbar group that owns this filter.</param>
    /// <param name="displayName">Human-readable label shown in the list.</param>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class FilterPaletteAttribute(FilterGroup group, string displayName) : Attribute
    {
        /// <summary>
        /// Gets the toolbar group that owns this filter.
        /// </summary>
        public FilterGroup Group { get; } = group;

        /// <summary>
        /// Gets the human-readable label shown in the Available Filters list.
        /// </summary>
        public string DisplayName { get; } = displayName;
    }
}
