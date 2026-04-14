namespace Mfr.Filters
{
    /// <summary>
    /// Defines logical cache scopes used by <see cref="FilterChainContext"/>.
    /// </summary>
    public enum FilterCacheScope
    {
        /// <summary>
        /// Scope for parsed replace-list file entries.
        /// </summary>
        ReplaceListEntries
    }

    /// <summary>
    /// Represents per-chain transient cache storage shared across filters.
    /// </summary>
    public sealed class FilterChainContext
    {
        private readonly Dictionary<FilterCacheKey, object> _keyToValue = [];

        /// <summary>
        /// Gets an existing cached value for <paramref name="key"/> or creates and caches one.
        /// </summary>
        /// <typeparam name="T">Cached value type.</typeparam>
        /// <param name="key">Cache key.</param>
        /// <param name="factory">Factory invoked when cache does not contain the key.</param>
        /// <returns>The cached or newly created value.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the key already exists with a different cached value type.
        /// </exception>
        public T GetOrAdd<T>(FilterCacheKey key, Func<T> factory)
        {
            if (_keyToValue.TryGetValue(key, out var existing))
            {
                if (existing is T typedExisting)
                {
                    return typedExisting;
                }

                throw new InvalidOperationException(
                    $"Cache key '{key}' already stores '{existing.GetType().Name}', not '{typeof(T).Name}'.");
            }

            var created = factory();
            _keyToValue[key] = created!;
            return created;
        }
    }

    /// <summary>
    /// Key used by <see cref="FilterChainContext"/> to isolate cached entries.
    /// </summary>
    /// <param name="Scope">Logical cache scope.</param>
    /// <param name="Id">Scope-specific identifier.</param>
    public readonly record struct FilterCacheKey(FilterCacheScope Scope, string Id);
}
