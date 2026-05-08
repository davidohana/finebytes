namespace Mfr.Filters.Formatting.Tokens.GeneralGroup
{
    /// <summary>
    /// Registers general-purpose tokens (counter, current time).
    /// </summary>
    internal static class GeneralGroupTokens
    {
        /// <summary>
        /// Registers all General group tokens into <paramref name="registry"/>.
        /// </summary>
        /// <param name="registry">Registry to populate.</param>
        public static void Register(FormatTokenRegistry registry)
        {
            registry.Register("counter", CounterResolver.Resolve);
            registry.Register("now", NowResolver.Resolve);
        }
    }
}
