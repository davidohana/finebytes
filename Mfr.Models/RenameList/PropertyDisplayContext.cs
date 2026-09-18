namespace Mfr.Models.RenameList
{
    /// <summary>
    /// Where a property value is being formatted for display.
    /// <para>
    /// Used when Token and Grid intentionally diverge (for example PDF Created/Modified
    /// culture and <see cref="DateTimeOffset"/> vs local <see cref="DateTime"/>). Domains
    /// with no fork still take this parameter so formatters share one signature.
    /// </para>
    /// </summary>
    internal enum PropertyDisplayContext
    {
        /// <summary>Formatter token expansion.</summary>
        Token,

        /// <summary>Rename List grid column.</summary>
        Grid,
    }
}
