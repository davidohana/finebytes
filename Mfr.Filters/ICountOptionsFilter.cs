namespace Mfr.Filters
{
    /// <summary>
    /// Filter that stores its settings in <see cref="CountFilterOptions"/> (Trim/Extract Left/Right).
    /// </summary>
    public interface ICountOptionsFilter
    {
        /// <summary>
        /// Gets the character-count options.
        /// </summary>
        CountFilterOptions Options { get; }

        /// <summary>
        /// Returns a copy of this filter with <paramref name="options"/> applied.
        /// </summary>
        /// <param name="options">Replacement count options.</param>
        /// <returns>Updated filter instance.</returns>
        BaseFilter WithOptions(CountFilterOptions options);
    }
}
