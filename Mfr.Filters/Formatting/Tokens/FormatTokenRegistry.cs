using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens
{
    /// <summary>
    /// Name-to-delegate map of formatter token resolvers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Token group modules call <see cref="Register(string, Func{string, RenameItem, string})"/>
    /// (or its no-arg overload) and <see cref="RegisterAlias"/> during registry construction.
    /// Names are case-sensitive and must match the literal text inside <c>&lt;name:arg&gt;</c> templates.
    /// </para>
    /// </remarks>
    internal sealed class FormatTokenRegistry
    {
        private readonly Dictionary<string, Func<string, RenameItem, string>> _nameToResolver = new(StringComparer.Ordinal);

        /// <summary>
        /// Registers a resolver that consumes the token argument string.
        /// </summary>
        /// <param name="name">Canonical token name (text before <c>:</c> in the template).</param>
        /// <param name="resolver">Resolver invoked with the raw argument text and the rename item.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or already registered.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="resolver"/> is <c>null</c>.</exception>
        public void Register(string name, Func<string, RenameItem, string> resolver)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(resolver);
            if (_nameToResolver.ContainsKey(name))
                throw new ArgumentException($"Format token '{name}' is already registered.", nameof(name));

            _nameToResolver[name] = resolver;
        }

        /// <summary>
        /// Registers a resolver that ignores the token argument string.
        /// </summary>
        /// <param name="name">Canonical token name.</param>
        /// <param name="resolver">Resolver invoked with only the rename item.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or already registered.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="resolver"/> is <c>null</c>.</exception>
        public void Register(string name, Func<RenameItem, string> resolver)
        {
            ArgumentNullException.ThrowIfNull(resolver);
            Register(name, (_, item) => resolver(item));
        }

        /// <summary>
        /// Adds an alternate name that resolves to the same delegate as <paramref name="canonicalName"/>.
        /// </summary>
        /// <param name="alias">Alternate token name.</param>
        /// <param name="canonicalName">Existing canonical token name to alias.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="alias"/> is already registered or <paramref name="canonicalName"/> is not registered.</exception>
        public void RegisterAlias(string alias, string canonicalName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(alias);
            ArgumentException.ThrowIfNullOrWhiteSpace(canonicalName);
            if (_nameToResolver.ContainsKey(alias))
                throw new ArgumentException($"Format token alias '{alias}' is already registered.", nameof(alias));
            if (!_nameToResolver.TryGetValue(canonicalName, out var resolver))
                throw new ArgumentException($"Cannot alias '{alias}' to unknown token '{canonicalName}'.", nameof(canonicalName));

            _nameToResolver[alias] = resolver;
        }

        /// <summary>
        /// Resolves a token by name, throwing when the name has no registration.
        /// </summary>
        /// <param name="name">Token name parsed from the template.</param>
        /// <param name="arg">Raw argument text after the first <c>:</c>, or empty.</param>
        /// <param name="item">Rename item used by item-aware resolvers.</param>
        /// <returns>The resolved token text.</returns>
        /// <exception cref="NotSupportedException">Thrown when <paramref name="name"/> is not registered.</exception>
        public string Resolve(string name, string arg, RenameItem item)
        {
            if (!_nameToResolver.TryGetValue(name, out var resolver))
                throw new NotSupportedException($"Phase 1 formatter token '{name}' is not supported.");

            return resolver(arg, item);
        }
    }
}
