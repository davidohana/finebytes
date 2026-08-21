namespace Mfr.Filters
{
    /// <summary>
    /// Palette group for a preset filter (matches MFR 7 Available Filters toolbar categories).
    /// </summary>
    public enum FilterGroup
    {
        /// <summary>Case / casing transforms.</summary>
        Case,

        /// <summary>Space and word-separator filters.</summary>
        Space,

        /// <summary>Trim and extract filters.</summary>
        Trimming,

        /// <summary>Find/replace and cleaning filters.</summary>
        Replace,

        /// <summary>Formatter, counter, inserter, and related.</summary>
        Formatting,

        /// <summary>Embedded audio tag filters.</summary>
        Audio,

        /// <summary>Filesystem attributes and timestamps.</summary>
        Attributes,

        /// <summary>Miscellaneous utilities.</summary>
        Misc,
    }
}
