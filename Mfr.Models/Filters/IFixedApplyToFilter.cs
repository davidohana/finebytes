namespace Mfr.Models.Filters
{
    /// <summary>
    /// Filter that always writes one fixed domain (no selectable <see cref="FilterTarget"/>).
    /// </summary>
    public interface IFixedApplyToFilter
    {
        /// <summary>
        /// Gets the Apply-To label for the Filter Chain and Filter Options.
        /// </summary>
        string FixedApplyToLabel { get; }
    }
}
